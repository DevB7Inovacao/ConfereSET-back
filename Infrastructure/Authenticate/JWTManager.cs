using Core.DTO;
using Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Infrastructure.Authenticate
{
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
				var tokenKey = Encoding.UTF8.GetBytes(_configuration["JWT:Key"] ?? throw new InvalidOperationException("JWT:Key não configurada"));

				var tokenDescriptor = new SecurityTokenDescriptor
				{
					Subject = new ClaimsIdentity(new[]
					{
						new Claim(ClaimTypes.Name, users.Name),
						new Claim("EmpresaId", users.Empresa.Id.ToString()),
						new Claim("UserId", users.Id.ToString()),
						new Claim("Type", users.Type.ToString())
					}),
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
