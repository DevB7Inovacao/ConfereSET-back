using Core.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace ControlApi.Controllers
{
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

        private IActionResult? ChecarPermissaoEscrita()
        {
            return User.IsAdminOrGerente() ? null : StatusCode(StatusCodes.Status403Forbidden, "Apenas administradores da empresa podem alterar cadastros.");
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateChecklistItemRequest req)
        {
            try
            {
                var semPermissao = ChecarPermissaoEscrita();
                if (semPermissao != null) return semPermissao;

                if (req == null) return BadRequest("Payload inválido.");
                req.EmpresaId = User.GetEmpresaId();
                var result = await _service.Create(req);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("byChecklist/{checklistId}")]
        public async Task<IActionResult> GetByChecklist(int checklistId)
        {
            try
            {
                var checklist = await _checklistService.GetById(checklistId);
                if (checklist == null || checklist.EmpresaId != User.GetEmpresaId()) return NotFound("Checklist não encontrado.");
                var result = await _service.GetByChecklist(checklistId);
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
                var result = await _service.GetById(id);
                if (result == null) return NotFound("Item não encontrado.");
            if (result.EmpresaId != User.GetEmpresaId()) return NotFound("Item não encontrado.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateChecklistItemRequest req)
        {
            try
            {
                var semPermissao = ChecarPermissaoEscrita();
                if (semPermissao != null) return semPermissao;

                var __scope = await _service.GetById(id);
                if (__scope == null || __scope.EmpresaId != User.GetEmpresaId()) return NotFound("Item não encontrado.");
                var ok = await _service.Update(id, req);
                return ok ? Ok(true) : BadRequest("Falha ao atualizar.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var semPermissao = ChecarPermissaoEscrita();
                if (semPermissao != null) return semPermissao;

                var __scope = await _service.GetById(id);
                if (__scope == null || __scope.EmpresaId != User.GetEmpresaId()) return NotFound("Item não encontrado.");
                var (ok, mensagem) = await _service.Delete(id);
                if (!ok) return BadRequest("Falha ao excluir.");
                return mensagem != null ? Ok(mensagem) : Ok(true);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("toggle-status/{id}")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var semPermissao = ChecarPermissaoEscrita();
                if (semPermissao != null) return semPermissao;

                var __scope = await _service.GetById(id);
                if (__scope == null || __scope.EmpresaId != User.GetEmpresaId()) return NotFound("Item não encontrado.");
                var ok = await _service.ToggleStatus(id);
                return ok ? Ok(true) : BadRequest("Falha ao alternar status.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}