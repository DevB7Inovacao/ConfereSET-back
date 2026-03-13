using Core.Enums;

namespace Core.Models
{
    public class Ocorrencia : BaseModel
    {
        public required int ObraId { get; set; }
        public required int TipoOcorrenciaId { get; set; }
        public required string Titulo { get; set; }
        public string? Descricao { get; set; }
        public string? Localizacao { get; set; }
        public StatusOcorrencia Status { get; set; } = StatusOcorrencia.Aberta;
        public DateTime DataOcorrencia { get; set; } = DateTime.Now;
        public int? CriadoPorUserId { get; set; }
        public Obras? Obra { get; set; }
        public TiposOcorrencia? TipoOcorrencia { get; set; }
        public User? CriadoPor { get; set; }
    }
}