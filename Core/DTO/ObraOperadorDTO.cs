namespace Core.DTO
{
    public class ObraOperadorDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class AddOperadorToObraRequest
    {
        public required int ObraId { get; set; }
        public required int OperadorId { get; set; }
    }

    public class RemoveOperadorFromObraRequest
    {
        public required int ObraId { get; set; }
        public required int OperadorId { get; set; }
    }

    public class ObraWithOperadoresDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Status { get; set; }
        public string? StreetAddress { get; set; }
        public string? Number { get; set; }
        public string? AddressLine2 { get; set; }
        public string? Neighborhood { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? ClientName { get; set; }
        public string? ClientEmail { get; set; }
        public string? ClientPhone { get; set; }
        public string? ClientDocument { get; set; }
        public List<ObraOperadorDTO> Operadores { get; set; } = new();
    }
}