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

		private const int TipoGestor = (int)TypeUser.gerente;
		private const int TipoOperador = (int)TypeUser.operador;

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

				var existente = await _unitOfWork.Assinaturas.GetAssinaturaAtivaByEmpresaId(req.EmpresaId);
				if (existente != null)
					throw new Exception("Empresa já possui uma assinatura ativa.");

				var mpResponsePlan = await _mpClient.GetPreapprovalPlan(plano.MPPreapprovalPlanId);

				var externalRef = Guid.NewGuid().ToString();

				var assinatura = new Assinatura
				{
					EmpresaId = req.EmpresaId,
					PlanoId = req.PlanoId,
					MPSubscriptionId= null,
					Status = StatusAssinatura.Pendente,
					DataInicio = DateTime.UtcNow,
					DataVencimento = DateTime.UtcNow.AddMonths((int)plano.Recorrencia),
					ExternalReference = externalRef,
					MPPayerEmail = req.PayerEmail,
					UltimoStatusMP = mpResponsePlan.Status
				};

				await _unitOfWork.Assinaturas.Add(assinatura);
				_unitOfWork.Save();

				var initPoint = mpResponsePlan.InitPoint;

				return new CheckoutAssinaturaResponse
				{
					InitPoint = initPoint,
					MPSubscriptionId = mpResponsePlan.Id
				};
			}
			catch (Exception ex)
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
			var assinatura = await _unitOfWork.Assinaturas.GetAssinaturaAtivaByEmpresaId(empresaId);
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
			var items = await _unitOfWork.Assinaturas.GetAllPaged(page, pageSize, empresaId);
			var total = await _unitOfWork.Assinaturas.CountAll();
			return (items.Select(MapToDTO).ToList(), total);
		}

		public async Task<LimitesAssinaturaDTO> VerificarLimites(int empresaId)
		{
			var assinatura = await _unitOfWork.Assinaturas.GetAssinaturaAtivaByEmpresaId(empresaId);

			if (assinatura?.Plano == null)
				return new LimitesAssinaturaDTO { AssinaturaAtiva = false };

			var totalGestores = await _unitOfWork.Users.CountUsersByEmpresaIdAndType(empresaId, TipoGestor);
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

			// ESTRATÉGIA: Buscar assinatura pendente pelo MPPreapprovalPlanId
			// e que não tenha sido associada ainda
			var assinatura = await _unitOfWork.Assinaturas.GetPendingByPlanIdAndNoMPId(mpData.PreapprovalPlanId);

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
			assinatura.MPSubscriptionId = mpSubscriptionId;
			
			_unitOfWork.Assinaturas.Update(assinatura);
			_unitOfWork.Save();
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
	}

	public interface IAssinaturaService
	{
		Task<CheckoutAssinaturaResponse> IniciarCheckout(CreateAssinaturaRequest req);
		Task<bool> AtribuirPlanoVitalicio(int empresaId, int planoId);
		Task<AssinaturaDTO?> GetByEmpresaId(int empresaId);
		Task<AssinaturaDTO?> GetById(int id);
		Task<bool> Cancelar(int id);
		Task<(List<AssinaturaDTO> Items, int Total)> GetAllPaged(int page, int pageSize, int empresaId);
		Task<LimitesAssinaturaDTO> VerificarLimites(int empresaId);
		Task ProcessarWebhookAssinatura(string mpSubscriptionId);
		Task ProcessarWebhookPagamento(string mpPaymentId);
		Task<CallBackAssinaturaResponse> CallBack(string preapproval_id);
		Task<bool> AtualizarAssinatura(int assinaturaId, decimal? novoValor = null, string? cardToken = null);
	}
}