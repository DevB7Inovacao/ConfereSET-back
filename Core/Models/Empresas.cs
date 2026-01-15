using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class Empresas : BaseModel
    {
        public string Name { get; set; }
        public bool Status { get; set; } //False = inative, True = ative
        public string? CNPJ { get; set; }
    }
}
