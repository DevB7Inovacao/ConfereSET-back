using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
    public class FiltersModeloTextoVariavelDTO
    {
        public int? EmpresaId { get; set; }
        public int? Status { get; set; }
        public int? Categoria { get; set; }
        public string? Nome { get; set; }
        public string? NomeAmigavel { get; set; }
        public string? Classe { get; set; }

        public int pageNumber { get; set; }
        public int pageSize { get; set; }

        public FiltersModeloTextoVariavelDTO()
        {
            pageNumber = 1;
            pageSize = 20;
        }
    }
}
