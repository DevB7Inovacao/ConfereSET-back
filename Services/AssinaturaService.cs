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
			_backUrl = configuration["MercadoPago:BackUrl"] ?? "https://confere-set-front.vercel.app";
		}

		public async Task<CheckoutAssinaturaResponse> IniciarCheckout(CreateAssinaturaRequest req)
		{
			try
			{
				var empresa = await _unitOfWork.Empresas.GetEmpresaById(req.EmpresaId)
						?? throw new Exception("Empresa não encontrada.");

				var plano = await _unitOfWork.Planos.GetPlanoById(req.PlanoId)
						?? throw new Exception("Plano não encontrado.");

				if (plano.Valor == 0)
					throw new Exception("Plano vitalício não pode ser contratado via checkout.");

				if (string.IsNullOrWhiteSpace(plano.MPPreapprovalPlanId))
					throw new Exception("Plano não sincronizado com o Mercado Pago.");

				if (string.IsNullOrWhiteSpace(req.PayerEmail))
					throw new Exception("E-mail do pagador é obrigatório.");

				// Bloqueia múltiplas assinaturas ativas concorrentes.
				var existente = await _unitOfWork.Assinaturas.GetAssinaturaAtivaByEmpresaId(req.EmpresaId);
				if (existente != null)
					throw new Exception("Empresa já possui uma assinatura ativa.");

				// Cancela rascunhos pendentes anteriores da mesma empresa sem MPSubscriptionId
				// (poderia ter sido um checkout que o usuário abandonou).
				var pendenteAntiga = await _unitOfWork.Assinaturas.GetPendingEmpresaSemMPId(req.EmpresaId);
				if (pendenteAntiga != null)
				{
					pendenteAntiga.Status = StatusAssinatura.Cancelada;
					pendenteAntiga.UltimoStatusMP = "abandoned";
					_unitOfWork.Assinaturas.Update(pendenteAntiga);
				}

				// External reference único — chave para casar o webhook com a assinatura correta.
				var externalRef = Guid.NewGuid().ToString("N");

				// Cria de fato um PREAPPROVAL no Mercado Pago, vinculado ao plano e à nossa empresa
				// via external_reference. O init_point devolvido aqui é o link específico desta
				// assinatura — diferente do init_point genérico do plano.
				var mpPreapproval = await _mpClient.CreatePreapproval(new MPCreatePreapprovalRequest
				{
					PreapprovalPlanId = plano.MPPreapprovalPlanId!,
					Reason = plano.Nome,
					PayerEmail = req.PayerEmail,
					ExternalReference = externalRef,
					BackUrl = _backUrl,
					Status = "pending",
					AutoRecurring = new MPAutoRecurring
					{
						Frequency = (int)plano.Recorrencia,
						FrequencyType = "months",
						TransactionAmount = plano.Valor,
						CurrencyId = "BRL"
					}
				});

				var assinatura = new Assinatura
				{
					EmpresaId = req.EmpresaId,
					PlanoId = req.PlanoId,
					// Já guardamos o MP id desde a criação — não dependemos de match por plano.
					MPSubscriptionId = mpPreapproval.Id,
					Status = StatusAssinatura.Pendente,
					DataInicio = DateTime.UtcNow,
					DataVencimento = DateTime.UtcNow.AddMonths((int)plano.Recorrencia),
					ExternalReference = externalRef,
					MPPayerEmail = req.PayerEmail,
					UltimoStatusMP = mpPreapproval.Status
				};

				await _unitOfWork.Assinaturas.Add(assinatura);
				_unitOfWork.Save();

				return new CheckoutAssinaturaResponse
				{
					InitPoint = mpPreapproval.InitPoint,
					MPSubscriptionId = mpPreapproval.Id
				};
			}
			catch (Exception)
			{
				throw;
			}
		}

		public async Task<bool> AtribuirPlanoVitalicio(int empresaId, int planoId)
		{
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

			if (assinatura.Plano?.Valor == 0)
				throw new Exception("Assinatura vitalícia não pode ser cancelada.");

			if (!string.IsNullOrWhiteSpace(assinatura.MPSubscriptionId))
				await _mpClient.CancelPreapproval(assinatura.MPSubscriptionId);

			assinatura.Status = StatusAssinatura.Cancelada;
			_unitOfWork.Assinaturas.Update(assinatura);
			return _unitOfWork.Save() > 0;
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
				await _unitOfWork.Users.CountUsersByEmpresaIdAndType(empresaId, TipoGestor) +
				await _unitOfWork.Users.CountUsersByEmpresaIdAndType(empresaId, TipoAdmin);
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
			var mpData = await _mpClient.GetPreapproval(mpSubscriptionId);

			// Estratégia de match (em ordem de robustez):
			// 1) por MPSubscriptionId — funciona quando o IniciarCheckout já criou a assinatura
			//    com o ID retornado pelo MP (caminho moderno).
			// 2) por external_reference — caso o registro local tenha ficado sem MPSubscriptionId
			//    por alguma razão (fluxo legado / corrida na criação).
			//
			// O fallback antigo "primeira pendente do plano" (GetPendingByPlanIdAndNoMPId) foi
			// removido porque era frágil com múltiplos assinantes simultâneos.

			var assinatura = await _unitOfWork.Assinaturas.GetByMPSubscriptionId(mpSubscriptionId);

			if (assinatura == null && !string.IsNullOrWhiteSpace(mpData.ExternalReference))
			{
				assinatura = await _unitOfWork.Assinaturas.GetByExternalReference(mpData.ExternalReference);
			}

			if (assinatura == null) return;

			assinatura.UltimoStatusMP = mpData.Status;
			assinatura.Status = mpData.Status switch
			{
				"authorized" => StatusAssinatura.Ativa,
				"paused" => StatusAssinatura.Suspensa,
				"cancelled" => StatusAssinatura.Cancelada,
				_ => assinatura.Status
			};

			if (assinatura.Status == StatusAssinatura.Ativa && assinatura.Plano != null)
				assinatura.DataVencimento = DateTime.UtcNow.AddMonths((int)assinatura.Plano.Recorrencia);

			// Garante que o MP id esteja salvo (idempotente).
			if (string.IsNullOrWhiteSpace(assinatura.MPSubscriptionId))
				assinatura.MPSubscriptionId = mpSubscriptionId;

			_unitOfWork.Assinaturas.Update(assinatura);
			_unitOfWork.Save();

			// Sincroniza status da empresa com a assinatura: ativar quando autorizada, suspender
			// quando suspensa, manter como está nos demais casos. Não desativamos empresa por
			// "Cancelada" porque o usuário pode estar contratando outro plano.
			if (assinatura.Status == StatusAssinatura.Ativa || assinatura.Status == StatusAssinatura.Suspensa)
			{
				var empresa = await _unitOfWork.Empresas.GetEmpresaById(assinatura.EmpresaId);
				if (empresa != null)
				{
					var novoStatus = assinatura.Status == StatusAssinatura.Ativa;
					if (empresa.Status != novoStatus)
					{
						empresa.Status = novoStatus;
						_unitOfWork.Empresas.Update(empresa);
						_unitOfWork.Save();
					}
				}
			}
		}

		public async Task ProcessarWebhookPagamento(string mpPaymentId)
		{
			var jaProcessado = await _unitOfWork.PagamentosAssinatura.ExistsByMPPaymentId(mpPaymentId);
			if (jaProcessado) return;

			var payment = await _mpClient.GetPayment(mpPaymentId);

			if (string.IsNullOrWhiteSpace(payment.PreapprovalId)) return;

			var assinatura = await _unitOfWork.Assinaturas.GetByMPSubscriptionId(payment.PreapprovalId);
			if (assinatura == null) return;

			var pagamento = new PagamentoAssinatura
			{
				AssinaturaId = assinatura.Id,
				Valor = payment.TransactionAmount,
				DataPagamento = payment.DateApproved ?? DateTime.UtcNow,
				MPPaymentId = mpPaymentId,
				Status = payment.Status
			};

			await _unitOfWork.PagamentosAssinatura.Add(pagamento);
			_unitOfWork.Save();
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
		
			if (string.IsNullOrWhiteSpace(preapproval_id)) return new CallBackAssinaturaResponse { Success = false };
			var preapproval = await _mpClient.GetPreapproval(preapproval_id);
			if (!string.IsNullOrEmpty( preapproval.Status))
			{
				if(preapproval.Status== "authorized")
					return new CallBackAssinaturaResponse { Success = true, Message="Assinatura autorizada!" };
				else
					return new CallBackAssinaturaResponse { Success = false,Message="Assinatura não autorizada." };
			}
			return new CallBackAssinaturaResponse { Message = "Não localizado a assinatura!", Success = true };
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
				updateRequest["auto_recurring"] = new
				{
					transaction_amount = novoValor.Value
				};

				// Atualiza local também (opcional mas recomendado)
				if (assinatura.Plano != null)
					assinatura.Plano.Valor = novoValor.Value;
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
		public async Task<StatusAcessoAssinatura> GetStatusAcesso(int empresaId)
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
			var assinaturas = await _unitOfWork.Assinaturas.GetAllPaged(1, 50, empresaId);
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
		Task<LimitesAssinaturaDTO> VerificarLimites(int empresaId);
		Task ProcessarWebhookAssinatura(string mpSubscriptionId);
		Task ProcessarWebhookPagamento(string mpPaymentId);
		Task<CallBackAssinaturaResponse> CallBack(string preapproval_id);
		Task<bool> AtualizarAssinatura(int assinaturaId, decimal? novoValor = null, string? cardToken = null);
		Task<Assinatura> IniciarTrial(int empresaId, int dias = 15);
		Task<StatusAcessoAssinatura> GetStatusAcesso(int empresaId);
	}
}