using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
    public class UpdateModeloTextoRequest
    {
        public string? Nome { get; set; }
        public string? Texto { get; set; }
        public int? Status { get; set; }
    }
}
