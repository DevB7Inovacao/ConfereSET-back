using System.Security.Claims;
using Core.Models;

namespace ControlApi
{
	/// <summary>
	/// Helpers para ler claims emitidas em <c>Infrastructure.Authenticate.JWTManager</c>.
	/// Claims gravadas no token: <c>ClaimTypes.Name</c>, <c>EmpresaId</c>, <c>UserId</c>, <c>Type</c>.
	/// </summary>
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

		public static int GetUserId(this ClaimsPrincipal user)
		{
			var claim = user.FindFirst("UserId");
			if (claim == null)
				throw new UnauthorizedAccessException("UserId não encontrado no token");

			if (!int.TryParse(claim.Value, out int userId))
				throw new UnauthorizedAccessException("UserId inválido");

			return userId;
		}

		/// <summary>
		/// Tipo do usuário conforme o enum <see cref="TypeUser"/>. Retorna <c>null</c>
		/// se a claim estiver ausente ou inválida (mantém comportamento tolerante).
		/// </summary>
		public static TypeUser? GetUserType(this ClaimsPrincipal user)
		{
			var claim = user.FindFirst("Type");
			if (claim == null || string.IsNullOrWhiteSpace(claim.Value))
				return null;

			return Enum.TryParse<TypeUser>(claim.Value, ignoreCase: true, out var type)
				? type
				: null;
		}

		public static bool IsAdminOrGerente(this ClaimsPrincipal user)
		{
			var t = user.GetUserType();
			return t == TypeUser.admin || t == TypeUser.gerente;
		}

		public static bool IsReadOnly(this ClaimsPrincipal user)
		{
			return user.GetUserType() == TypeUser.somenteleitura;
		}
	}
}