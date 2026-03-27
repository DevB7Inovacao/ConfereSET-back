using Core.Enums;

namespace Core.Models
{
    public class Assinatura : BaseModel
    {
        public int EmpresaId { get; set; }
        public int PlanoId { get; set; }
        public StatusAssinatura Status { get; set; } = StatusAssinatura.Pendente;
        public DateTime DataInicio { get; set; }
        public DateTime DataVencimento { get; set; }
        public string? MPSubscriptionId { get; set; }
        public string? MPPayerEmail { get; set; }
        public string? UltimoStatusMP { get; set; }
        public Empresas? Empresa { get; set; }
        public Plano? Plano { get; set; }
        public List<PagamentoAssinatura> Pagamentos { get; set; } = new();
    }
}