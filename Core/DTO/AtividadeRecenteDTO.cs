using Core.Enums;

namespace Core.DTO
{
    public class AtividadeRecenteDTO
    {
        public int Id { get; set; }
        public int OperadorId { get; set; }
        public string? OperadorNome { get; set; }
        public TipoAtividade Tipo { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public int? ObraId { get; set; }
        public string? ObraNome { get; set; }
        public int? ReferenciaId { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class AtividadeRecentePagedDTO
    {
        public int PageCount { get; set; }
        public IList<AtividadeRecenteDTO> Result { get; set; } = new List<AtividadeRecenteDTO>();
    }

    public class FiltersAtividadeRecenteDTO
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}