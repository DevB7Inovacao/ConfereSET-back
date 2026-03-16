namespace Core.DTO
{
    public class CreateChecklistRequest
    {
        public required int EmpresaId { get; set; }
        public required string Nome { get; set; }
    }
}