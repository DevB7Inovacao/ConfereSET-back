namespace Core.DTO
{
    public class ObraEquipamentoDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
    }

    public class AddEquipamentoToObraRequest
    {
        public required int ObraId { get; set; }
        public required int EquipamentoId { get; set; }
    }

    public class RemoveEquipamentoFromObraRequest
    {
        public required int ObraId { get; set; }
        public required int EquipamentoId { get; set; }
    }
}