using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class Equipamentos : BaseModel
    {
        public required string Nome { get; set; }
        public string? Descricao { get; set; }
        public int Status { get; set; } = 1;
    }
}