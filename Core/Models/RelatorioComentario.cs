namespace Core.Models
{
    public class RelatorioComentario : BaseModel
    {
        public int RelatorioSecaoId { get; set; }
        public required int AutorId { get; set; }
        public required string Texto { get; set; }
        public RelatorioSecao? RelatorioSecao { get; set; }
        public User? Autor { get; set; }
    }
}