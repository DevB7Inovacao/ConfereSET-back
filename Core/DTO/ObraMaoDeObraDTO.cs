namespace Core.DTO
{
    public class ObraMaoDeObraDTO
    {
        public int Id { get; set; }
        public string Funcao { get; set; } = string.Empty;
        public string? Descricao { get; set; }
    }

    public class AddMaoDeObraToObraRequest
    {
        public required int ObraId { get; set; }
        public required int MaoDeObraId { get; set; }
    }

    public class RemoveMaoDeObraFromObraRequest
    {
        public required int ObraId { get; set; }
        public required int MaoDeObraId { get; set; }
    }
}