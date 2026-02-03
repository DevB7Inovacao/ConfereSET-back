using Core.Enums;

namespace Core.DTO
{
    public class UpdateTipoOcorrenciaRequest
    {
        public string? Nome { get; set; }
        public string? Descricao { get; set; }
        public int? Gravidade { get; set; }
        public TipoOcorrenciaRequisito? Requisitos { get; set; }
        public int? Status { get; set; }
    }
}