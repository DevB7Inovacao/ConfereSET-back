namespace Core.DTO
{
    public class TipoOcorrenciaSimpleDTO
    {
        public int Id { get; set; }
        public string Nome { get; set; } = null!;
        public string? Descricao { get; set; }
    }
}