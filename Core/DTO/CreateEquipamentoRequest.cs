using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
    public class CreateEquipamentoRequest
    {
        public required string Nome { get; set; }
        public string? Descricao { get; set; }
    }
}