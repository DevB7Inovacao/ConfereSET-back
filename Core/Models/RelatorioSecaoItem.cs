using System.Collections.Generic;

namespace Core.Models
{
    public class RelatorioSecaoItem : BaseModel
    {
        public int RelatorioSecaoId { get; set; }
        public int? ReferenciaId { get; set; }
        public string? Nome { get; set; }
        public string? Descricao { get; set; }
        public RelatorioSecao? RelatorioSecao { get; set; }
        public List<RelatorioItemFoto> Fotos { get; set; } = new();
    }
}