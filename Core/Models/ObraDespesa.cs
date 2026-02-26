namespace Core.Models
{
    public class ObraDespesa : BaseModel
    {
        public required int ObraId { get; set; }
        public required int DespesaId { get; set; }
        public Obras? Obra { get; set; }
        public Despesas? Despesa { get; set; }
    }
}