using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
    public class FiltersMaoDeObraDTO
    {
        public int? EmpresaId { get; set; }
        public string? Search { get; set; }
        public int? Status { get; set; }
        public int pageNumber { get; set; }
        public int pageSize { get; set; }

        public FiltersMaoDeObraDTO()
        {
            this.pageNumber = 1;
            this.pageSize = 9;
        }
    }
}