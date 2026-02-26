namespace Core.Models
{
    public class ObraModeloTexto : BaseModel
    {
        public required int ObraId { get; set; }
        public required int ModeloTextoId { get; set; }
        public Obras? Obra { get; set; }
        public ModeloTexto? ModeloTexto { get; set; }
    }
}