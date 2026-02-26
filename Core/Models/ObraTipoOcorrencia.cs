namespace Core.Models
{
    public class ObraTipoOcorrencia : BaseModel
    {
        public required int ObraId { get; set; }
        public required int TipoOcorrenciaId { get; set; }
        public Obras? Obra { get; set; }
        public TiposOcorrencia? TipoOcorrencia { get; set; }
    }
}