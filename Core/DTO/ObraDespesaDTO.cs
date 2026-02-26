namespace Core.DTO
{
    public class ObraDespesaDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string? Category { get; set; }
        public string? Description { get; set; }
        public int Status { get; set; }
    }

    public class AddDespesaToObraRequest
    {
        public required int ObraId { get; set; }
        public required int DespesaId { get; set; }
    }

    public class RemoveDespesaFromObraRequest
    {
        public required int ObraId { get; set; }
        public required int DespesaId { get; set; }
    }
}