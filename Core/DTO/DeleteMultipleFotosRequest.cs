using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
	public class DeleteMultipleFotosRequest
	{
		public List<int> FotoIds { get; set; } = new();
	}
}
