using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class GrupoDeObras : BaseModel
    {
        public required string Name { get; set; }
        public int Status { get; set; } = 0;
        public ICollection<RelacaoGrupoObras> Obras { get; set; } = new List<RelacaoGrupoObras>();
    }
}