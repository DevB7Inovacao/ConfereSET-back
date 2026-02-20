using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class ChecklistVariavel : BaseModel
    {
        public required int EmpresaId { get; set; }
        public int ChecklistId { get; set; }
        public Checklist? Checklist { get; set; }
        public int ModeloTextoVariavelId { get; set; }
        public ModeloTextoVariavel? ModeloTextoVariavel { get; set; }
        public int Status { get; set; } = 1;
    }
}