using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class User : BaseModel
    {
        public required string Name { get; set; }
        public required string Password { get; set; }
        public required string Email { get; set; }
        public required int Type { get; set; }
        public required int Status { get; set; }
        public required Empresas Empresa { get; set; }
    }
}
