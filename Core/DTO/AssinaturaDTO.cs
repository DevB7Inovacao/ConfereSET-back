using Core.Enums;

namespace Core.DTO
{
    public class AssinaturaDTO
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public string? EmpresaNome { get; set; }
        public int PlanoId { get; set; }
        public string? PlanoNome { get; set; }
        public decimal PlanoValor { get; set; }
        public StatusAssinatura Status { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataVencimento { get; set; }
        public string? MPSubscriptionId { get; set; }
        public string? MPPayerEmail { get; set; }
    }

    public class PagamentoAssinaturaDTO
    {
        public int Id { get; set; }
        public int AssinaturaId { get; set; }
        public int EmpresaId { get; set; }
        public string? EmpresaNome { get; set; }
        public string? PlanoNome { get; set; }
        public decimal Valor { get; set; }
        public DateTime DataPagamento { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? MPPaymentId { get; set; }
    }

    /// <summary>
    /// Contratação de plano. Com <see cref="Token"/> (card_token_id gerado pelo formulário de cartão
    /// do Mercado Pago) a assinatura já nasce autorizada; sem token, o checkout devolve o link do
    /// Mercado Pago (init_point) para a pessoa pagar lá.
    /// </summary>
    public class CreateAssinaturaRequest
    {
		public int EmpresaId { get; set; }
		public required int PlanoId { get; set; }
		public required string PayerEmail { get; set; }
		public string? PayerFirstName { get; set; }
		public string? PayerLastName { get; set; }
		public string? Token { get; set; } // card_token_id
		public string? PaymentMethodId { get; set; }
		public int? Installments { get; set; }
		public decimal? TransactionAmount { get; set; }
	}

    public class CheckoutAssinaturaResponse
    {
        public int AssinaturaId { get; set; }
        /// <summary>Link de pagamento do Mercado Pago (fluxo sem cartão). Vazio quando já autorizada.</summary>
        public string InitPoint { get; set; } = string.Empty;
        public string MPSubscriptionId { get; set; } = string.Empty;
        /// <summary>Status no Mercado Pago: authorized, pending, paused, cancelled.</summary>
        public string Status { get; set; } = string.Empty;
        /// <summary>true = pagamento autorizado e acesso já liberado.</summary>
        public bool Ativa { get; set; }
    }

  public class CallBackAssinaturaResponse
  {
		public bool Success { get; set; }
		public string? Message { get; set; }
		/// <summary>authorized | pending | paused | cancelled | not_found</summary>
		public string? Status { get; set; }
	}


		public class AtribuirPlanoVitalicioRequest
    {
        public required int EmpresaId { get; set; }
        public required int PlanoId { get; set; }
    }

    public class LimitesAssinaturaDTO
    {
        public bool AssinaturaAtiva { get; set; }
        public int LimiteGestores { get; set; }
        public int LimiteOperadores { get; set; }
        public int GestoresUtilizados { get; set; }
        public int OperadoresUtilizados { get; set; }
        public bool PodeAdicionarGestor { get; set; }
        public bool PodeAdicionarOperador { get; set; }
    }
}