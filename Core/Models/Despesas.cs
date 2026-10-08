using System;
using System.Collections.Generic;

namespace Core.Models
{
    public class Despesas : BaseModel
    {
        public required string Name { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string? Category { get; set; }
        public string? Description { get; set; }
        public int ObraId { get; set; }
        public int EmpresaId { get; set; }
        public int Status { get; set; } = 0;
        public ICollection<DespesaComprovante> Comprovantes { get; set; } = new List<DespesaComprovante>();
    }
}