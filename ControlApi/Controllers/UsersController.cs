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
		private readonly IAssinaturaService _assinaturaService;

		public UsersController(IJWTManager jWTManager, IUserService userService, IEmpresasService empresaService, IAssinaturaService assinaturaService)
		{
			this._jWTManager = jWTManager;
			this.userService = userService;
			this._empresaService = empresaService;
			this._assinaturaService = assinaturaService;
		}

		private static bool IsPerfilAdministradorEmpresa(TypeUser type) =>
			type == TypeUser.gerente || type == TypeUser.admin;

		private static bool IsPerfilOperador(TypeUser type) =>
			type == TypeUser.operador;

		private async Task<IActionResult?> ValidarLimiteUsuariosPlano(
			int empresaId,
			TypeUser novoTipo,
			TypeUser? tipoAtual = null)
		{
			var limites = await _assinaturaService.VerificarLimites(empresaId);
			if (!limites.AssinaturaAtiva)
				return BadRequest("Sua empresa não possui assinatura ou trial ativo. Adquira um plano para gerenciar usuários.");

			if (IsPerfilAdministradorEmpresa(novoTipo))
			{
				var administradoresUsados = limites.GestoresUtilizados;
				if (tipoAtual.HasValue && IsPerfilAdministradorEmpresa(tipoAtual.Value))
					administradoresUsados = Math.Max(0, administradoresUsados - 1);

				if (administradoresUsados >= limites.LimiteGestores)
					return BadRequest($"Limite de administradores atingido para o plano atual ({limites.LimiteGestores}).");
			}

			if (IsPerfilOperador(novoTipo))
			{
				var operadoresUsados = limites.OperadoresUtilizados;
				if (tipoAtual.HasValue && IsPerfilOperador(tipoAtual.Value))
					operadoresUsados = Math.Max(0, operadoresUsados - 1);

				if (operadoresUsados >= limites.LimiteOperadores)
					return BadRequest($"Limite de operadores atingido para o plano atual ({limites.LimiteOperadores}).");
			}

			return null;
		}

		[AllowAnonymous]
		[HttpPost]
		[Route("authenticate")]
		public async Task<IActionResult> Authenticate(UserAuthenticateRequest userDTO)
		{
			TokenJWT? token = null;
			var email = (userDTO.Email ?? "").Trim();
			// [v11] Senha trimada igual ao CreateUser/UpdateUser para evitar mismatch
			// quando o usuário (ou autofill do navegador) injeta whitespace na ponta.
			var senha = (userDTO.Password ?? "").Trim();
			var user = await userService.GetUserByEmail(email);

			// Verifica a senha aceitando BCrypt (atual) e AES (legado). Faz upgrade transparente
			// se o usuário ainda tem hash AES no banco.
			if (user != null && await userService.VerifyPasswordAndUpgrade(user, senha))
			{
				try
				{
					token = await _jWTManager.Authenticate(user);
				}
				catch (Exception ex)
				{
					throw;
				}
			}


			if (user == null)
				return Unauthorized(new { message = "Usuário não localizado!" });
			if (token == null)
				return Unauthorized(new { message = "Senha inválida!" });

			// Bloqueio de acesso para contas/empresas desativadas. Mensagens claras para o usuário.
			if (user.Status == 0)
				return Unauthorized(new { message = "Usuário desativado. Contate o administrador da sua empresa." });
			if (user.Empresa != null && user.Empresa.Status == false)
				return Unauthorized(new { message = "Empresa desativada. Entre em contato com o suporte." });

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
			var criandoNovaEmpresa = userdata.IdEmpresa == null;
			var hasEmpresa = !criandoNovaEmpresa ? await _empresaService.GetEmpresaById((int)userdata.IdEmpresa!) : null;

			if (!criandoNovaEmpresa && hasEmpresa == null)
				return BadRequest("Empresa não encontrada.");

			if (criandoNovaEmpresa)
			{
				var empresa = new Empresas()
				{
					Name = userdata.EmpresaName ?? "",
					Status = true,
					CNPJ = userdata.CNPJ ?? "",
				};

				var result = await _empresaService.CreateEmpresa(empresa);
				if (!result.Success)
				{
					return BadRequest(result.Message);
				}
				hasEmpresa = result.Data as Empresas;

				// Inicia trial automático de 15 dias assim que a empresa é criada.
				// Falha aqui não impede o cadastro do usuário; o admin pode iniciar manualmente depois.
				try
				{
					if (hasEmpresa != null && hasEmpresa.Id > 0)
						await _assinaturaService.IniciarTrial(hasEmpresa.Id, 15);
				}
				catch { /* trial é best-effort no cadastro */ }
			}

			// Cadastro público cria o responsável da empresa como GERENTE (administrador da empresa).
			// O perfil TypeUser.admin é reservado ao dono/master da plataforma.
			var tipoUsuario = criandoNovaEmpresa ? TypeUser.gerente : userdata.Type;

			if (!criandoNovaEmpresa)
			{
				if (User?.Identity?.IsAuthenticated != true)
					return Unauthorized(new { message = "Faça login para criar usuários em uma empresa existente." });

				var tipoLogado = User.GetUserType();
				if (tipoUsuario == TypeUser.admin && tipoLogado != TypeUser.admin)
					return BadRequest("O perfil Admin é reservado ao master da plataforma. Para a empresa, use Administrador/Gerente.");

				var limiteInvalido = await ValidarLimiteUsuariosPlano(hasEmpresa!.Id, tipoUsuario);
				if (limiteInvalido != null) return limiteInvalido;
			}

			var user = new User()
			{
				Name = userdata.Name,
				Password = userdata.Password,
				Email = userdata.Email,
				Status = userdata.Status,
				Type = tipoUsuario,
				Empresa =  hasEmpresa
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

		[HttpGet("getUsers")]
		public async Task<IActionResult> GetUsers([FromQuery] FiltersDTO filtersDTO)
		{
			filtersDTO.EmpresaId = User.GetEmpresaId();
			var result = await userService.GetUsers(filtersDTO);

			return Ok(result);
		}


		/// <summary>
		/// Get data by userid
		/// </summary>
		/// <param name="userId"></param>
		/// <returns></returns>
		[HttpGet("getbyuserid/{userId}")]
		public async Task<IActionResult> GetUsersByUserId(int userId)
		{
			var user = await userService.GetUserById(userId);
			if (user == null) return NotFound("Usuário não encontrado.");
			// Multi-tenant: só pode ler usuários da própria empresa.
			var empresaIdJwt = User.GetEmpresaId();
			if (user.EmpresaId != empresaIdJwt) return NotFound("Usuário não encontrado.");
			return Ok(user);
		}

		/// <summary>
		/// Update the user
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		[HttpPut("{userId}")]
		public async Task<IActionResult> UpdateUser(CreateUserRequest user, int userId)
		{
			if (user != null)
			{
				// Multi-tenant: só atualiza usuários da própria empresa.
				var __existing = await userService.GetUserById(userId);
				var empresaIdJwt = User.GetEmpresaId();
				if (__existing == null || __existing.EmpresaId != empresaIdJwt) return NotFound("Usuário não encontrado.");

				var tipoLogado = User.GetUserType();
				if (user.Type == TypeUser.admin && tipoLogado != TypeUser.admin)
					return BadRequest("O perfil Admin é reservado ao master da plataforma. Para a empresa, use Administrador/Gerente.");

				var limiteInvalido = await ValidarLimiteUsuariosPlano(empresaIdJwt, user.Type, __existing.Type);
				if (limiteInvalido != null) return limiteInvalido;

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

		[HttpDelete("{userId}")]
		public async Task<IActionResult> DeleteUser(int userId)
		{
			try
			{
				// Multi-tenant: só deleta usuários da própria empresa.
				var __existing = await userService.GetUserById(userId);
				var empresaIdJwt = User.GetEmpresaId();
				if (__existing == null || __existing.EmpresaId != empresaIdJwt) return NotFound("Usuário não encontrado.");
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

		[HttpGet("count")]
		public async Task<IActionResult> Count()
		{
			// Multi-tenant: força a empresa do JWT, ignorando query string.
			var empresaId = User.GetEmpresaId();
			if (empresaId <= 0) return BadRequest("empresaId inválido.");

			var total = await userService.CountUsersByEmpresaId(empresaId);

			return Ok(new { total });
		}
	}
}