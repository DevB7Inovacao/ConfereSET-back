using Core.Enums;

namespace Core.DTO
{
    public class PlanoDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public decimal Valor { get; set; }
        public RecorrenciaPlano Recorrencia { get; set; }
        public int LimiteGestores { get; set; }
        public int LimiteOperadores { get; set; }
        public bool Ativo { get; set; }
        public string? MPPreapprovalPlanId { get; set; }
    }

    public class CreatePlanoRequest
    {
        public required string Nome { get; set; }
        public string? Descricao { get; set; }
        public decimal Valor { get; set; }
        public RecorrenciaPlano Recorrencia { get; set; } = RecorrenciaPlano.Mensal;
        public int LimiteGestores { get; set; }
        public int LimiteOperadores { get; set; }
		public int EmpresaId { get; set; }
	}

    public class UpdatePlanoRequest
    {
        public string? Nome { get; set; }
        public string? Descricao { get; set; }
        public decimal? Valor { get; set; }
        public RecorrenciaPlano? Recorrencia { get; set; }
        public int? LimiteGestores { get; set; }
        public int? LimiteOperadores { get; set; }
        public bool? Ativo { get; set; }
    }
}