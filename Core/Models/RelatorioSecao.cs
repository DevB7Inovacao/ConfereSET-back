using Core.Enums;

namespace Core.Models
{
    public class RelatorioSecao : BaseModel
    {
        public int RelatorioId { get; set; }
        public required string DataSecao { get; set; }
        public TipoSecao TipoSecao { get; set; }
        public int Ordem { get; set; } = 0;
        public string? ConteudoJson { get; set; }
        public Relatorio? Relatorio { get; set; }
        public List<RelatorioSecaoItem> Itens { get; set; } = new();
    }
}