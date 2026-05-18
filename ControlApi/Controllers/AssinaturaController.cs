using Core.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace ControlApi.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AssinaturaController : ControllerBase
	{
		private readonly IAssinaturaService _assinaturaService;

		public AssinaturaController(IAssinaturaService assinaturaService)
		{
			_assinaturaService = assinaturaService;
		}

		[AllowAnonymous]
		[HttpPost("checkout")]
		public async Task<IActionResult> IniciarCheckout([FromBody] CreateAssinaturaRequest req)
		{
			try
			{
				var result = await _assinaturaService.IniciarCheckout(req);
				return Ok(result);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}
		[HttpPost("callback")]
		public async Task<IActionResult> Callback([FromQuery] string preapproval_id)
		{
			try
			{
				var result = await _assinaturaService.CallBack(preapproval_id);
				return Ok(result);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}
	
		[HttpPut("{id}/atualizar")]
		public async Task<IActionResult> Atualizar(int id, [FromBody] AtualizarAssinaturaRequest req)
		{
			try
			{
				var result = await _assinaturaService.AtualizarAssinatura(id, req.NovoValor, req.CardToken);
				return result ? Ok(true) : BadRequest("Falha ao atualizar assinatura.");
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[AllowAnonymous]
		[HttpPost("vitalicio")]
		public async Task<IActionResult> AtribuirVitalicio([FromBody] AtribuirPlanoVitalicioRequest req)
		{
			try
			{
				var result = await _assinaturaService.AtribuirPlanoVitalicio(req.EmpresaId, req.PlanoId);
				return Ok(result);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[AllowAnonymous]
		[HttpGet("empresa/{empresaId}")]
		public async Task<IActionResult> GetByEmpresaId(int empresaId)
		{
			var assinatura = await _assinaturaService.GetByEmpresaId(empresaId);
			if (assinatura == null) return NotFound("Nenhuma assinatura ativa encontrada.");
			return Ok(assinatura);
		}

		[AllowAnonymous]
		[HttpGet("{id}")]
		public async Task<IActionResult> GetById(int id)
		{
			var assinatura = await _assinaturaService.GetById(id);
			if (assinatura == null) return NotFound("Assinatura não encontrada.");
			return Ok(assinatura);
		}

		[AllowAnonymous]
		[HttpPost("{id}/cancelar")]
		public async Task<IActionResult> Cancelar(int id)
		{
			try
			{
				var result = await _assinaturaService.Cancelar(id);
				return result ? Ok(true) : BadRequest("Falha ao cancelar assinatura.");
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[Authorize]
		[HttpGet("all")]
		public async Task<IActionResult> GetAllPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
		{
			int empresaId = User.GetEmpresaId();
			var (items, total) = await _assinaturaService.GetAllPaged(page, pageSize,empresaId);
			return Ok(new { items, total });
		}

		[AllowAnonymous]
		[HttpGet("empresa/{empresaId}/limites")]
		public async Task<IActionResult> VerificarLimites(int empresaId)
		{
			var limites = await _assinaturaService.VerificarLimites(empresaId);
			return Ok(limites);
		}
	}
}