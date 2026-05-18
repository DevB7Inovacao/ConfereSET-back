using Core.DTO;
using Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace ControlApi.Controllers
{
    /// <summary>
    /// CRUD de checklists (templates por empresa). Todas as operações são escopadas pela
    /// empresa do JWT — o cliente nunca decide a empresa. Escrita restrita a admin/gerente.
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ChecklistController : ControllerBase
    {
        private readonly IChecklistService _service;

        public ChecklistController(IChecklistService service)
        {
            _service = service;
        }

        /// <summary>
        /// Garante que o checklist existe e pertence à empresa do JWT antes de operações
        /// destrutivas. Retorna <c>NotFound</c> quando não existe e <c>403</c> quando é de
        /// outra empresa (não revelando, ao cliente externo, a existência do recurso).
        /// </summary>
        private async Task<(bool ok, IActionResult? denied, Checklist? entity)> LoadAndAssertEmpresa(int id)
        {
            var entity = await _service.GetById(id);
            if (entity == null) return (false, NotFound("Checklist não encontrado."), null);

            var empresaJwt = User.GetEmpresaId();
            if (entity.EmpresaId != empresaJwt)
                return (false, StatusCode(StatusCodes.Status403Forbidden, "Checklist não pertence à sua empresa."), null);

            return (true, null, entity);
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateChecklistRequest req)
        {
            try
            {
                if (req == null) return BadRequest("Payload inválido.");

                if (!User.IsAdminOrGerente())
                    return StatusCode(StatusCodes.Status403Forbidden, "Apenas admin/gerente pode criar checklists.");

                // EmpresaId vem sempre do JWT — body é ignorado por segurança.
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
        public async Task<IActionResult> GetPaged([FromQuery] FiltersChecklistDTO filters)
        {
            try
            {
                filters ??= new FiltersChecklistDTO();

                // Escopo de empresa sempre do JWT, sobrepondo qualquer valor da query.
                filters.EmpresaId = User.GetEmpresaId();

                var result = await _service.GetPaged(filters);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(Sta