namespace Core.Models
{
    public class ObraEquipamento : BaseModel
    {
        public required int ObraId { get; set; }
        public required int EquipamentoId { get; set; }
        public Obras? Obra { get; set; }
        public Equipamentos? Equipamento { get; set; }
    }
}