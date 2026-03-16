namespace Core.Models
{
    public class ChecklistItem : BaseModel
    {
        public required int EmpresaId { get; set; }
        public required int ChecklistId { get; set; }
        public Checklist? Checklist { get; set; }
        public required string Descricao { get; set; }
        public int Ordem { get; set; } = 0;
        public int Status { get; set; } = 1;
    }
}