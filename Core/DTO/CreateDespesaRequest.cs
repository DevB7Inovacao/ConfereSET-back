using System;

namespace Core.DTO
{
    public class CreateDespesaRequest
    {
        public required string Name { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string? Category { get; set; }
        public string? Description { get; set; }
        public int ObraId { get; set; }
    }
}