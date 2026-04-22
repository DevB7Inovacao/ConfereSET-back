using Core.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace ControlApi.Controllers
{
	[Authorize]
	[Route("api/[controller]")]
	[ApiController]
	public class ObraChecklistController : ControllerBase
	{
		private readonly IObraChecklistService _service;

		public ObraChecklistController(IObraChecklistService service)
		{
			_service = service;
		}

		[AllowAnonymous]
		[HttpPost("add")]
		public async Task<IActionResult> AddChecklistToObra([FromBody] AddChecklistToObraRequest req)
		{
			try
			{
				var result = await _service.AddChecklistToObra(req);
				return Ok(result);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}


		[AllowAnonymous]
		[HttpGet("{id}")]
		public async Task<IActionResult> GetById(int id)
		{
			try
			{
				var result = await _service.GetById(id);
				return Ok(result);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[AllowAnonymous]
		[HttpGet("byObra/{obraId}")]
		public async Task<IActionResult> GetByObra(int obraId)
		{
			try
			{
				var result = await _service.GetByObra(obraId);
				return Ok(result);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[AllowAnonymous]
		[HttpPut("responder/{obraChecklistItemId}")]
		public async Task<IActionResult> ResponderItem(int obraChecklistItemId, [FromBody] ResponderChecklistItemRequest req)
		{
			try
			{
				var ok = await _service.ResponderItem(obraChecklistItemId, req);
				return ok ? Ok(true) : BadRequest("Falha ao responder item.");
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}
		[AllowAnonymous]
		[HttpPut("item/{obraChecklistItemId}/metadata")]
		public async Task<IActionResult> UpdateItemMetadata(int obraChecklistItemId, [FromBody] ResponderChecklistItemRequest req)
		{
			try
			{
				var ok = await _service.ResponderItensAdicionais(obraChecklistItemId, req);
				return ok ? Ok(true) : BadRequest("Falha ao responder item.");
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}


		[AllowAnonymous]
		[HttpDelete("{obraChecklistId}")]
		public async Task<IActionResult> RemoveChecklistFromObra(int obraChecklistId)
		{
			try
			{
				var ok = await _service.RemoveChecklistFromObra(obraChecklistId);
				return ok ? Ok(true) : BadRequest("Falha ao remover checklist.");
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[AllowAnonymous]
		[HttpPost("sincronizar/{checklistId}")]
		public async Task<IActionResult> SincronizarChecklist(int checklistId)
		{
			try
			{
				if (checklistId <= 0) return BadRequest("checklistId inválido.");
				var ok = await _service.SincronizarChecklist(checklistId);
				return ok ? Ok(true) : NotFound("Checklist não encontrado.");
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}
	}
}