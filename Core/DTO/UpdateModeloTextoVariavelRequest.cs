using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
    public class UpdateModeloTextoVariavelRequest
    {
        public string? Nome { get; set; }
        public string? NomeAmigavel { get; set; }
        public string? NomePropriedade { get; set; }
        public int? Categoria { get; set; }
        public string? Classe { get; set; }
        public string? Valor { get; set; }
        public int? Status { get; set; }
    }
}
