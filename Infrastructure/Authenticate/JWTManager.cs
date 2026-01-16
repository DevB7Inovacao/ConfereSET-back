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

		private readonly IConfiguration iconfiguration;
		public JWTManager(IConfiguration iconfiguration)
		{
			this.iconfiguration = iconfiguration;
		}
		public async Task<TokenJWT?> Authenticate(User users)
		{
			var tokenHandler = new JwtSecurityTokenHandler();

			var tokenKey = Encoding.UTF8.GetBytes(iconfiguration["JWT:Key"]!);
			var tokenDescriptor = new SecurityTokenDescriptor
			{
                Subject = new ClaimsIdentity(new Claim[]
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
			return new TokenJWT { Token = tokenHandler.WriteToken(token) };

		}
	}
	public interface IJWTManager
	{
		Task<TokenJWT?> Authenticate(User users);
	}
}
