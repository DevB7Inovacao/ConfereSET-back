using Core.DTO;
using Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Infrastructure.Authenticate
{
	/// <summary>
	/// Emite tokens JWT para autenticação. O token agora carrega <c>Issuer</c> e <c>Audience</c>
	/// (quando configurados em <c>Jwt:Issuer</c> / <c>Jwt:Audience</c>), permitindo que o
	/// <c>Program.cs</c> ative a validação dessas claims sem invalidar tokens antigos — basta
	/// adicionar os valores ao appsettings.
	/// </summary>
	public class JWTManager : IJWTManager
	{
		private readonly IConfiguration _configuration;

		public JWTManager(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		public async Task<TokenJWT?> Authenticate(User users)
		{
			try
			{
				var tokenHandler = new JwtSecurityTokenHandler();
				var tokenKey = Encoding.UTF8.GetBytes(
					_configuration["Jwt:Key"]
					?? _configuration["JWT:Key"]
					?? throw new InvalidOperationException("Jwt:Key não configurada"));

				// Issuer/Audience são opcionais: se ausentes, o token é emitido sem essas
				// claims e a validação correspondente em Program.cs também fica desligada.
				// Quando presentes, o Program.cs ativa ValidateIssuer/ValidateAudience.
				var issuer = _configuration["Jwt:Issuer"];
				var audience = _configuration["Jwt:Audience"];

				var tokenDescriptor = new SecurityTokenDescriptor
				{
					Subject = new ClaimsIdentity(new[]
					{
						new Claim(ClaimTypes.Name, users.Name),
						new Claim("EmpresaId", users.Empresa.Id.ToString()),
						new Claim("UserId", users.Id.ToString()),
						new Claim("Type", users.Type.ToString())
					}),
					Issuer = string.IsNullOrWhiteSpace(issuer) ? null : issuer,
					Audience = string.IsNullOrWhiteSpace(audience) ? null : audience,
					Expires = DateTime.UtcNow.AddDays(1),
					SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(tokenKey), SecurityAlgorithms.HmacSha256Signature)
				};

				var token = tokenHandler.CreateToken(tokenDescriptor);
				var tokenString = tokenHandler.WriteToken(token);

				return await Task.FromResult(new TokenJWT { Token = tokenString });
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException("Erro ao gerar token JWT", ex);
			}
		}
	}

	public interface IJWTManager
	{
		Task<TokenJWT?> Authenticate(User users);
	}
}