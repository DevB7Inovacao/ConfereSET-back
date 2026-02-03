using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class ModeloTextoVariavelVinculo : BaseModel
    {
        public required int EmpresaId { get; set; }

        public int ModeloTextoId { get; set; }
        public ModeloTexto? ModeloTexto { get; set; }

        public int ModeloTextoVariavelId { get; set; }
        public ModeloTextoVariavel? ModeloTextoVariavel { get; set; }

        /// <summary>
        /// 1 = ativo, 0 = removido/desvinculado (soft)
        /// </summary>
        public int Status { get; set; } = 1;
    }
}
