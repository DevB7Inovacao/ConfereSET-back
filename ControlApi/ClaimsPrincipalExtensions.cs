using System.Security.Claims;

namespace ControlApi
{
	public static class ClaimsPrincipalExtensions
	{
		public static int GetEmpresaId(this ClaimsPrincipal user)
		{
			var claim = user.FindFirst("EmpresaId");
			if (claim == null)
				throw new UnauthorizedAccessException("EmpresaId não encontrado no token");

			if (!int.TryParse(claim.Value, out int empresaId))
				throw new UnauthorizedAccessException("EmpresaId inválido");

			return empresaId;
		}
	}
}
