using Core.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace ControlApi.Controllers
{
    /// <summary>
    /// CRUD de itens de checklist. Toda escrita exige admin/gerente, e leitura/escrita são
    /// escopadas pela empresa do JWT (validando o <c>EmpresaId</c> do item e do checklist
    /// associado).
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ChecklistItemController : ControllerBase
    {
        private readonly IChecklistItemService _service;
        private readonly IChecklistService _checklistService;

        public ChecklistItemController(IChecklistItemService service, IChecklistService checklistService)
        {
            _service = service;
            _checklistService = checklistService;
        }

        private async Task<(bool ok, IActionResult? denied, ChecklistItemDTO? entity)> LoadAndAssertEmpresa(int id)
        {
            var entity = await _service.GetById(id);
            if (entity == null) return (false, NotFound("Item não encontrado."), null);

            var empresaJwt = User.GetEmpresaId();
            if (entity.EmpresaId != empresaJwt)
                return (false, StatusCode(StatusCodes.Status403Forbidden, "Item não pertence à sua empresa."), null);

            return (true, null, entity);
        }

        private async Task<(bool ok, IActionResult? denied)> AssertChecklistEmpresa(int checklistId)
        {
            var checklist = await _checklistService.GetById(checklistId);
            if (checklist == null) return (false, NotFound("Checklist não encontrado."));

            var empresaJwt = User.GetEmpresaId();
            if (checklist.EmpresaId != empresaJwt)
                return (false, StatusCode(StatusCodes.Status403Forbidden, "Checklist não pertence à sua empresa."));

            return (true, null);
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateChecklistItemRequest req)
        {
            try
            {
                if (req == null) return BadRequest("Payload inválido.");

                if (!User.IsAdminOrGerente())
                    return StatusCode(StatusCodes.Status403Forbidden, "Apenas admin/gerente pode criar itens.");

                // EmpresaId vem sempre do JWT. O service valida a consistência entre o
                // ChecklistId informado e a empresa.
                req.EmpresaId = User.GetEmpresaId();

                // Defesa extra: o checklist alvo precisa pertencer à empresa do JWT.
                var (ok, denied) = await AssertChecklistEmpresa(req.ChecklistId);
                if (!ok) return denied!;

                var result = await _service.Create(req);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (Exception ex)
            {
                return Ba