using Core.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace ControlApi.Controllers
{
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

		[AllowAnonymous]
		[HttpGet]
		public async Task<IActionResult> GetAtivos()
		{
			var planos = await _planoService.GetAtivos();
			return Ok(planos);
		}

		// Catálogo completo (inclui inativos) e qualquer escrita: só o master da plataforma.
		// As empresas veem apenas os planos ativos (GET público acima).
		[HttpGet("all")]
		public async Task<IActionResult> GetAll()
		{
			if (!User.IsPlatformAdmin()) return StatusCode(StatusCodes.Status403Forbidden, "Apenas o administrador da plataforma pode gerenciar planos.");
			int empresaid = User.GetEmpresaId();
			var planos = await _planoService.GetAll(empresaid);
			return Ok(planos);
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetById(int id)
		{
			var plano = await _planoService.GetById(id);
			if (plano == null) return NotFound("Plano não encontrado.");
			return Ok(plano);
		}


		[HttpPost]
		public async Task<IActionResult> Create([FromBody] CreatePlanoRequest req)
		{
			if (!User.IsPlatformAdmin()) return StatusCode(StatusCodes.Status403Forbidden, "Apenas o administrador da plataforma pode gerenciar planos.");
			try
			{
				int empresaid = User.GetEmpresaId();
				req.EmpresaId = empresaid;
				var plano = await _planoService.Create(req);
				return Ok(plano);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> Update(int id, [FromBody] UpdatePlanoRequest req)
		{
			if (!User.IsPlatformAdmin()) return StatusCode(StatusCodes.Status403Forbidden, "Apenas o administrador da plataforma pode gerenciar planos.");
			try
			{
				var plano = await _planoService.Update(id, req);
				return Ok(plano);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		/// <summary>
		/// Exclui um plano. Se houver empresas usando-o, ele é desativado em vez de excluído.
		/// </summary>
		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id)
		{
			if (!User.IsPlatformAdmin()) return StatusCode(StatusCodes.Status403Forbidden, "Apenas o administrador da plataforma pode gerenciar planos.");
			try
			{
				if (id <= 0) return BadRequest("id inválido.");
				var result = await _planoService.Delete(id);
				return Ok(new
				{
					deleted = result.Excluido,
					disabled = result.Desativado,
					message = result.Mensagem
				});
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}
	}
}