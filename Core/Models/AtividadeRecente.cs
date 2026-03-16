using Core.Enums;

namespace Core.Models
{
    public class AtividadeRecente : BaseModel
    {
        public required int OperadorId { get; set; }
        public required TipoAtividade Tipo { get; set; }
        public required string Descricao { get; set; }
        public int? ObraId { get; set; }
        public int? ReferenciaId { get; set; }
        public User? Operador { get; set; }
        public Obras? Obra { get; set; }
    }
}