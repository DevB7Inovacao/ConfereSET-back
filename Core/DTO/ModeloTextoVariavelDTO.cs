using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
    public class ModeloTextoVariavelDTO
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public string? Nome { get; set; }
        public string? NomeAmigavel { get; set; }
        public string? NomePropriedade { get; set; }
        public int Categoria { get; set; }
        public string? Classe { get; set; }
        public string? Valor { get; set; }
        public int Status { get; set; }
    }

    public class ModeloTextoVariavelPagedDTO
    {
        public int PageCount { get; set; }
        public IList<ModeloTextoVariavelDTO> Result { get; set; } = new List<ModeloTextoVariavelDTO>();
    }
}
