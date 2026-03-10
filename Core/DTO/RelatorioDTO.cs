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
        public int CriadoPorUserId { get; set; }
        public string? CriadoPorNome { get; set; }
        public string? Titulo { get; set; }
        public StatusRelatorio Status { get; set; }
        public DateTime DataRelatorio { get; set; }
        public string? HtmlSnapshot { get; set; }
        public List<RelatorioSecaoDTO> Secoes { get; set; } = new();
    }

    public class RelatorioSecaoDTO
    {
        public int Id { get; set; }
        public int RelatorioId { get; set; }
        public string? DataSecao { get; set; }
        public TipoSecao TipoSecao { get; set; }
        public int Ordem { get; set; }
        public string? ConteudoJson { get; set; }
        public List<RelatorioSecaoItemDTO> Itens { get; set; } = new();
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