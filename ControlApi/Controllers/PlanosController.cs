using Core.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace ControlApi.Controllers
{
	/// <summary>
	/// Gestão de planos. Apenas a listagem de planos ativos é pública (vitrine de planos
	/// para visitantes antes do cadastro). Todas as demais operações exigem autenticação
	/// e — para escrita — papel admin/gerente da empresa do chamador.
	/// </summary>
	[Authorize]
	[Route("api/[controller]")]
	[ApiController]
	public class PlanosController : ControllerBase
	{
		private readonly IPlanoService _planoService;

		public PlanosController(IPlanoService planoService)
		{
			_planoService = planoService;
		}

		/// <summary>
		/// Lista pública de planos ativos — usada na vitrine /planos do front para usuários
		/// não autenticados decidirem qual plano contratar.
		/// </summary>
		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> GetAtivos()
		{
			var planos = await _planoService.GetAtivos();
			return Ok(planos);
		}

		/// <summary>Lista os planos da empresa do JWT.</summary>
		[HttpGet("all")]
		public async Task<IActionResult> GetAll()
		{
			try
			{
				int empresaid = User.GetEmpresaId();
				var planos = await _planoService.GetAll(empresaid);
				return Ok(planos);
			}
			catch (UnauthorizedAccessException ex)
			{
				return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
			}
		}

		/// <summary>Detalhe de plano. Bloqueia 403 se o plano pertencer a outra empresa.</summary>
		[HttpGet("{id}")]
		public async Task<IActionResult> GetById(int id)
		{
			try
			{
				var plano = await _planoService.G