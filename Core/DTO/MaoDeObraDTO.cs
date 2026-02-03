using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
    public class MaoDeObraDTO
    {
        public int Id { get; set; }
        public string? Funcao { get; set; }
        public string? Descricao { get; set; }
        public int? Status { get; set; }
    }

    public class MaoDeObraPagedDTO
    {
        public int PageCount { get; set; }
        public IList<MaoDeObraDTO> Result { get; set; }
    }
}