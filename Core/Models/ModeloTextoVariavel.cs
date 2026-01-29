using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class ModeloTextoVariavel : BaseModel
    {
        public required int EmpresaId { get; set; }
        public required string Nome { get; set; }
        public required string NomeAmigavel { get; set; }
        public required string NomePropriedade { get; set; }
        public int Categoria { get; set; }
        public string? Classe { get; set; }
        public string? Valor { get; set; }
        public int Status { get; set; } = 1;
    }
}
