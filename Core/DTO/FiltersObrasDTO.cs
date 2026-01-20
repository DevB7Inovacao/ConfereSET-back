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
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public int pageNumber { get; set; }
        public int pageSize { get; set; }

        public FiltersObrasDTO()
        {
            this.pageNumber = 1;
            this.pageSize = 9;
        }
    }
}
