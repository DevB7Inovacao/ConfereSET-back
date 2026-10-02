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
		private const string MsgSemPermissao = "Apenas administradores da empresa podem alterar cadastros.";
		private const string MsgSomenteLeitura = "Usuários somente leitura não podem responder checklists.";

		private readonly IObraChecklistService _service;
		private readonly IObrasService _obrasService;

		public ObraChecklistController(IObraChecklistService service, IObrasService obrasService)
		{
			_service = service;
			_obrasService = obrasService;
		}

		private IActionResult? ChecarPermissaoEscrita()
		{
			return User.IsAdminOrGerente() ? null : StatusCode(StatusCodes.Status403Forbidden, MsgSemPermissao);
		}

		// Operadores preenchem checklists; apenas somente leitura é bloqueado.
		private IActionResult? ChecarPermissaoResposta()
		{
			return User.IsReadOnly() ? StatusCode(StatusCodes.Status403Forbidden, MsgSomenteLeitura) : null;
		}

		private async Task<bool> ObraPertenceAEmpresa(int? obraId)
		{
			if (!obraId.HasValue) return false;
			var obra = await _obrasService.GetObraById(obraId.Value);
			return obra != null && obra.EmpresaId == User.GetEmpresaId();
		}

		/// <summary>
		/// Escopo de empresa + vínculo: operador (type 2) só acessa checklists de obras às quais está
		/// vinculado (ObraOperador). Admin/gerente e somente leitura mantêm o escopo da empresa.
		/// </summary>
		private async Task<bool> ObraVisivel(int? obraId)
		{
			if (!await ObraPertenceAEmpresa(obraId)) return false;
			if (!User.IsOperador()) return true;
			return await _obrasService.IsOperadorVinculado(obraId!.Value, User.GetUserId());
		}

		[HttpPost("add")]
		public async Task<IActionResult> AddChecklistToObra([FromBody] AddChecklistToObraRequest req)
		{
			try
			{
				var semPermissao = ChecarPermissaoEscrita();
				if (semPermissao != null) return semPermissao;

				if (req == null) return BadRequest("Payload inválido.");
				if (!await ObraPertenceAEmpresa(req.ObraId)) return NotFound("Obra não encontrada.");
				if (await _service.GetChecklistEmpresaId(req.ChecklistId) != User.GetEmpresaId()) return NotFound("Checklist não encontrado.");

				var result = await _service.AddChecklistToObra(req);
				return Ok(result);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}


		[HttpGet("{id}")]
		public async Task<IActionResult> GetById(int id)
		{
			try
			{
				if (!await ObraVisivel(await _service.GetObraIdByObraChecklistId(id)))
					return NotFound("Vínculo de checklist não encontrado.");

				var result = await _service.GetById(id);
				return Ok(result);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpGet("byObra/{obraId}")]
		public async Task<IActionResult> GetByObra(int obraId)
		{
			try
			{
				var empresaIdJwt = User.GetEmpresaId();
				if (User.IsOperador() && !await _obrasService.IsOperadorVinculado(obraId, User.GetUserId()))
					return NotFound("Obra não encontrada.");

				var result = await _service.GetByObra(obraId, empresaIdJwt);
				return Ok(result);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}
		[HttpGet("empresa/{empresaId}")]
		public async Task<IActionResult> GetByObraEmpresa(int empresaId)
		{
			try
			{
				// Multi-tenant: força a empresa do JWT, ignorando o path param.
				var empresaIdJwt = User.GetEmpresaId();
				var result = await _service.GetByObraEmpresa(empresaIdJwt);
				if (User.IsOperador())
				{
					var vinculadas = (await _obrasService.GetObraIdsByOperadorId(User.GetUserId())).ToHashSet();
					result = result.Where(x => vinculadas.Contains(x.ObraId)).ToList();
				}
				return Ok(result);
			}
			catch (Exception ex)
			{
				return BadRequest("Erro ao obter checklists da empresa: " + ex.Message);
			}
		}

		[HttpPut("responder/{obraChecklistItemId}")]
		public async Task<IActionResult> ResponderItem(int obraChecklistItemId, [FromBody] ResponderChecklistItemRequest req)
		{
			try
			{
				var semPermissao = ChecarPermissaoResposta();
				if (semPermissao != null) return semPermissao;

				if (!await ObraVisivel(await _service.GetObraIdByItemId(obraChecklistItemId)))
					return NotFound("Item não encontrado.");

				var ok = await _service.ResponderItem(obraChecklistItemId, req);
				return ok ? Ok(true) : BadRequest("Falha ao responder item.");
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}
		[HttpPut("item/{obraChecklistItemId}/metadata")]
		public async Task<IActionResult> UpdateItemMetadata(int obraChecklistItemId, [FromBody] ResponderChecklistItemRequest req)
		{
			try
			{
				var semPermissao = ChecarPermissaoResposta();
				if (semPermissao != null) return semPermissao;

				if (!await ObraVisivel(await _service.GetObraIdByItemId(obraChecklistItemId)))
					return NotFound("Item não encontrado.");

				var ok = await _service.ResponderItensAdicionais(obraChecklistItemId, req);
				return ok ? Ok(true) : BadRequest("Falha ao salvar informações do item.");
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}


		[HttpDelete("{obraChecklistId}")]
		public async Task<IActionResult> RemoveChecklistFromObra(int obraChecklistId)
		{
			try
			{
				var semPermissao = ChecarPermissaoEscrita();
				if (semPermissao != null) return semPermissao;

				if (!await ObraPertenceAEmpresa(await _service.GetObraIdByObraChecklistId(obraChecklistId)))
					return NotFound("Vínculo de checklist não encontrado.");

				var ok = await _service.RemoveChecklistFromObra(obraChecklistId);
				return ok ? Ok(true) : BadRequest("Falha ao remover checklist.");
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpPost("sincronizar/{checklistId}")]
		public async Task<IActionResult> SincronizarChecklist(int checklistId)
		{
			try
			{
				var semPermissao = ChecarPermissaoEscrita();
				if (semPermissao != null) return semPermissao;

				if (checklistId <= 0) return BadRequest("checklistId inválido.");
				if (await _service.GetChecklistEmpresaId(checklistId) != User.GetEmpresaId()) return NotFound("Checklist não encontrado.");

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
