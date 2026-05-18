using ControlApi;
using Core.DTO;
using Core.Models;
using Infrastructure.Authenticate;
using Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace ControlApi.Controllers
{
	/// <summary>
	/// Gestão de usuários e autenticação.
	/// <para>
	/// Regras gerais:
	/// <list type="bullet">
	/// <item><b>authenticate</b>: público (login).</item>
	/// <item><b>create (cadastro inicial)</b>: público apenas quando <c>idEmpresa</c> é nulo — fluxo de onboarding (cria empresa + primeiro usuário como <c>admin</c>).</item>
	/// <item><b>create (usuário adicional)</b>: exige token de <c>admin</c>/<c>gerente</c> da mesma empresa.</item>
	/// <item><b>update/delete/get/count</b>: exigem token; admin/gerente da empresa do alvo; auto-edição também permitida para o próprio usuário (limitada).</item>
	/// </list>
	/// </para>
	/// Mantém contratos de DTO e rotas iguais ao back anterior para não quebrar o front existente.
	/// </summary>
	[Authorize]
	[Route("api/[controller]")]
	[ApiController]
	public class UsersController : ControllerBase
	{
		private readonly IJWTManager _jWTManager;
		private readonly IUserService userService;
		private readonly IEmpresasService _empresaService;

		// Senha mínima — mesma constante usada no front (validar no cliente também).
		private const int MinPasswordLength = 8;

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

			// Verifica senha com fallback (BCrypt + AES legado). O service grava on-the-fly em BCrypt
			// quando reconhece um hash AES ainda válido.
			var senhaConfere = user != null && await userService.VerifyPasswordAndUpgrade(user, userDTO.Password);

			if (senhaConfere)
			{
				try
				{
					token = await _jWTManager.Authenticate(user!);
				}
				catch (Exception)
				{
					throw;
				}
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

		/// <summary>
		/// Cria o primeiro usuário (admin) junto com a empresa OU adiciona um usuário a uma empresa
		/// existente. As duas formas usam o mesmo endpoint para preservar compatibilidade com o front.
		///
		/// <list type="bullet">
		/// <item>Quando <c>idEmpresa</c> é <c>null/0</c>: cria empresa nova e usuário <b>admin</b>.
		/// Não exige token (este é o cadastro inicial público).</item>
		/// <item>Quando <c>idEmpresa</c> é preenchido: exige token de <c>admin</c>/<c>gerente</c> da
		/// mesma empresa. O tipo do novo usuário pode ser definido (com limite: gerente só cria
		/// operador/somenteleitura).</item>
		/// </list>
		/// </summary>
		[AllowAnonymous]
		[HttpPost]
		[Route("create")]
		public async Task<IActionResult> Create(CreateUserRequest userdata)
		{
			try
			{
				if (userdata == null) return BadRequest("Payload inválido.");
				if (string.IsNullOrWhiteSpace(userdata.Email)) return BadRequest("Email é obrigatório.");
				if (string.IsNullOrWhiteSpace(userdata.Name)) return BadRequest("Nome é obrigatório.");
				if (string.IsNullOrWhiteSpace(userdata.Password))
					return BadRequest("Senha é obrigatória.");
				if (userdata.Password!.Length < MinPasswordLength)
					return BadRequest($"A senha precisa ter pelo menos {MinPasswordLength} caracteres.");

				// Email único é regra do sistema (login usa GetUserByEmail). Bloqueamos duplicados.
				var existing = await userService.GetUserByEmail(userdata.Email.Trim());
				if (existing != null)
					return BadRequest("Já existe um usuário com este e-mail.");

				var temEmpresa = userdata.IdEmpresa != null && userdata.IdEmpresa > 0;

				if (!temEmpresa)
				{
					// FLUXO 1: cadastro inicial público — cria empresa + primeiro usuário como ADMIN.
					var empresa = new Empresas
					{
						Name = userdata.EmpresaName ?? string.Empty,
						Status = true,
						CNPJ = userdata.CNPJ ?? string.Empty,
					};

					var result = await _empresaService.CreateEmpresa(empresa);
					if (!result.Success)
						return BadRequest(result.Message);

					var empresaCriada = result.Data as Empresas
						?? throw new Exception("Falha ao recuperar empresa criada.");

					var primeiroUsuario = new User
					{
						Name = userdata.Name.Trim(),
						Password = userdata.Password,
						Email = userdata.Email.Trim(),
						// Primeiro usuário SEMPRE vira admin, independente do que o cliente mandar.
						// Isso garante que o "dono" da empresa tenha permissão para editar a empresa
						// (vide EmpresasController.UpdateById que exige TypeUser.admin).
						Type = TypeUser.admin,
						Status = userdata.Status == 0 ? 1 : userdata.Status,
						Empresa = empresaCriada
					};

					var ok = await userService.CreateUser(primeiroUsuario);
					return ok
						? Ok(new { success = true, message = "Cadastro realizado com sucesso.", empresaId = empresaCriada.Id })
						: BadRequest("Falha ao criar usuário.");
				}
				else
				{
					// FLUXO 2: usuário adicional em empresa existente — exige token.
					if (User?.Identity?.IsAuthenticated != true)
						return StatusCode(StatusCodes.Status401Unauthorized, "É necessário estar autenticado para adicionar usuários a uma empresa existente.");

					var empresaIdJwt = User.GetEmpresaId();
					if (empresaIdJwt != userdata.IdEmpresa)
						return StatusCode(StatusCodes.Status403Forbidden, "Não é permitido criar usuário em outra empresa.");

					if (!User.IsAdminOrGerente())
						return StatusCode(StatusCodes.Status403Forbidden, "Apenas admin/gerente pode adicionar usuários.");

					// Gerente não pode promover ninguém a admin/gerente. Só pode criar operador/somenteleitura.
					if (User.GetUserType() == TypeUser.gerente &&
						(userdata.Type == TypeUser.admin || userdata.Type == TypeUser.gerente))
					{
						return StatusCode(StatusCodes.Status403Forbidden, "Gerente só pode criar usuários do tipo operador ou somenteleitura.");
					}

					var empresa = await _empresaService.GetEmpresaById((int)userdata.IdEmpresa!);
					if (empresa == null)
						return BadRequest("Empresa não encontrada.");

					var novoUser = new User
					{
						Name = userdata.Name.Trim(),
						Password = userdata.Password,
						Email = userdata.Email.Trim(),
						Type = userdata.Type,
						Status = userdata.Status == 0 ? 1 : userdata.Status,
						Empresa = empresa
					};

					var ok = await userService.CreateUser(novoUser);
					return ok
						? Ok(new { success = true, message = "Usuário criado com sucesso." })
						: BadRequest("Falha ao criar usuário.");
				}
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpGet("getUsersPaged")]
		public async Task<IActionResult> GetUsersPaged([FromQuery] FiltersDTO filtersDTO)
		{
			try
			{
				var empresaId = User.GetEmpresaId();
				filtersDTO.EmpresaId = empresaId;
				var result = await userService.GetUsersPaged(filtersDTO);
				return Ok(result);
			}
			catch (UnauthorizedAccessException ex)
			{
				return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
			}
		}

		[HttpGet("getUsers")]
		public async Task<IActionResult> GetUsers([FromQuery] FiltersDTO filtersDTO)
		{
			// Endpoint sem filtro de empresa — restrito a admin (visão geral, usada por "master").
			try
			{
				if (!User.IsAdminOrGerente())
					return StatusCode(StatusCodes.Status403Forbidden, "Sem permissão.");

				var result = await userService.GetUsers(filtersDTO);
				return Ok(result);
			}
			catch (UnauthorizedAccessException ex)
			{
				return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
			}
		}

		/// <summary>Detalhes de um usuário. Só admin/gerente da mesma empresa, ou o próprio usuário.</summary>
		[HttpGet("getbyuserid/{userId}")]
		public async Task<IActionResult> GetUsersByUserId(int userId)
		{
			try
			{
				var alvo = await userService.GetUserById(userId);
				if (alvo == null) return NotFound();

				var empresaJwt = User.GetEmpresaId();
				var userIdJwt = User.GetUserId();

				if (alvo.EmpresaId != empresaJwt)
					return StatusCode(StatusCodes.Status403Forbidden, "Sem permissão.");

				var ehProprio = alvo.Id == userIdJwt;
				if (!User.IsAdminOrGerente() && !ehProprio)
					return StatusCode(StatusCodes.Status403Forbidden, "Sem permissão.");

				return Ok(alvo);
			}
			catch (UnauthorizedAccessException ex)
			{
				return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
			}
		}

		/// <summary>
		/// Atualiza usuário. Admin/gerente da empresa, OU o próprio usuário (com restrições:
		/// não pode mudar o próprio Type/Status).
		/// </summary>
		[HttpPut("{userId}")]
		public async Task<IActionResult> UpdateUser(CreateUserRequest user, int userId)
		{
			try
			{
				if (user == null) return BadRequest("Payload inválido.");

				var alvo = await userService.GetUserById(userId);
				if (alvo == null) return NotFound("Usuário não encontrado.");

				var empresaJwt = User.GetEmpresaId();
				var userIdJwt = User.GetUserId();
				var meuType = User.GetUserType();

				if (alvo.EmpresaId != empresaJwt)
					return StatusCode(StatusCodes.Status403Forbidden, "Sem permissão.");

				var ehProprio = alvo.Id == userIdJwt;
				if (!User.IsAdminOrGerente() && !ehProprio)
					return StatusCode(StatusCodes.Status403Forbidden, "Sem permissão.");

				// O próprio user não pode mudar o próprio tipo/status (apenas admin/gerente faz isso).
				if (ehProprio && !User.IsAdminOrGerente())
				{
					user.Type = alvo.Type;
					user.Status = alvo.Status;
				}

				// Gerente não pode mexer em admin nem promover/rebaixar para admin.
				if (meuType == TypeUser.gerente)
				{
					if (alvo.Type == TypeUser.admin)
						return StatusCode(StatusCodes.Status403Forbidden, "Gerente não pode alterar usuários admin.");
					if (user.Type == TypeUser.admin)
						return StatusCode(StatusCodes.Status403Forbidden, "Gerente não pode promover usuário a admin.");
				}

				// Se senha veio preenchida, exige tamanho mínimo.
				if (!string.IsNullOrWhiteSpace(user.Password) && user.Password.Length < MinPasswordLength)
					return BadRequest($"A senha precisa ter pelo menos {MinPasswordLength} caracteres.");

				var ok = await userService.UpdateUser(user, userId);
				return ok ? Ok(true) : BadRequest("Falha ao atualizar usuário.");
			}
			catch (UnauthorizedAccessException ex)
			{
				return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpDelete("{userId}")]
		public async Task<IActionResult> DeleteUser(int userId)
		{
			try
			{
				var alvo = await userService.GetUserById(userId);
				if (alvo == null) return NotFound("Usuário não encontrado.");

				var empresaJwt = User.GetEmpresaId();
				var userIdJwt = User.GetUserId();

				if (alvo.EmpresaId != empresaJwt)
					return StatusCode(StatusCodes.Status403Forbidden, "Sem permissão.");

				if (!User.IsAdminOrGerente())
					return StatusCode(StatusCodes.Status403Forbidden, "Apenas admin/gerente pode excluir usuários.");

				if (alvo.Id == userIdJwt)
					return BadRequest("Você não pode excluir o próprio usuário.");

				// Gerente não pode excluir admin.
				if (User.GetUserType() == TypeUser.gerente && alvo.Type == TypeUser.admin)
					return StatusCode(StatusCodes.Status403Forbidden, "Gerente não pode excluir um admin.");

				bool result = await userService.DeleteUser(userId);
				return result ? Ok("Usuário excluído com sucesso.") : BadRequest("Falha ao excluir usuário.");
			}
			catch (UnauthorizedAccessException ex)
			{
				return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		/// <summary>Conta usuários da empresa. EmpresaId vem sempre do JWT, ignorando query.</summary>
		[HttpGet("count")]
		public async Task<IActionResult> Count()
		{
			try
			{
				var empresaId = User.GetEmpresaId();
				var total = await userService.CountUsersByEmpresaId(empresaId);
				return Ok(new { total });
			}
			catch (UnauthorizedAccessException ex)
			{
				return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
			}
		