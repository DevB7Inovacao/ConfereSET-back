using Core.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace ControlApi.Controllers
{
    /// <summary>
    /// Variáveis (tokens) de modelos de texto, por empresa.
    /// <para>
    /// Regras de autorização:
    /// <list type="bullet">
    /// <item>Todos os endpoints exigem JWT válido (<c>[Authorize]</c>).</item>
    /// <item>EmpresaId é forçado pelo JWT em criação, listagem, sync, render e leitura — body/query do cliente é ignorado.</item>
    /// <item>Operações de escrita (Create, Update, Delete, ToggleStatus, Sync) exigem admin/gerente.</item>
    /// <item>GetById/Update/Delete/ToggleStatus validam que o recurso pertence à empresa do chamador.</item>
    /// </list>
    /// </para>
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ModeloTextoVariavelController : ControllerBase
    {
        private readonly IModeloTextoVariavelService _service;

        public ModeloTextoVariavelController(IModeloTextoVariavelService service)
        {
            _service = service;
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateModeloTextoVariavelRequest req)
        {
            try
            {
                if (!User.IsAdminOrGerente())
                    return StatusCode(StatusCodes.Status403Forbidden, "Apenas admin/gerente podem realizar esta ação.");

                if (req == null) return BadRequest("Payload inválido.");

                // EmpresaId sempre vem do JWT; sobrescreve qualquer valor enviado no body.
                req.EmpresaId = User.GetEmpresaId();

                var created = await _service.Create(req);
                return Ok(created.Id);
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

        [HttpGet("getPaged")]
        public async Task<IActionResult> GetPaged([FromQuery] FiltersModeloTextoVariavelDTO filters)
        {
            try
            {
                var empresaId = User.GetEmpresaId();
                filters ??= new FiltersModeloTextoVariavelDTO();
                filters.EmpresaId = empresaId;

                var result = await _service.GetPaged(filters);
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

        [HttpGet("getById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var empresaId = User.GetEmpresaId();

                var model = await _service.GetById(id);
                if (model == null) return NotFound("Variável não encontrada.");

                if (model.EmpresaId != empresaId)
                    return StatusCode(StatusCodes.Status403Forbidden, "Recurso pertence a outra empresa.");

                return Ok(model);
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

        [HttpPut("update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateModeloTextoVariavelRequest req)
        {
            try
            {
                if (!User.IsAdminOrGerente())
                    return StatusCode(StatusCodes.Status403Forbidden, "Apenas admin/gerente podem realizar esta ação.");

                var empresaId = User.GetEmpresaId();

                var existing = await _service.GetById(id);
                if (existing == null) return NotFound("Variável não encontrada.");
                if (existing.EmpresaId != empresaId)
                    return StatusCode(StatusCodes.Status403Forbidden, "Recurso pertence a outra empresa.");

                var ok = await _service.Update(id, req);
                return ok ? Ok(true) : BadRequest("Falha ao atualizar.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return Sta