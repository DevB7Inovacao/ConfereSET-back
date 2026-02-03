using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
    public class MaoDeObraSimpleDTO
    {
        public int Id { get; set; }
        public string Funcao { get; set; } = null!;
        public string? Descricao { get; set; }
    }
}