using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
    public class FiltersModeloTextoDTO
    {
        public int? EmpresaId { get; set; }
        public int? Status { get; set; }
        public string? Nome { get; set; }

        public int pageNumber { get; set; }
        public int pageSize { get; set; }

        public FiltersModeloTextoDTO()
        {
            pageNumber = 1;
            pageSize = 10;
        }
    }
}
