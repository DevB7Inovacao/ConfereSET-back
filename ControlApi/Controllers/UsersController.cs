using ControlApi;
using Core.DTO;
using Core.Models;
using Infrastructure.Authenticate;
using Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace API.Controllers
{
	[Authorize]
	[Route("api/[controller]")]
	[ApiController]
	public class UsersController : ControllerBase
	{
		private readonly IJWTManager _jWTManager;
		private readonly IUserService userService;
		private readonly IEmpresasService _empresaService;

		public UsersController(IJWTManager jWTManager, IUserService userService, IEmpresasService empresaService)
		{
			this._jWTManager = jWTManager;
			this.userService = userService;
			this._empresaService = empresaService;
		}

		[AllowAnonymous]
		[HttpPost]
		[Route("authenticate")]
		public async Task<IActionResult> Authenticate(UserAuthenticateRequest userDTO)
		{
			TokenJWT? token = null;
			var user = await userService.GetUserByEmail(userDTO.Email);

			if (user != null && Encrypt.EncryptPassword(userDTO.Password) == user.Password)
			{
				token = await _jWTManager.Authenticate(user);
			}


			if (user == null)
				return Unauthorized(new { message = "Usuário não localizado!" });
			if (token == null)
				return Unauthorized(new { message = "Senha inválida!" });
			var empresaId = user.Empresa?.Id ?? 0;

			return Ok(new
			{
				token = token.Token,
				userId = user.Id,
				empresaId = empresaId,
				type = user.Type,
				email = user.Email,
				name = user.Name
			});
		}

		[AllowAnonymous]
		[HttpPost]
		[Route("create")]
		public async Task<IActionResult> Create(CreateUserRequest userdata)
		{
			var hasEmpresa = (userdata.IdEmpresa != null) ? _empresaService.GetEmpresaById((int)userdata.IdEmpresa) : null;

			if (hasEmpresa == null)
			{
				var empresa = new Empresas()
				{
					Name = userdata.EmpresaName ?? "",
					Status = true,
					CNPJ = userdata.CNPJ ?? "",
				};

				hasEmpresa = _empresaService.CreateEmpresa(empresa);
			}

			var user = new User()
			{
				Name = userdata.Name,
				Password = userdata.Password,
				Email = userdata.Email,
				Status = userdata.Status,
				Type = userdata.Type,
				Empresa = await hasEmpresa
			};
			var isUserCreated = await userService.CreateUser(user);

			if (isUserCreated.Equals(true))
				return Ok(isUserCreated);
			else
				return BadRequest();
		}



		[HttpGet("getUsersPaged")]
		public async Task<IActionResult> GetUsersPaged([FromQuery] FiltersDTO filtersDTO)
		{
			var empresaId = User.GetEmpresaId();
			filtersDTO.EmpresaId = empresaId;
			var result = await userService.GetUsersPaged(filtersDTO);

			return Ok(result);

		}

		/// <summary>
		/// Get data by userid
		/// </summary>
		/// <param name="userId"></param>
		/// <returns></returns>
		[AllowAnonymous]
		[HttpGet("getbyuserid/{userId}")]
		public async Task<IActionResult> GetUsersByUserId(int userId)
		{
			var user = await userService.GetUserById(userId);
			if (user != null)
				return Ok(user);
			else
				return BadRequest();
		}

		/// <summary>
		/// Update the user
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		[AllowAnonymous]
		[HttpPut("{userId}")]
		public async Task<IActionResult> UpdateUser(CreateUserRequest user, int userId)
		{
			if (user != null)
			{
				var isUserCreated = await userService.UpdateUser(user, userId);
				if (isUserCreated)
					return Ok(isUserCreated);
				else
					return BadRequest();
			}
			else
			{
				return BadRequest();
			}
		}

		[AllowAnonymous]
		[HttpDelete("{userId}")]
		public async Task<IActionResult> DeleteUser(int userId)
		{
			try
			{
				bool result = await userService.DeleteUser(userId);
				if (result)
					return Ok("Usuário excluído com sucesso.");
				else
					return BadRequest("Falha ao excluir usuário.");
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[AllowAnonymous]
		[HttpGet("count")]
		public async Task<IActionResult> Count([FromQuery] int empresaId)
		{
			if (empresaId <= 0) return BadRequest("empresaId inválido.");

			var total = await userService.CountUsersByEmpresaId(empresaId);

			return Ok(new { total });
		}
	}
}