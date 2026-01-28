using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class ModeloTexto : BaseModel
    {
        public required int EmpresaId { get; set; }
        public required string Nome { get; set; }
        public required string Texto { get; set; }
        public int Status { get; set; } = 1; /// 1 = ativo, 0 = inativo
    }
}
