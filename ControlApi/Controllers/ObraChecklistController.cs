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
		private const string MsgConcluido = "Checklist concluído. Peça ao gestor para reabrir.";

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

		/// <summary>[v2] Execução concluída fica travada para quem não é gestor (ele pode reabrir).</summary>
		private IActionResult? ChecarConcluido(ObraChecklistEscopoDTO escopo)
		{
			if (escopo.Concluido && !User.IsAdminOrGerente())
				return StatusCode(StatusCodes.Status403Forbidden, MsgConcluido);
			return null;
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
				if (req == null) return BadRequest("Payload inválido.");
				if (!await ObraPertenceAEmpresa(req.ObraId)) return NotFound("Obra não encontrada.");
				if (await _service.GetChecklistEmpresaId(req.ChecklistId) != User.GetEmpresaId()) return NotFound("Checklist não encontrado.");

				// Vincular um checklist é do gestor. [v2] O operador vinculado à obra pode iniciar uma
				// NOVA RODADA de um checklist que já foi aplicado na obra (inspeção diária/semanal).
				if (!User.IsAdminOrGerente())
				{
					if (!User.IsOperador()) return StatusCode(StatusCodes.Status403Forbidden, MsgSemPermissao);
					if (!await ObraVisivel(req.ObraId)) return NotFound("Obra não encontrada.");
					if (!await _service.JaAplicadoNaObra(req.ObraId, req.ChecklistId))
						return StatusCode(StatusCodes.Status403Forbidden, "Peça ao gestor para aplicar este checklist na obra.");
				}

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

				var escopo = await _service.GetEscopoByItemId(obraChecklistItemId);
				if (escopo == null || !await ObraVisivel(escopo.ObraId))
					return NotFound("Item não encontrado.");
				var bloqueio = ChecarConcluido(escopo);
				if (bloqueio != null) return bloqueio;

				var ok = await _service.ResponderItem(obraChecklistItemId, req, User.GetUserId());
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

				var escopo = await _service.GetEscopoByItemId(obraChecklistItemId);
				if (escopo == null || !await ObraVisivel(escopo.ObraId))
					return NotFound("Item não encontrado.");
				var bloqueio = ChecarConcluido(escopo);
				if (bloqueio != null) return bloqueio;

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

		// =====================================================================
		// [Conferelist v2] Fotos dos itens
		// =====================================================================

		[HttpPost("item/{obraChecklistItemId}/fotos")]
		[RequestSizeLimit(150_000_000)]
		public async Task<IActionResult> AddFotos(int obraChecklistItemId, [FromBody] List<AddChecklistItemFotoRequest> fotos)
		{
			try
			{
				var semPermissao = ChecarPermissaoResposta();
				if (semPermissao != null) return semPermissao;

				var escopo = await _service.GetEscopoByItemId(obraChecklistItemId);
				if (escopo == null || !await ObraVisivel(escopo.ObraId))
					return NotFound("Item não encontrado.");
				var bloqueio = ChecarConcluido(escopo);
				if (bloqueio != null) return bloqueio;

				var result = await _service.AddFotos(obraChecklistItemId, fotos, User.GetUserId());
				return Ok(result);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpPut("foto/{fotoId}")]
		public async Task<IActionResult> UpdateFoto(int fotoId, [FromBody] UpdateChecklistItemFotoRequest req)
		{
			try
			{
				var semPermissao = ChecarPermissaoResposta();
				if (semPermissao != null) return semPermissao;

				var escopo = await _service.GetEscopoByFotoId(fotoId);
				if (escopo == null || !await ObraVisivel(escopo.ObraId))
					return NotFound("Foto não encontrada.");
				var bloqueio = ChecarConcluido(escopo);
				if (bloqueio != null) return bloqueio;

				var ok = await _service.UpdateFotoLegenda(fotoId, req?.Legenda);
				return ok ? Ok(true) : BadRequest("Falha ao salvar a legenda.");
			}
			catch (KeyNotFoundException ex)
			{
				return NotFound(ex.Message);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpDelete("foto/{fotoId}")]
		public async Task<IActionResult> DeleteFoto(int fotoId)
		{
			try
			{
				var semPermissao = ChecarPermissaoResposta();
				if (semPermissao != null) return semPermissao;

				var escopo = await _service.GetEscopoByFotoId(fotoId);
				if (escopo == null || !await ObraVisivel(escopo.ObraId))
					return NotFound("Foto não encontrada.");
				var bloqueio = ChecarConcluido(escopo);
				if (bloqueio != null) return bloqueio;

				await _service.DeleteFoto(fotoId);
				return Ok(true);
			}
			catch (KeyNotFoundException ex)
			{
				return NotFound(ex.Message);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		// =====================================================================
		// [Conferelist v2] Concluir / reabrir
		// =====================================================================

		[HttpPost("{id}/concluir")]
		[RequestSizeLimit(20_000_000)]
		public async Task<IActionResult> Concluir(int id, [FromBody] ConcluirObraChecklistRequest req)
		{
			try
			{
				var semPermissao = ChecarPermissaoResposta();
				if (semPermissao != null) return semPermissao;

				var escopo = await _service.GetEscopo(id);
				if (escopo == null || !await ObraVisivel(escopo.ObraId))
					return NotFound("Checklist não encontrado.");

				var result = await _service.Concluir(id, req ?? new ConcluirObraChecklistRequest(), User.GetUserId());
				return Ok(result);
			}
			catch (PendenciasException ex)
			{
				return BadRequest(new { message = ex.Message, pendencias = ex.Pendencias });
			}
			catch (KeyNotFoundException ex)
			{
				return NotFound(ex.Message);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpPost("{id}/reabrir")]
		public async Task<IActionResult> Reabrir(int id)
		{
			try
			{
				var semPermissao = ChecarPermissaoEscrita();
				if (semPermissao != null) return semPermissao;

				var escopo = await _service.GetEscopo(id);
				if (escopo == null || !await ObraPertenceAEmpresa(escopo.ObraId))
					return NotFound("Checklist não encontrado.");

				var result = await _service.Reabrir(id);
				return Ok(result);
			}
			catch (KeyNotFoundException ex)
			{
				return NotFound(ex.Message);
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}
		}
	}
}
