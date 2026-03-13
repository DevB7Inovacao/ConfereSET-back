using Core.Enums;

namespace Core.DTO
{
    public class OcorrenciaDTO
    {
        public int Id { get; set; }
        public int ObraId { get; set; }
        public string? ObraNome { get; set; }
        public int TipoOcorrenciaId { get; set; }
        public string? TipoOcorrenciaNome { get; set; }
        public int TipoOcorrenciaGravidade { get; set; }
        public string? Titulo { get; set; }
        public string? Descricao { get; set; }
        public string? Localizacao { get; set; }
        public StatusOcorrencia Status { get; set; }
        public DateTime DataOcorrencia { get; set; }
        public int? CriadoPorUserId { get; set; }
        public string? CriadoPorNome { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class OcorrenciaPagedDTO
    {
        public int PageCount { get; set; }
        public IList<OcorrenciaDTO> Result { get; set; } = new List<OcorrenciaDTO>();
    }

    public class CreateOcorrenciaRequest
    {
        public required int ObraId { get; set; }
        public required int TipoOcorrenciaId { get; set; }
        public required string Titulo { get; set; }
        public string? Descricao { get; set; }
        public string? Localizacao { get; set; }
        public DateTime? DataOcorrencia { get; set; }
        public int? CriadoPorUserId { get; set; }
    }

    public class UpdateOcorrenciaRequest
    {
        public int? TipoOcorrenciaId { get; set; }
        public string? Titulo { get; set; }
        public string? Descricao { get; set; }
        public string? Localizacao { get; set; }
        public StatusOcorrencia? Status { get; set; }
        public DateTime? DataOcorrencia { get; set; }
    }

    public class FiltersOcorrenciaDTO
    {
        public int? ObraId { get; set; }
        public int? EmpresaId { get; set; }
        public int? CriadoPorUserId { get; set; }
        public StatusOcorrencia? Status { get; set; }
        public int? TipoOcorrenciaId { get; set; }
        public string? Search { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class UpdateOcorrenciaStatusRequest
    {
        public required StatusOcorrencia Status { get; set; }
    }
}