using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class RelacaoGrupoObras : BaseModel
    {
        public int GroupId { get; set; }
        public GrupoDeObras Group { get; set; } = null!;
        public int ObraId { get; set; }
        public Obras Obra { get; set; } = null!;
    }
}
