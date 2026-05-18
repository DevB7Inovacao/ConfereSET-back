using Core.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace ControlApi.Controllers
{
    /// <summary>
    /// Modelos de texto (templates) por empresa.
    /// <para>
    /// Regras de autorização:
    /// <list type="bullet">
    /// <item>Todos os endpoints exigem JWT válido (<c>[Authorize]</c>).</item>
    /// <item>EmpresaId é forçado pelo JWT em criação, listagem e leitura — body/query do cliente é ignorado.</item>
    /// <item>Operações de escrita (Create, Update, Delete, ToggleStatus) exigem admin/gerente.</item>
    /// <item>GetById/Update/Delete/ToggleStatus validam que o recurso pertence à empresa do chamador.</item>
    /// </list>
    /// </para>
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ModeloTextoController : ControllerBase
    {
        private readonly IModeloTextoService _service;
    private readonly IRelatorioService _relatorioService;

        public ModeloTextoController(IModeloTextoService service,IRelatorioService relatorioService)
        {
            _service = service;
      _relatorioService = relatorioService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateModeloTextoRequest req)
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
        public async Task<IActionResult> GetPaged([FromQuery] FiltersModeloTextoDTO filters)
        {
            try
            {
                var empresaId = User.GetEmpresaId();
                filters ??= new FiltersModeloTextoDTO();
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
                if (model == null) return NotFound("Modelo não encontrado.");

                if (model.EmpresaId != empresaId)
                   