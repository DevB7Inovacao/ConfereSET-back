using Core.Enums;

namespace Core.DTO
{
    public class CreateTipoOcorrenciaRequest
    {
        public required string Nome { get; set; }
        public string? Descricao { get; set; }
        public int Gravidade { get; set; }
        public TipoOcorrenciaRequisito Requisitos { get; set; }
    }
}