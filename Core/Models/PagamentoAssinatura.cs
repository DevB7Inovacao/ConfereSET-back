namespace Core.Models
{
    public class PagamentoAssinatura : BaseModel
    {
        public int AssinaturaId { get; set; }
        public decimal Valor { get; set; }
        public DateTime DataPagamento { get; set; }
        public string? MPPaymentId { get; set; }
        public string Status { get; set; } = string.Empty;
        public Assinatura? Assinatura { get; set; }
    }
}