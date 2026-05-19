namespace Core.DTO
{
    public class StatusAcessoAssinatura
    {
        public bool Liberado { get; set; }
        public string Estado { get; set; } = "desconhecido";
        public int? AssinaturaId { get; set; }
        public int? PlanoId { get; set; }
        public DateTime? DataVencimento { get; set; }
        public int? DiasRestantes { get; set; }
    }
}
