namespace Core.DTO
{
    public class ChecklistItemDTO
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public int ChecklistId { get; set; }
        public string? Descricao { get; set; }
        public int Ordem { get; set; }
        public int Status { get; set; }
    }

    public class CreateChecklistItemRequest
    {
        public required int EmpresaId { get; set; }
        public required int ChecklistId { get; set; }
        public required string Descricao { get; set; }
        public int Ordem { get; set; } = 0;
    }

    public class UpdateChecklistItemRequest
    {
        public string? Descricao { get; set; }
        public int? Ordem { get; set; }
        public int? Status { get; set; }
    }
}