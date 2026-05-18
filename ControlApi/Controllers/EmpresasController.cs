using AutoMapper;
using Core.DTO;
using Core.Models;
using Infrastructure.Authenticate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace ControlApi.Controllers
{
	/// <summary>
	/// Gestão de empresas.
	/// <para>
	/// Regras de autorização (validadas neste controller, não em filtros globais):
	/// <list type="bullet">
	/// <item><b>create</b>: público, parte do onboarding inicial (mesmo padrão de
	/// <c>UsersController.Create</c>). A validação efetiva está em <c>EmpresasService.CreateEmpresa</c>.</item>
	/// <item><b>getEmpresasPaged / getById / update</b>: usuário só vê/edita a própria empresa
	/// (EmpresaId do JWT). Update exige <c>admin</c>.</item>
	/// <item><b>delete / toggle-status</b>: operação destrutiva sobre a própria empresa, restrita a
	/// <c>admin</c>. A id alvo deve coincidir com a empresa do JWT.</item>
	/// </list>
	/// </para>
	/// Contratos de DTO e status codes mantidos iguais aos anteriores para não quebrar o front.
	/// </summary>
	[Authorize]
	[Route("api/[controller]")]
	[ApiController]
	public class EmpresasController : ControllerBase
	{
		private readonly IJWTManager _jWTManager;
		private readonly IMapper _mapper;
		IEmpresasService _empresasService;

		public EmpresasController(IJWTManager jWTManager, IMapper mapper, IEmpresasService empresasService)
		{
			this._jWTManager = jWTManager;
			this._mapper = mapper;
			this._empresasService = empresasService;
		}

		/// <summary>
		/// Cadastro inicial público (onboarding). Mantido <see cref="AllowAnonymousAttribute"/>
		/// porque o usuário ainda não possui token neste momento — o fluxo equivalente está em
		/// <c>UsersController.Create</c> quando não há <c>idEmpresa</c>.
		/// </summary>
		[AllowAnonymous]
		[HttpPost]
		[Route("create")]
		public async Task<IActionResult> CreateEmpresa([FromBody] EmpresasDTO empresas)
		{
			try
			{
				var empresa = _mapper.Map<Empresas>(empresas);
				var result = await _empresasService.CreateEmpresa(empresa);

				if (result.Success)
					return Ok("Empresa cadastrada com sucesso.");
				else
					return BadRequest("Erro ao cadastrar empresa.");
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		/// <summary>
		/// Atualiza dados da empresa. Exige <c>admin</c> do JWT da mesma empresa; o
		/// <c>empresaId</c> da URL precisa coincidir com o <c>EmpresaId</c> do token.
		/// O <c>UserId</c>/<c>UserType</c> do payload são ignorados em favor do JWT.
		/// </summary>
		[HttpPut("{empresaId}")]
		public async Task<IActionResult> UpdateById(int empresaId, [FromBody] UpdateEmpresaByIdRequest req)
		{
			try
			{
				if (empresaId <= 0) return BadRequest("empresaId inválido.");
				if (req == null) return BadRequest("Payload inválido.");

				var empresaJwt = User.GetEmpresaId();
				if (empresaJwt != empresaId)
					return StatusCode(StatusCodes.Status403Forbidden, "Não é permitido alterar dados de outra empresa.");

				if (User.GetUserType() != TypeUser.admin)
					return StatusCode(StatusCodes.Status403Forbidden, "Apenas admin pode alterar dados da empresa.");

				var result = await _empresasService.UpdateEmpresa(req.Empresa, empresaId);
				if (result) return Ok(true);

				return BadRequest("Falha ao atualizar empresa.");
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

		/// <summary>
		/// Exclui a própria empresa. Operação altamente destrutiva — restrita a <c>admin</c>
		/// do JWT da mesma empresa (o <c>id</c> da URL precisa coincidir com o token).
		/// </summary>
		[HttpDelete]
		[Route("delete/{id}")]
		public async Task<IActionResult> DeleteEmpresa(int id)
		{
			try
			{
				if (id <= 0) return BadRequest("id inválid