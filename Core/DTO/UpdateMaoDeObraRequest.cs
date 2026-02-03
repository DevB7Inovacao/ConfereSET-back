using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
    public class UpdateMaoDeObraRequest
    {
        public string? Funcao { get; set; }
        public string? Descricao { get; set; }
        public int? Status { get; set; }
    }
}