using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
    public class CreateMaoDeObraRequest
    {
        public required string Funcao { get; set; }
        public string? Descricao { get; set; }
    }
}