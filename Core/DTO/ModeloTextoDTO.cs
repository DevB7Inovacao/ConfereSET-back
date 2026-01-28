using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
    public class ModeloTextoDTO
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public string? Nome { get; set; }
        public string? Texto { get; set; }
        public int Status { get; set; }
    }

    public class ModeloTextoPagedDTO
    {
        public int PageCount { get; set; }
        public IList<ModeloTextoDTO> Result { get; set; } = new List<ModeloTextoDTO>();
    }
}
