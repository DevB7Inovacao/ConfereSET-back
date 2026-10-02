namespace Core.Models
{
    public class Checklist : BaseModel
    {
        public required int EmpresaId { get; set; }
        public required string Nome { get; set; }
        public int Status { get; set; } = 1;
        // [Conferelist v2]
        public string? Descricao { get; set; }
        public string? Categoria { get; set; }
    }
}