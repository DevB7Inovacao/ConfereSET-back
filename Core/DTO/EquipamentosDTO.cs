using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
    public class EquipamentosDTO
    {
        public int Id { get; set; }
        public string? Nome { get; set; }
        public string? Descricao { get; set; }
        public int? Status { get; set; }
    }

    public class EquipamentosPagedDTO
    {
        public int PageCount { get; set; }
        public IList<EquipamentosDTO> Result { get; set; }
    }
}