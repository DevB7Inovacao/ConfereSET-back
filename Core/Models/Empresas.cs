using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Models
{
    public class Empresas : BaseModel
    {
        public string Name { get; set; } = "";
        public bool Status { get; set; } = true;
        public string? CNPJ { get; set; }
        public string? TradeName { get; set; }
        public string? AppName { get; set; }
        public string? LogoBase64 { get; set; }
        public string? LogoContentType { get; set; }
        public string? ContactEmail { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? PrimaryColor { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
		public ICollection<Plano> Planos { get; set; } = new List<Plano>();
	}
}
