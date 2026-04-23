using Core.Models;

namespace Core.DTO
{
	public class CreateUserRequest
	{
		public required string Name { get; set; }
		public string? Password { get; set; }
		public required string Email { get; set; }
		public required TypeUser Type { get; set; }
		public int Status { get; set; }

		//Dados da empresa
		public int? IdEmpresa { get; set; }
		public string? CNPJ { get; set; }
		public string? EmpresaName { get; set; }
	}
}
