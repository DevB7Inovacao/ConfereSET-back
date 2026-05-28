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
        public int? TipoOcorrenciaId { get; set; }
        // [v2] Título customizável da seção (ex.: "Equipamentos pesados — turno noite").
        // Quando null/vazio, a UI usa o label padrão do TipoSecao.
        public string? Titulo { get; set; }
        public Relatorio? Relatorio { get; set; }
        public TiposOcorrencia? TipoOcorrencia { get; set; }
        public List<RelatorioSecaoItem> Itens { get; set; } = new();
        public List<RelatorioComentario> Comentarios { get; set; } = new();
    }
}