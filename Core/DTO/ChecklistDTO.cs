namespace Core.DTO
{
    public class ChecklistDTO
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public string? Nome { get; set; }
        public int Status { get; set; }
    }

    public class ChecklistPagedDTO
    {
        public int PageCount { get; set; }
        public IList<ChecklistDTO> Result { get; set; } = new List<ChecklistDTO>();
    }
}