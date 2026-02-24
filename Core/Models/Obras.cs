using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class Obras : BaseModel
    {
        public required string Name { get; set; }
        public string? StreetAddress { get; set; }
        public string? Number { get; set; }
        public string? AddressLine2 { get; set; }
        public string? Neighborhood { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? ClientName { get; set; }
        public string? ClientEmail { get; set; }
        public string? ClientPhone { get; set; }
        public string? ClientDocument { get; set; }
        public int Status { get; set; } = 0;
        public int? EmpresaId { get; set; }
        public Empresas? Empresa { get; set; }
        public DateTime? StartDate { get; set; }
        public int ProgressPercentage { get; set; } = 0;
    }
}