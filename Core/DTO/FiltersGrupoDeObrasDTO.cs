using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
    public class FiltersGrupoDeObrasDTO
    {
        public string? Name { get; set; }
        public int? Status { get; set; }
        public int pageNumber { get; set; }
        public int pageSize { get; set; }

        public FiltersGrupoDeObrasDTO()
        {
            pageNumber = 1;
            pageSize = 9;
        }
    }
}
