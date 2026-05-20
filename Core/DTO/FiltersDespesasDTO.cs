using System;

namespace Core.DTO
{
    public class FiltersDespesasDTO
    {
        public int? EmpresaId { get; set; }
        public string? Name { get; set; }
        public int? Status { get; set; }
        public int? ObraId { get; set; }
        public string? Category { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int pageNumber { get; set; }
        public int pageSize { get; set; }

        public FiltersDespesasDTO()
        {
            this.pageNumber = 1;
            this.pageSize = 9;
        }
    }
}