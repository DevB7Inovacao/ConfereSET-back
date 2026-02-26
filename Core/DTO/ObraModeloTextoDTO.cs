namespace Core.DTO
{
    public class ObraModeloTextoDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Texto { get; set; }
        public int Status { get; set; }
    }

    public class AddModeloTextoToObraRequest
    {
        public required int ObraId { get; set; }
        public required int ModeloTextoId { get; set; }
    }

    public class RemoveModeloTextoFromObraRequest
    {
        public required int ObraId { get; set; }
        public required int ModeloTextoId { get; set; }
    }
}