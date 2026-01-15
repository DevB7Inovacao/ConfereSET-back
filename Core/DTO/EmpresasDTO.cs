using Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
    public class EmpresasDTO
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool? Status { get; set; }
        public string? CNPJ { get; set; }
    }

    public class EmpresasPagedDTO
    {
        public int PageCount { get; set; }
        public IList<EmpresasDTO> Result { get; set; }

    }
}
