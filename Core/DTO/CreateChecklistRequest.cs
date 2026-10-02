namespace Core.DTO
{
    public class CreateChecklistRequest
    {
        public required int EmpresaId { get; set; }
        public required string Nome { get; set; }
        // [Conferelist v2]
        public string? Descricao { get; set; }
        public string? Categoria { get; set; }
    }
}