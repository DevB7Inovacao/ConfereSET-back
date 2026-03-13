using Core.Enums;

namespace Core.Models
{
    public class Relatorio : BaseModel
    {
        public required int ModeloTextoId { get; set; }
        public required int ObraId { get; set; }
        public required int CriadoPorUserId { get; set; }
        public required string Titulo { get; set; }
        public StatusRelatorio Status { get; set; } = StatusRelatorio.Rascunho;
        public DateTime DataRelatorio { get; set; } = DateTime.Now;
        public string? HtmlSnapshot { get; set; }
        public string? ObservacaoRejeicao { get; set; }
        public ModeloTexto? ModeloTexto { get; set; }
        public Obras? Obra { get; set; }
        public User? CriadoPor { get; set; }
        public List<RelatorioSecao> Secoes { get; set; } = new();
    }
}