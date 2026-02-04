using System;
using System.Collections.Generic;

namespace Core.DTO
{
    public class DespesaDTO
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string? Category { get; set; }
        public string? Description { get; set; }
        public int ObraId { get; set; }
        public int Status { get; set; }
    }

    public class DespesasPagedDTO
    {
        public int PageCount { get; set; }
        public IList<DespesaDTO> Result { get; set; }
    }
}