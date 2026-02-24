using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
    public class FiltersObrasDTO
    {
        public string? Name { get; set; }
        public int? Status { get; set; }
        public int pageNumber { get; set; } = 1;
        public int pageSize { get; set; } = 10;
        public int? EmpresaId { get; set; }
        public int? OperadorId { get; set; }

        public FiltersObrasDTO()
        {
            this.pageNumber = 1;
            this.pageSize = 10;
        }
    }
}
