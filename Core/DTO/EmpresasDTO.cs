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
        public string? TradeName { get; set; }
        public string? AppName { get; set; }
        public string? LogoBase64 { get; set; }
        public string? LogoContentType { get; set; }
        public string? ContactEmail { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }

    public class EmpresasPagedDTO
    {
        public int PageCount { get; set; }
        public IList<EmpresasDTO> Result { get; set; }

    }
}
