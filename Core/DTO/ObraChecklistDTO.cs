namespace Core.DTO
{
    public class ObraChecklistDTO
    {
        public int Id { get; set; }
        public int ObraId { get; set; }
        public int ChecklistId { get; set; }
        public string? ChecklistNome { get; set; }
        public int Status { get; set; }
        public List<ObraChecklistItemDTO> Itens { get; set; } = new();
    }

    public class ObraChecklistItemDTO
    {
        public int Id { get; set; }
        public int ObraChecklistId { get; set; }
        public int ChecklistItemId { get; set; }
        public string? Descricao { get; set; }
        public int Ordem { get; set; }
        /// <summary>0 = pendente, 1 = conforme, 2 = não conforme</summary>
        public int Resposta { get; set; }
        public string? Observacao { get; set; }
    }

    public class AddChecklistToObraRequest
    {
        public required int ObraId { get; set; }
        public required int ChecklistId { get; set; }
    }

    public class ResponderChecklistItemRequest
    {
        /// <summary>0 = pendente, 1 = conforme, 2 = não conforme</summary>
        public int Resposta { get; set; }
        public string? Observacao { get; set; }
    }
}