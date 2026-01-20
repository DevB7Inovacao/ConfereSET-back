using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
    public class CreateObraRequest
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
    }
}
