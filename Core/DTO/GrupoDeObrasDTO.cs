using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
    public class GrupoDeObrasDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int Status { get; set; }
        public List<int> ObrasIds { get; set; } = new();
    }

    public class GrupoDeObrasPagedDTO
    {
        public List<GrupoDeObrasDTO> Result { get; set; } = new();
        public int PageCount { get; set; }
    }
}
