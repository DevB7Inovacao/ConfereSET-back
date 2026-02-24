namespace Core.Models
{
    public class ObraOperador : BaseModel
    {
        public required int ObraId { get; set; }
        public required int OperadorId { get; set; }
        public Obras? Obra { get; set; }
        public User? Operador { get; set; }
    }
}