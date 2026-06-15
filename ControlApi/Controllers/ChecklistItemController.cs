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

        public ChecklistItemController(IChecklistItemService service)
        {
            _service = service;
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateChecklistItemRequest req)
        {
            try
            {
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
                var __scope = await _service.GetById(id);
                if (__scope == null || __scope.EmpresaId != User.GetEmpresaId()) return NotFound("Item não encontrado.");
                var ok = await _service.Delete(id);
                return ok ? Ok(true) : BadRequest("Falha ao excluir.");
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