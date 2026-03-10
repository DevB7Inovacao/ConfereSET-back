namespace Core.Models
{
    public class RelatorioItemFoto : BaseModel
    {
        public required int RelatorioSecaoItemId { get; set; }
        public required byte[] ImagemBytes { get; set; }
        public required string ContentType { get; set; }
        public string? NomeArquivo { get; set; }
        public RelatorioSecaoItem? RelatorioSecaoItem { get; set; }
    }
}