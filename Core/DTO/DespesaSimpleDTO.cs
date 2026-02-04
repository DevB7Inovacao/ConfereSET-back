using System;

namespace Core.DTO
{
    public class DespesaSimpleDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public int ObraId { get; set; }
    }
}