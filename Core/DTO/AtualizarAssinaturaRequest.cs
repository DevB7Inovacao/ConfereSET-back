using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
	public class AtualizarAssinaturaRequest
	{
		public decimal? NovoValor { get; set; }
		public string? CardToken { get; set; }
	}
}
