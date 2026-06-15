using Core.Enums;

namespace Core.DTO
{
	public class RelatorioDTO
	{
		public int Id { get; set; }
		public int ModeloTextoId { get; set; }
		public string? ModeloTextoNome { get; set; }
		public int ObraId { get; set; }
		public string? ObraNome { get; set; }
		public string? ObraStreetAddress { get; set; }
		public string? ObraNumber { get; set; }
		public string? ObraAddressLine2 { get; set; }
		public string? ObraNeighborhood { get; set; }
		public string? ObraCity { get; set; }
		public string? ObraState { get; set; }
		public string? ObraPostalCode { get; set; }
		public string? ObraCountry { get; set; }
		public string? ObraClientName { get; set; }
		public string? ObraClientEmail { get; set; }
		public string? ObraClientPhone { get; set; }
		public int CriadoPorUserId { get; set; }
		public string? CriadoPorNome { get; set; }
		public string? Titulo { get; set; }
		public StatusRelatorio Status { get; set; }
		public DateTime DataRelatorio { get; set; }
		public string? HtmlSnapshot { get; set; }
		public string? ObservacaoRejeicao { get; set; }
		public List<RelatorioSecaoDTO> Secoes { get; set; } = new();
		public string? EmpresaNome { get; set; }
		public string? EmpresaTelefone { get; set; }
		public string? EmpresaEmail { get; set; }
		public string? EmpresaLogoBase64 { get; set; }
		public string? EmpresaLogoContentType { get; set; }
	}

	public class RelatorioSecaoDTO
	{
		public int Id { get; set; }
		public int RelatorioId { get; set; }
		public string? DataSecao { get; set; }
		public TipoSecao TipoSecao { get; set; }
		public int Ordem { get; set; }
		public string? ConteudoJson { get; set; }
		public int? TipoOcorrenciaId { get; set; }
		public string? TipoOcorrenciaNome { get; set; }
		// [v2] Título customizável por seção.
		public string? Titulo { get; set; }
		public List<RelatorioSecaoItemDTO> Itens { get; set; } = new();
		public List<RelatorioComentarioDTO> Comentarios { get; set; } = new();
	}

	public class RelatorioSecaoItemDTO
	{
		public int Id { get; set; }
		public int RelatorioSecaoId { get; set; }
		public int? ReferenciaId { get; set; }
		public string? Nome { get; set; }
		public string? Descricao { get; set; }
		public List<RelatorioItemFotoDTO> Fotos { get; set; } = new();
	}

	public class RelatorioItemFotoDTO
	{
		public int Id { get; set; }
		public int RelatorioSecaoItemId { get; set; }
		public string? ContentType { get; set; }
		public string? NomeArquivo { get; set; }
		public string? ImagemBase64 { get; set; }
		public string S3Url { get; set; } = string.Empty;
	}

	public class RelatorioComentarioDTO
	{
		public int Id { get; set; }
		public int RelatorioSecaoId { get; set; }
		public int AutorId { get; set; }
		public string? AutorNome { get; set; }
		public string? Texto { get; set; }
		public DateTime CreatedDate { get; set; }
	}

	public class AddComentarioRequest
	{
		public required int AutorId { get; set; }
		public required string Texto { get; set; }
	}

	public class UpdateComentarioRequest
	{
		public required string Texto { get; set; }
	}

	public class RelatorioPagedDTO
	{
		public int PageCount { get; set; }
		public IList<RelatorioDTO> Result { get; set; } = new List<RelatorioDTO>();
	}

	public class CreateRelatorioRequest
	{
		public required int ModeloTextoId { get; set; }
		public required int ObraId { get; set; }
		public required int CriadoPorUserId { get; set; }
		public required string Titulo { get; set; }
		public DateTime? DataRelatorio { get; set; }
	}

	public class UpdateRelatorioSecaoItemRequest
	{
		public int? ReferenciaId { get; set; }
		public string? Descricao { get; set; }
	}

	public class AddFotoToItemRequest
	{
		public required string ImagemBase64 { get; set; }
		public required string ContentType { get; set; }
		public string? NomeArquivo { get; set; }
	}

	public class UpdateRelatorioStatusRequest
	{
		public required StatusRelatorio Status { get; set; }
		public string? ObservacaoRejeicao { get; set; }
	}

	public class FiltersRelatorioDTO
	{
		public int? ObraId { get; set; }
		public int? EmpresaId { get; set; }
		public int? CriadoPorUserId { get; set; }
		public StatusRelatorio? Status { get; set; }
		public int PageNumber { get; set; } = 1;
		public int PageSize { get; set; } = 10;
	}
}