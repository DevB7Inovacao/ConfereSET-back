using Core.Enums;
using System;

namespace Core.Models
{
    public class TiposOcorrencia : BaseModel
    {
        public required string Nome { get; set; }
        public string? Descricao { get; set; }
        public int Gravidade { get; set; } = 1;
        public TipoOcorrenciaRequisito Requisitos { get; set; } = TipoOcorrenciaRequisito.None;
        public int Status { get; set; } = 1;
    }
}