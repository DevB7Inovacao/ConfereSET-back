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

    public class CreateAssinaturaRequest
    {
		public required int EmpresaId { get; set; }
		public required int PlanoId { get; set; }
		public required string PayerEmail { get; set; }
		public required string PayerFirstName { get; set; }
		public required string PayerLastName { get; set; }
		public required string Token { get; set; } // card_token_id
		public required string PaymentMethodId { get; set; }
		public required int Installments { get; set; }
		public required decimal TransactionAmount { get; set; }
	}

    public class CheckoutAssinaturaResponse
    {
        public string InitPoint { get; set; } = string.Empty;
        public string MPSubscriptionId { get; set; } = string.Empty;
    }

  public class CallBackAssinaturaResponse
  {
		public bool Success { get; set; }
		public string? Message { get; set; }
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

    public class PagamentoAssinaturaDTO
    {
        public int Id { get; set; }
        public int AssinaturaId { get; set; }
        public decimal Valor { get; set; }
        public DateTime DataPagamento { get; set; }
        public string? MPPaymentId { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}