namespace Core.Models
{
    public class ObraChecklist : BaseModel
    {
        public required int ObraId { get; set; }
        public Obras? Obra { get; set; }
        public required int ChecklistId { get; set; }
        public Checklist? Checklist { get; set; }
        public int Status { get; set; } = 1;
        public List<ObraChecklistItem> Itens { get; set; } = new();
    }
}