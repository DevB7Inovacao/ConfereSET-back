using Core.Enums;

namespace Core.DTO
{
	public class TiposOcorrenciaDTO
	{
		public int Id { get; set; }
		public string? Nome { get; set; }
		public string? Descricao { get; set; }
		public int? Gravidade { get; set; }
		public TipoOcorrenciaRequisito Requisitos { get; set; }
		public int? Status { get; set; }
	}

	public class TiposOcorrenciaPagedDTO
	{
		public int PageCount { get; set; }
		public IList<TiposOcorrenciaDTO>? Result { get; set; }
	}
}