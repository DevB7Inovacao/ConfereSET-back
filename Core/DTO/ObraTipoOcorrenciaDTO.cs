namespace Core.DTO
{
    public class ObraTipoOcorrenciaDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public int Gravidade { get; set; }
    }

    public class AddTipoOcorrenciaToObraRequest
    {
        public required int ObraId { get; set; }
        public required int TipoOcorrenciaId { get; set; }
    }

    public class RemoveTipoOcorrenciaFromObraRequest
    {
        public required int ObraId { get; set; }
        public required int TipoOcorrenciaId { get; set; }
    }
}