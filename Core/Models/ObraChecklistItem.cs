namespace Core.Models
{
    public class ObraChecklistItem : BaseModel
    {
        public required int ObraChecklistId { get; set; }
        public ObraChecklist? ObraChecklist { get; set; }
        public required int ChecklistItemId { get; set; }
        public ChecklistItem? ChecklistItem { get; set; }
        /// <summary>0 = pendente, 1 = conforme, 2 = não conforme</summary>
        public int Resposta { get; set; } = 0;
        public string? Observacao { get; set; }
    }
}