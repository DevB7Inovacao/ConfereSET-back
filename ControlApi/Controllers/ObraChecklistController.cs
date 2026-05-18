using Core.DTO;
using Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;
using System.Linq;

namespace ControlApi.Controllers
{
	/// <summary>
	/// Endpoints de vínculo de checklists a obras.
	/// <para>
	/// Regras:
	/// <list type="bullet">
	/// <item><b>Vincular/Desvincular/Sincronizar</b>: admin/gerente.</item>
	/// <item><b>Responder item / metadata</b>: operador também pode (é a operação dele em campo).</item>
	/// <item><b>Empresa</b>: validado pela <c>Obra.EmpresaId</c> do checklist vinculado.</item>
	/// </list>
	/// </para>
	/// </summary>
	[Authorize]
	[Route("api/[controller]")]
	[ApiController]
	public class ObraChecklistController : ControllerBase
	{
		private readonly IObraChecklistService _service;
		private readonly IObrasService _obrasService;

		public ObraChecklistController(IObraChecklistService service, IObrasService obrasService)
		{
			_service = service;
			_obrasService = obrasService;
		}

		private async Task<(Obras? obra, IActionResult? denied)> LoadObraAndAssertEmpresa(int obraId)
		{
			var obra = await _obrasService.GetObraById(obraId);
			if (obra == null) return (null, BadRequest("Obra informada não encontrada."));

			var empresaJwt = User.GetEmpresaId();
			if (obra.EmpresaId != empresaJwt)
				return (null, StatusCode(StatusCodes.Status403Forbidden, "Obra pertence a outra empresa."));

			return (obra, null);
		}

		private IActionResult? AssertAdminOrGerente()
		{
			if (!User.IsAdminOrGerente())
				return StatusCode(StatusCodes.Status403Forbidden, "Apenas admin/gerente podem realizar esta ação.");
			return null;
		}

		[HttpPost("add")]
		public async Task<IActionResult> AddChecklistToObra([FromBody] AddChecklistToObraRequest req)
		{
			try
			{
				if (req == null) return BadRequest("Payload inválido.");
				var deny = AssertAdminOrGerente();
				if (deny != null) return deny;

				var (obra, denied) = await LoadObraAndAssertEmpresa(req.ObraId);
				if (obra == null) return denied!;

				var result = await _service.AddChecklistToObra(req);
				return Ok(result);
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


		[HttpGet("{id}")]
		public async Task<IActionResult> GetById(int id)
		{
			try
			{
				if (id <= 0) return BadRequest("id inválido.");

				var result = await _service.GetById(id);
				if (result == null) return NotFound("Vínculo não encontrado.");

				var (obra, denied) = await LoadObraAndAssertEmpresa(result.ObraId);
				if (obra == null) return denied!;

				return Ok(result);
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

		[HttpGet("byObra/{obraId}")]
		public async Task<IActionResult> GetByObra(int obraId)
		{
			try
			{
				if (obraId <= 0) return BadRequest("obraId inválido.");

				var (obra, denied) = await LoadObraAndAssertEmpresa(obraId);
				if (obra == null) return denied!;

				var result = await _service.GetByObra(obraId);
				return Ok(result);
			}
			catch (