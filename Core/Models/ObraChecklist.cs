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
        // [Conferelist v2] Execução (rodada) do checklist na obra.
        public string? Titulo { get; set; }
        public DateTime? ConcluidoEm { get; set; }
        public int? ConcluidoPorUserId { get; set; }
        public string? ResponsavelNome { get; set; }
        public string? AssinaturaUrl { get; set; }
        public string? ObservacaoGeral { get; set; }
    }
}