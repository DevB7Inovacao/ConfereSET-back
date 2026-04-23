using Core.Enums;

namespace Core.Models
{
    public class Plano : BaseModel
    {
        public required string Nome { get; set; }
        public string? Descricao { get; set; }
        public decimal Valor { get; set; }
        public RecorrenciaPlano Recorrencia { get; set; } = RecorrenciaPlano.Mensal;
        public int LimiteGestores { get; set; }
        public int LimiteOperadores { get; set; }
        public bool Ativo { get; set; } = true;
        public string? MPPreapprovalPlanId { get; set; }
		public int? EmpresaId { get; set; }
		public Empresas? Empresa { get; set; }
	}
}