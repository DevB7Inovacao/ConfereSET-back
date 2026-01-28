using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
    public class CreateModeloTextoRequest
    {
        public required int EmpresaId { get; set; }
        public required string Nome { get; set; }
        public required string Texto { get; set; }
    }
}
