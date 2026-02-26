namespace Core.Models
{
    public class ObraMaoDeObra : BaseModel
    {
        public required int ObraId { get; set; }
        public required int MaoDeObraId { get; set; }
        public Obras? Obra { get; set; }
        public MaoDeObra? MaoDeObra { get; set; }
    }
}