using Core.DTO;
using Core.Enums;
using Core.Models;
using Infrastructure.MercadoPago;
using Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;

namespace Services
{
	public class AssinaturaService : IAssinaturaService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMercadoPagoClient _mpClient;
		private readonly string _backUrl;

		private const int TipoAdmin = (int)TypeUser.admin;
		private const int TipoGestor = (int)TypeUser.gerente;
		private const int TipoOperador = (int)TypeUser.operador;
		private const int TrialDiasPadrao = 15;

		public AssinaturaService(IUnitOfWork unitOfWork, IMercadoPagoClient mpClient, IConfiguration configuration)
		{
			_unitOfWork = unitOfWork;
			_mpClient = mpClient;
			_backUrl = configuration["MercadoPago:BackUrl"] ?? "https://www.confereset.com.br/assinatura/callback";
		}

		public async Task<CheckoutAssinaturaResponse> IniciarCheckout(CreateAssinaturaRequest req)
		{
			_ = await _unitOfWork.Empresas.GetEmpresaById(req.EmpresaId)
					?? throw new Exception("Empresa não encontrada.");

			var plano = await _unitOfWork.Planos.GetPlanoById(req.PlanoId)
					?? throw new Exception("Plano não encontrado.");

			if (!plano.Ativo)
				throw new Exception("Este plano não está mais disponível. Escolha outro plano.");
			if (plano.Valor <= 0)
				throw new Exception("Plano gratuito/vitalício não é contratado pelo checkout.");

			var email = (req.PayerEmail ?? "").Trim().ToLowerInvariant();
			if (!System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
				throw new Exception("Informe um e-mail válido para o pagamento.");

			// Já está neste plano? (troca para OUTRO plano é permitida: a antiga é cancelada quando
			// a nova for autorizada — ver SincronizarAssinatura.)
			var ativa = await _unitOfWork.Assinaturas.GetAssinaturaAtivaByEmpresaId(req.EmpresaId);
			if (ativa != null && ativa.PlanoId == plano.Id && ativa.Plano?.Valor > 0)
				throw new Exception("Sua empresa já está neste plano.");

			// Checkouts anteriores que ficaram pendentes (abandonados): cancela aqui e no MP para
			// não haver duas cobranças se a pessoa pagar o link antigo depois.
			foreach (var pendente in await _unitOfWork.Assinaturas.GetByEmpresaAndStatus(req.EmpresaId, StatusAssinatura.Pendente))
			{
				if (!string.IsNullOrWhiteSpace(pendente.MPSubscriptionId))
				{
					try { await _mpClient.CancelPreapproval(pendente.MPSubscriptionId); } catch { /* best-effort */ }
				}
				pendente.Status = StatusAssinatura.Cancelada;
				pendente.UltimoStatusMP = "abandoned";
				_unitOfWork.Assinaturas.Update(pendente);
			}

			var comCartao = !string.IsNullOrWhiteSpace(req.Token);
			var externalRef = Guid.NewGuid().ToString("N");

			// Assinatura SEM plano associado no MP (o fluxo antigo usava preapproval_plan_id sem
			// cartão, combinação que o MP recusa). Com cartão → "authorized" (cobra já);
			// sem cartão → "pending" + init_point para pagar no site do MP.
			MPPreapprovalResponse mp;
			try
			{
				mp = await _mpClient.CreatePreapproval(new MPCreatePreapprovalRequest
				{
					Reason = $"Confere SET – {plano.Nome}",
					PayerEmail = email,
					CardTokenId = comCartao ? req.Token!.Trim() : null,
					ExternalReference = externalRef,
					BackUrl = _backUrl,
					Status = comCartao ? "authorized" : "pending",
					AutoRecurring = new MPAutoRecurring
					{
						Frequency = (int)plano.Recorrencia,
						FrequencyType = "months",
						TransactionAmount = decimal.Round(plano.Valor, 2),
						CurrencyId = "BRL"
					}
				});
			}
			catch (MercadoPagoException ex)
			{
				_unitOfWork.Save(); // mantém o cancelamento dos pendentes antigos
				throw new Exception(ex.MensagemUsuario);
			}

			var assinatura = new Assinatura
			{
				EmpresaId = req.EmpresaId,
				PlanoId = plano.Id,
				MPSubscriptionId = mp.Id,
				Status = StatusAssinatura.Pendente,
				DataInicio = DateTime.UtcNow,
				DataVencimento = mp.NextPaymentDate?.ToUniversalTime() ?? DateTime.UtcNow.AddMonths((int)plano.Recorrencia),
				ExternalReference = externalRef,
				MPPayerEmail = email,
				UltimoStatusMP = mp.Status
			};
			await _unitOfWork.Assinaturas.Add(assinatura);
			_unitOfWork.Save();

			// Cartão aprovado na hora: ativa, libera a empresa e encerra a assinatura anterior.
			if (mp.Status == "authorized")
				await AplicarStatus(assinatura, "authorized", mp.NextPaymentDate);

			return new CheckoutAssinaturaResponse
			{
				AssinaturaId = assinatura.Id,
				InitPoint = comCartao ? string.Empty : mp.InitPoint,
				MPSubscriptionId = mp.Id,
				Status = mp.Status,
				Ativa = assinatura.Status == StatusAssinatura.Ativa
			};
		}

		/// <summary>
		/// Aplica o status do Mercado Pago na assinatura local: Ativa/Suspensa/Cancelada/Pendente,
		/// vencimento pelo próximo débito, status da empresa e — quando ativa — cancelamento das
		/// outras assinaturas pagas da empresa (troca de plano).
		/// </summary>
		private async Task AplicarStatus(Assinatura assinatura, string? statusMp, DateTime? proximoPagamento)
		{
			LimparCacheAcesso(assinatura.EmpresaId);
			var anterior = assinatura.Status;
			assinatura.UltimoStatusMP = statusMp;
			assinatura.Status = statusMp switch
			{
				"authorized" => StatusAssinatura.Ativa,
				"paused" => StatusAssinatura.Suspensa,
				"cancelled" => StatusAssinatura.Cancelada,
				"pending" => assinatura.Status == StatusAssinatura.Ativa ? StatusAssinatura.Ativa : StatusAssinatura.Pendente,
				_ => assinatura.Status
			};

			if (assinatura.Status == StatusAssinatura.Ativa)
			{
				if (proximoPagamento.HasValue && proximoPagamento.Value > DateTime.UtcNow)
					assinatura.DataVencimento = proximoPagamento.Value.ToUniversalTime();
				else if (anterior != StatusAssinatura.Ativa && assinatura.Plano != null)
					assinatura.DataVencimento = DateTime.UtcNow.AddMonths((int)assinatura.Plano.Recorrencia);
			}

			_unitOfWork.Assinaturas.Update(assinatura);
			_unitOfWork.Save();

			if (assinatura.Status == StatusAssinatura.Ativa)
			{
				// Troca de plano: a assinatura paga anterior é encerrada (no MP também).
				foreach (var outra in await _unitOfWork.Assinaturas.GetByEmpresaAndStatus(assinatura.EmpresaId, StatusAssinatura.Ativa))
				{
					if (outra.Id == assinatura.Id || outra.Plano?.Valor == 0) continue;
					if (!string.IsNullOrWhiteSpace(outra.MPSubscriptionId))
					{
						try { await _mpClient.CancelPreapproval(outra.MPSubscriptionId); } catch { /* best-effort */ }
					}
					outra.Status = StatusAssinatura.Cancelada;
					outra.UltimoStatusMP = "replaced";
					_unitOfWork.Assinaturas.Update(outra);
				}
				// O trial deixa de valer quando o plano pago começa.
				foreach (var trial in await _unitOfWork.Assinaturas.GetByEmpresaAndStatus(assinatura.EmpresaId, StatusAssinatura.Trial))
				{
					trial.Status = StatusAssinatura.Expirada;
					trial.UltimoStatusMP = "trial_replaced";
					_unitOfWork.Assinaturas.Update(trial);
				}
				_unitOfWork.Save();
			}

			// Empresa acompanha a assinatura (não desativamos por "Cancelada": pode estar trocando de plano).
			if (assinatura.Status == StatusAssinatura.Ativa || assinatura.Status == StatusAssinatura.Suspensa)
			{
				var empresa = await _unitOfWork.Empresas.GetEmpresaById(assinatura.EmpresaId);
				var novoStatus = assinatura.Status == StatusAssinatura.Ativa;
				if (empresa != null && empresa.Status != novoStatus)
				{
					empresa.Status = novoStatus;
					_unitOfWork.Empresas.Update(empresa);
					_unitOfWork.Save();
				}
			}
		}

		/// <summary>
		/// Busca o status real da assinatura no Mercado Pago e aplica localmente. Usado pelo webhook,
		/// pela página de retorno do pagamento e pelo botão "Verificar pagamento". Seguro mesmo sem
		/// autenticação: só aplica o que o próprio Mercado Pago informa para aquele id.
		/// </summary>
		public async Task<string?> SincronizarAssinatura(string mpSubscriptionId)
		{
			if (string.IsNullOrWhiteSpace(mpSubscriptionId)) return null;
			var mpData = await _mpClient.GetPreapproval(mpSubscriptionId.Trim());

			var assinatura = await _unitOfWork.Assinaturas.GetByMPSubscriptionId(mpData.Id)
				?? (!string.IsNullOrWhiteSpace(mpData.ExternalReference)
					? await _unitOfWork.Assinaturas.GetByExternalReference(mpData.ExternalReference)
					: null);
			if (assinatura == null) return null;

			if (string.IsNullOrWhiteSpace(assinatura.MPSubscriptionId)) assinatura.MPSubscriptionId = mpData.Id;
			await AplicarStatus(assinatura, mpData.Status, mpData.NextPaymentDate);
			return mpData.Status;
		}

		public async Task<string?> SincronizarPorId(int assinaturaId)
		{
			var assinatura = await _unitOfWork.Assinaturas.GetAssinaturaById(assinaturaId)
					?? throw new Exception("Assinatura não encontrada.");
			if (string.IsNullOrWhiteSpace(assinatura.MPSubscriptionId))
				return assinatura.UltimoStatusMP;
			try
			{
				return await SincronizarAssinatura(assinatura.MPSubscriptionId);
			}
			catch (MercadoPagoException ex)
			{
				throw new Exception(ex.MensagemUsuario);
			}
		}

		public async Task<bool> AtribuirPlanoVitalicio(int empresaId, int planoId)
		{
			LimparCacheAcesso(empresaId);
			var empresa = await _unitOfWork.Empresas.GetEmpresaById(empresaId)
					?? throw new Exception("Empresa não encontrada.");

			var plano = await _unitOfWork.Planos.GetPlanoById(planoId)
					?? throw new Exception("Plano não encontrado.");

			if (plano.Valor != 0)
				throw new Exception("Este plano não é vitalício.");

			var existente = await _unitOfWork.Assinaturas.GetAssinaturaAtivaByEmpresaId(empresaId);
			if (existente != null)
			{
				existente.Status = StatusAssinatura.Cancelada;
				_unitOfWork.Assinaturas.Update(existente);
			}

			var assinatura = new Assinatura
			{
				EmpresaId = empresaId,
				PlanoId = planoId,
				Status = StatusAssinatura.Ativa,
				DataInicio = DateTime.UtcNow,
				DataVencimento = new DateTime(9999, 12, 31, 0, 0, 0, DateTimeKind.Utc),
				MPSubscriptionId = null,
				MPPayerEmail = null,
				UltimoStatusMP = "lifetime"
			};

			await _unitOfWork.Assinaturas.Add(assinatura);
			_unitOfWork.Save();
			return true;
		}

		public async Task<AssinaturaDTO?> GetByEmpresaId(int empresaId)
		{
			var assinatura = await GetAssinaturaAtualParaAcesso(empresaId);
			return assinatura == null ? null : MapToDTO(assinatura);
		}

		public async Task<AssinaturaDTO?> GetById(int id)
		{
			var assinatura = await _unitOfWork.Assinaturas.GetAssinaturaById(id);
			return assinatura == null ? null : MapToDTO(assinatura);
		}

		public async Task<bool> Cancelar(int id)
		{
			var assinatura = await _unitOfWork.Assinaturas.GetAssinaturaById(id)
					?? throw new Exception("Assinatura não encontrada.");
			LimparCacheAcesso(assinatura.EmpresaId);

			if (assinatura.Plano?.Valor == 0)
				throw new Exception("Assinatura vitalícia não pode ser cancelada.");

			if (!string.IsNullOrWhiteSpace(assinatura.MPSubscriptionId))
			{
				try
				{
					await _mpClient.CancelPreapproval(assinatura.MPSubscriptionId);
				}
				catch (MercadoPagoException ex) when (ex.StatusCode == 400 || ex.StatusCode == 404)
				{
					// Já cancelada/inexistente no MP: segue com o cancelamento local.
				}
				catch (MercadoPagoException ex)
				{
					throw new Exception(ex.MensagemUsuario);
				}
			}

			assinatura.Status = StatusAssinatura.Cancelada;
			_unitOfWork.Assinaturas.Update(assinatura);
			return _unitOfWork.Save() > 0;
		}

		public async Task<List<PagamentoAssinaturaDTO>> ListarPagamentosDaAssinatura(int assinaturaId)
		{
			var pagamentos = await _unitOfWork.PagamentosAssinatura.GetByAssinaturaId(assinaturaId);
			return pagamentos.Select(p => new PagamentoAssinaturaDTO
			{
				Id = p.Id,
				AssinaturaId = p.AssinaturaId,
				Valor = p.Valor,
				DataPagamento = p.DataPagamento,
				Status = p.Status,
				MPPaymentId = p.MPPaymentId
			}).ToList();
		}

		public async Task<List<PagamentoAssinaturaDTO>> ListarPagamentos(DateTime? de, DateTime? ate)
		{
			var pagamentos = await _unitOfWork.PagamentosAssinatura.GetAllComDetalhes(de, ate);
			return pagamentos.Select(p => new PagamentoAssinaturaDTO
			{
				Id = p.Id,
				AssinaturaId = p.AssinaturaId,
				EmpresaId = p.Assinatura?.EmpresaId ?? 0,
				EmpresaNome = p.Assinatura?.Empresa != null
					? (string.IsNullOrWhiteSpace(p.Assinatura.Empresa.TradeName) ? p.Assinatura.Empresa.Name : p.Assinatura.Empresa.TradeName)
					: null,
				PlanoNome = p.Assinatura?.Plano?.Nome,
				Valor = p.Valor,
				DataPagamento = p.DataPagamento,
				Status = p.Status,
				MPPaymentId = p.MPPaymentId
			}).ToList();
		}

		public async Task<(List<AssinaturaDTO> Items, int Total)> GetAllPaged(int page, int pageSize, int empresaId)
		{
			// empresaId <= 0 → dono da plataforma vê todas as empresas.
			var items = await _unitOfWork.Assinaturas.GetAllPaged(page, pageSize, empresaId);
			var total = await _unitOfWork.Assinaturas.CountAll(empresaId);
			return (items.Select(MapToDTO).ToList(), total);
		}

		public async Task<bool> Excluir(int id)
		{
			var assinatura = await _unitOfWork.Assinaturas.GetAssinaturaById(id)
					?? throw new Exception("Assinatura não encontrada.");
			LimparCacheAcesso(assinatura.EmpresaId);

			// Tenta cancelar no Mercado Pago, mas não impede a exclusão local se falhar.
			if (!string.IsNullOrWhiteSpace(assinatura.MPSubscriptionId))
			{
				try { await _mpClient.CancelPreapproval(assinatura.MPSubscriptionId); } catch { /* best-effort */ }
			}

			// Pagamentos vinculados saem em cascata (DeleteBehavior.Cascade).
			_unitOfWork.Assinaturas.Delete(assinatura);
			return _unitOfWork.Save() > 0;
		}

		public async Task<LimitesAssinaturaDTO> VerificarLimites(int empresaId)
		{
			var assinatura = await GetAssinaturaAtualParaAcesso(empresaId);

			if (assinatura?.Plano == null || !AssinaturaLiberaAcesso(assinatura))
				return new LimitesAssinaturaDTO { AssinaturaAtiva = false };

			// Para o plano, "gestores" significa administradores da empresa.
			// Contamos gerente e também registros legados criados como admin para não permitir burlar o limite.
			var totalGestores =
				// O master da plataforma (admin) não ocupa vaga no plano da empresa.
				await _unitOfWork.Users.CountUsersByEmpresaIdAndType(empresaId, TipoGestor);
			var totalOperadores = await _unitOfWork.Users.CountUsersByEmpresaIdAndType(empresaId, TipoOperador);

			return new LimitesAssinaturaDTO
			{
				AssinaturaAtiva = true,
				LimiteGestores = assinatura.Plano.LimiteGestores,
				LimiteOperadores = assinatura.Plano.LimiteOperadores,
				GestoresUtilizados = totalGestores,
				OperadoresUtilizados = totalOperadores,
				PodeAdicionarGestor = totalGestores < assinatura.Plano.LimiteGestores,
				PodeAdicionarOperador = totalOperadores < assinatura.Plano.LimiteOperadores
			};
		}

		public async Task ProcessarWebhookAssinatura(string mpSubscriptionId)
		{
			await SincronizarAssinatura(mpSubscriptionId);
		}

		/// <summary>
		/// Cobrança recorrente (subscription_authorized_payment): registra o pagamento uma vez só e,
		/// quando aprovado, renova o vencimento a partir do status atual da assinatura no MP.
		/// </summary>
		public async Task ProcessarWebhookCobrancaAssinatura(string authorizedPaymentId)
		{
			var ap = await _mpClient.GetAuthorizedPayment(authorizedPaymentId);
			if (string.IsNullOrWhiteSpace(ap.PreapprovalId)) return;

			var assinatura = await _unitOfWork.Assinaturas.GetByMPSubscriptionId(ap.PreapprovalId);
			if (assinatura == null) return;

			var chave = ap.Payment?.Id?.ToString() ?? $"ap-{ap.Id}";
			var status = ap.Payment?.Status ?? ap.Status;
			if (!await _unitOfWork.PagamentosAssinatura.ExistsByMPPaymentId(chave))
			{
				await _unitOfWork.PagamentosAssinatura.Add(new PagamentoAssinatura
				{
					AssinaturaId = assinatura.Id,
					Valor = ap.TransactionAmount,
					DataPagamento = (ap.DebitDate ?? ap.DateCreated ?? DateTime.UtcNow).ToUniversalTime(),
					MPPaymentId = chave,
					Status = status
				});
				_unitOfWork.Save();
			}

			// Status/vencimento vêm da própria assinatura (fonte única).
			await SincronizarAssinatura(ap.PreapprovalId);
		}

		public async Task ProcessarWebhookPagamento(string mpPaymentId)
		{
			if (await _unitOfWork.PagamentosAssinatura.ExistsByMPPaymentId(mpPaymentId)) return;

			var payment = await _mpClient.GetPayment(mpPaymentId);
			var preapprovalId = payment.PreapprovalId;
			if (string.IsNullOrWhiteSpace(preapprovalId) && payment.Metadata != null
				&& payment.Metadata.TryGetValue("preapproval_id", out var meta) && meta.ValueKind == System.Text.Json.JsonValueKind.String)
				preapprovalId = meta.GetString();
			if (string.IsNullOrWhiteSpace(preapprovalId)) return; // pagamento que não é de assinatura

			var assinatura = await _unitOfWork.Assinaturas.GetByMPSubscriptionId(preapprovalId);
			if (assinatura == null) return;

			await _unitOfWork.PagamentosAssinatura.Add(new PagamentoAssinatura
			{
				AssinaturaId = assinatura.Id,
				Valor = payment.TransactionAmount,
				DataPagamento = (payment.DateApproved ?? DateTime.UtcNow).ToUniversalTime(),
				MPPaymentId = mpPaymentId,
				Status = payment.Status
			});
			_unitOfWork.Save();

			await SincronizarAssinatura(preapprovalId);
		}

		private static AssinaturaDTO MapToDTO(Assinatura a) => new()
		{
			Id = a.Id,
			EmpresaId = a.EmpresaId,
			EmpresaNome = a.Empresa?.Name,
			PlanoId = a.PlanoId,
			PlanoNome = a.Plano?.Nome,
			PlanoValor = a.Plano?.Valor ?? 0,
			Status = a.Status,
			DataInicio = a.DataInicio,
			DataVencimento = a.DataVencimento,
			MPSubscriptionId = a.MPSubscriptionId,
			MPPayerEmail = a.MPPayerEmail
		};
		public async Task<CallBackAssinaturaResponse> CallBack(string preapproval_id)
		{
			if (string.IsNullOrWhiteSpace(preapproval_id))
				return new CallBackAssinaturaResponse { Success = false, Status = "not_found", Message = "Pagamento não identificado." };
			string? status;
			try
			{
				status = await SincronizarAssinatura(preapproval_id);
			}
			catch (MercadoPagoException ex)
			{
				return new CallBackAssinaturaResponse { Success = false, Status = "error", Message = ex.MensagemUsuario };
			}
			return status switch
			{
				"authorized" => new CallBackAssinaturaResponse { Success = true, Status = status, Message = "Assinatura ativa! O acesso já está liberado." },
				"pending" => new CallBackAssinaturaResponse { Success = false, Status = status, Message = "Pagamento em processamento no Mercado Pago." },
				"paused" => new CallBackAssinaturaResponse { Success = false, Status = status, Message = "A assinatura está pausada no Mercado Pago." },
				"cancelled" => new CallBackAssinaturaResponse { Success = false, Status = status, Message = "O pagamento não foi concluído e a assinatura foi cancelada." },
				null => new CallBackAssinaturaResponse { Success = false, Status = "not_found", Message = "Não encontramos esta assinatura." },
				_ => new CallBackAssinaturaResponse { Success = false, Status = status, Message = "Status do pagamento: " + status }
			};
		}
		public async Task<bool> AtualizarAssinatura(int assinaturaId, decimal? novoValor = null, string? cardToken = null)
		{
			var assinatura = await _unitOfWork.Assinaturas.GetAssinaturaById(assinaturaId)
					?? throw new Exception("Assinatura não encontrada.");

			if (string.IsNullOrWhiteSpace(assinatura.MPSubscriptionId))
				throw new Exception("Assinatura não vinculada ao Mercado Pago.");

			if (assinatura.Status != StatusAssinatura.Ativa)
				throw new Exception("Só é possível alterar assinaturas ativas.");

			// Monta payload dinâmico
			var updateRequest = new Dictionary<string, object>();

			// Troca de valor
			if (novoValor.HasValue)
			{
				if (novoValor.Value <= 0)
					throw new Exception("O valor deve ser maior que zero.");

				// Só a cobrança desta assinatura no Mercado Pago muda. Antes também alterava
				// Plano.Valor, que é compartilhado: mudava o preço de TODAS as empresas do plano.
				updateRequest["auto_recurring"] = new
				{
					transaction_amount = novoValor.Value
				};
			}

			// 💳 Troca de cartão
			if (!string.IsNullOrWhiteSpace(cardToken))
			{
				updateRequest["card_token_id"] = cardToken;
			}

			// Segurança: não deixa request vazio
			if (!updateRequest.Any())
				throw new Exception("Nenhuma alteração informada.");

			// 🔗 Chamada no Mercado Pago
			await _mpClient.UpdatePreapproval(assinatura.MPSubscriptionId, updateRequest);

			_unitOfWork.Assinaturas.Update(assinatura);
			return _unitOfWork.Save() > 0;
		}

		public async Task<Assinatura> IniciarTrial(int empresaId, int dias = TrialDiasPadrao)
		{
			LimparCacheAcesso(empresaId);
			var empresa = await _unitOfWork.Empresas.GetEmpresaById(empresaId)
				?? throw new Exception("Empresa não encontrada.");

			// Idempotente: se a empresa já tem qualquer assinatura ativa ou trial vigente,
			// não cria outra (evita duplicidade após retry de cadastro).
			var todas = await _unitOfWork.Assinaturas.GetAllPaged(1, 50, empresaId);
			var existente = todas.FirstOrDefault(a => a.Status == StatusAssinatura.Ativa)
				?? todas.FirstOrDefault(a => a.Status == StatusAssinatura.Trial && a.DataVencimento >= DateTime.UtcNow);
			if (existente != null)
				return existente;

			// [fix-500] Antes usávamos PlanoId = 0, mas a FK Assinatura→Plano é obrigatória e
			// não existe Plano com Id 0 → o INSERT do trial estourava (FK violation) e, pior,
			// deixava a entidade quebrada rastreada no DbContext, fazendo o Save() seguinte
			// (criação do usuário) falhar com 500. Agora o trial aponta para um Plano interno
			// (oculto: Ativo=false, EmpresaId=null) criado sob demanda.
			var planoTrialId = await GetOrCreateTrialPlanoId();

			var trial = new Assinatura
			{
				EmpresaId = empresaId,
				PlanoId = planoTrialId,
				Status = StatusAssinatura.Trial,
				DataInicio = DateTime.UtcNow,
				DataVencimento = DateTime.UtcNow.AddDays(dias <= 0 ? TrialDiasPadrao : dias),
				UltimoStatusMP = "trial"
			};
			await _unitOfWork.Assinaturas.Add(trial);
			_unitOfWork.Save();
			return trial;
		}

		/// <summary>
		/// Garante a existência de um Plano interno usado apenas pelo período de teste (trial).
		/// É oculto dos clientes: <c>Ativo=false</c> (não aparece em /api/planos) e
		/// <c>EmpresaId=null</c> (não aparece em /api/planos/all de nenhuma empresa).
		/// Resolve a FK obrigatória Assinatura→Plano sem precisar de migração de schema.
		/// </summary>
		private async Task<int> GetOrCreateTrialPlanoId()
		{
			var existente = await _unitOfWork.Planos.GetPlanoTrial();
			if (existente != null) return existente.Id;

			var plano = new Plano
			{
				Nome = "Trial Gratuito (interno)",
				Descricao = "Plano interno do período de teste. Não exibido aos clientes.",
				Valor = 0,
				Recorrencia = RecorrenciaPlano.Mensal,
				LimiteGestores = 2,
				LimiteOperadores = 5,
				Ativo = false,
				EmpresaId = null,
				MPPreapprovalPlanId = null
			};
			await _unitOfWork.Planos.Add(plano);
			_unitOfWork.Save();
			return plano.Id;
		}

		/// <summary>
		/// Estado computado do acesso para a empresa, considerando assinatura ativa
		/// e trial vigente (e expirando trial vencido em lazy fashion).
		/// </summary>
		/// <summary>
		/// Acesso liberado fica em memória por alguns segundos por empresa: a verificação roda em
		/// TODA chamada à API (middleware). Só o "liberado" é guardado — quem acabou de pagar não
		/// espera, e mudanças de status limpam a empresa do cache (<see cref="LimparCacheAcesso"/>).
		/// </summary>
		private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, (StatusAcessoAssinatura Status, DateTime Ate)> CacheAcesso = new();
		private static readonly TimeSpan TempoCacheAcesso = TimeSpan.FromSeconds(30);

		public static void LimparCacheAcesso(int empresaId) => CacheAcesso.TryRemove(empresaId, out _);

		public async Task<StatusAcessoAssinatura> GetStatusAcesso(int empresaId)
		{
			if (CacheAcesso.TryGetValue(empresaId, out var cache) && cache.Ate > DateTime.UtcNow)
				return cache.Status;
			var status = await CalcularStatusAcesso(empresaId);
			if (status.Liberado) CacheAcesso[empresaId] = (status, DateTime.UtcNow.Add(TempoCacheAcesso));
			else LimparCacheAcesso(empresaId);
			return status;
		}

		private async Task<StatusAcessoAssinatura> CalcularStatusAcesso(int empresaId)
		{
			var a = await GetAssinaturaAtualParaAcesso(empresaId);
			if (a == null)
				return new StatusAcessoAssinatura { Liberado = false, Estado = "sem_assinatura" };

			switch (a.Status)
			{
				case StatusAssinatura.Ativa:
					return new StatusAcessoAssinatura { Liberado = true, Estado = "ativa", AssinaturaId = a.Id, PlanoId = a.PlanoId, DataVencimento = a.DataVencimento };
				case StatusAssinatura.Trial:
					var diasRestantes = Math.Max(0, (int)Math.Ceiling((a.DataVencimento - DateTime.UtcNow).TotalDays));
					return new StatusAcessoAssinatura { Liberado = true, Estado = "trial", AssinaturaId = a.Id, PlanoId = a.PlanoId, DataVencimento = a.DataVencimento, DiasRestantes = diasRestantes };
				case StatusAssinatura.Pendente:
					return new StatusAcessoAssinatura { Liberado = false, Estado = "pendente", AssinaturaId = a.Id, PlanoId = a.PlanoId, DataVencimento = a.DataVencimento };
				case StatusAssinatura.Suspensa:
					return new StatusAcessoAssinatura { Liberado = false, Estado = "suspensa", AssinaturaId = a.Id, PlanoId = a.PlanoId, DataVencimento = a.DataVencimento };
				case StatusAssinatura.Cancelada:
					return new StatusAcessoAssinatura { Liberado = false, Estado = "cancelada", AssinaturaId = a.Id, PlanoId = a.PlanoId, DataVencimento = a.DataVencimento };
				case StatusAssinatura.Expirada:
					return new StatusAcessoAssinatura { Liberado = false, Estado = "expirada", AssinaturaId = a.Id, PlanoId = a.PlanoId, DataVencimento = a.DataVencimento, DiasRestantes = 0 };
				default:
					return new StatusAcessoAssinatura { Liberado = false, Estado = "desconhecido" };
			}
		}

		private static bool AssinaturaLiberaAcesso(Assinatura assinatura)
		{
			return assinatura.Status == StatusAssinatura.Ativa ||
				(assinatura.Status == StatusAssinatura.Trial && assinatura.DataVencimento >= DateTime.UtcNow);
		}

		private async Task<Assinatura?> GetAssinaturaAtualParaAcesso(int empresaId)
		{
			// Consulta enxuta (sem Empresa/logo nem Plano): só status, vencimento e plano.
			var assinaturas = await _unitOfWork.Assinaturas.GetParaAcesso(empresaId);
			if (!assinaturas.Any()) return null;

			var alterou = false;
			foreach (var trialVencido in assinaturas.Where(a => a.Status == StatusAssinatura.Trial && a.DataVencimento < DateTime.UtcNow))
			{
				var tracked = await _unitOfWork.Assinaturas.GetAssinaturaById(trialVencido.Id);
				if (tracked == null || tracked.Status != StatusAssinatura.Trial) continue;

				tracked.Status = StatusAssinatura.Expirada;
				tracked.UltimoStatusMP = "trial_expired";
				_unitOfWork.Assinaturas.Update(tracked);
				trialVencido.Status = StatusAssinatura.Expirada;
				alterou = true;
			}
			if (alterou) _unitOfWork.Save();

			return assinaturas.FirstOrDefault(a => a.Status == StatusAssinatura.Ativa)
				?? assinaturas.FirstOrDefault(a => a.Status == StatusAssinatura.Trial && a.DataVencimento >= DateTime.UtcNow)
				?? assinaturas.FirstOrDefault();
		}
	}

	public interface IAssinaturaService
	{
		Task<CheckoutAssinaturaResponse> IniciarCheckout(CreateAssinaturaRequest req);
		Task<bool> AtribuirPlanoVitalicio(int empresaId, int planoId);
		Task<AssinaturaDTO?> GetByEmpresaId(int empresaId);
		Task<AssinaturaDTO?> GetById(int id);
		Task<bool> Cancelar(int id);
		Task<bool> Excluir(int id);
		Task<(List<AssinaturaDTO> Items, int Total)> GetAllPaged(int page, int pageSize, int empresaId);
		Task<List<PagamentoAssinaturaDTO>> ListarPagamentos(DateTime? de, DateTime? ate);
		Task<List<PagamentoAssinaturaDTO>> ListarPagamentosDaAssinatura(int assinaturaId);
		Task<LimitesAssinaturaDTO> VerificarLimites(int empresaId);
		Task ProcessarWebhookAssinatura(string mpSubscriptionId);
		Task ProcessarWebhookPagamento(string mpPaymentId);
		Task ProcessarWebhookCobrancaAssinatura(string authorizedPaymentId);
		Task<string?> SincronizarAssinatura(string mpSubscriptionId);
		Task<string?> SincronizarPorId(int assinaturaId);
		Task<CallBackAssinaturaResponse> CallBack(string preapproval_id);
		Task<bool> AtualizarAssinatura(int assinaturaId, decimal? novoValor = null, string? cardToken = null);
		Task<Assinatura> IniciarTrial(int empresaId, int dias = 15);
		Task<StatusAcessoAssinatura> GetStatusAcesso(int empresaId);
	}
}