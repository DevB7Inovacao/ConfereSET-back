using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class MaoDeObra : BaseModel
    {
        public int EmpresaId { get; set; }
        public required string Funcao { get; set; }
        public string? Descricao { get; set; }
        public int Status { get; set; } = 1;
    }
}