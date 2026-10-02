using ControlApi;
using Core.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace ControlApi.Controllers
{
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

        private IActionResult? ChecarPermissaoEscrita()
        {
            return User.IsAdminOrGerente() ? null : StatusCode(StatusCodes.Status403Forbidden, "Apenas administradores da empresa podem alterar cadastros.");
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateChecklistRequest req)
        {
            try
            {
                var semPermissao = ChecarPermissaoEscrita();
                if (semPermissao != null) return semPermissao;

                if (req == null) return BadRequest("Payload inválido.");
                req.EmpresaId = User.GetEmpresaId();
                var created = await _service.Create(req);
                return Ok(created.Id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("getPaged")]
        public async Task<IActionResult> GetPaged([FromQuery] FiltersChecklistDTO filters)
        {
            // Multi-tenant: força o EmpresaId do JWT, ignorando query string.
            filters.EmpresaId = User.GetEmpresaId();
            try
            {
                var result = await _service.GetPaged(filters);
                return Ok(result);
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
                var model = await _service.GetById(id);
                if (model == null) return NotFound("Checklist não encontrado.");
            if (model.EmpresaId != User.GetEmpresaId()) return NotFound("Checklist não encontrado.");
                return Ok(model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateChecklistRequest req)
        {
            try
            {
                var semPermissao = ChecarPermissaoEscrita();
                if (semPermissao != null) return semPermissao;

                var __scope = await _service.GetById(id);
                if (__scope == null || __scope.EmpresaId != User.GetEmpresaId()) return NotFound("Checklist não encontrado.");
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
                if (__scope == null || __scope.EmpresaId != User.GetEmpresaId()) return NotFound("Checklist não encontrado.");
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
                if (__scope == null || __scope.EmpresaId != User.GetEmpresaId()) return NotFound("Checklist não encontrado.");
                var ok = await _service.ToggleStatus(id);
                return ok ? Ok(true) : BadRequest("Falha ao alternar status.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        public class DuplicarChecklistRequest
        {
            public string? Nome { get; set; }
        }

        /// <summary>[v2] Duplica o modelo com todos os itens ativos.</summary>
        [HttpPost("{id}/duplicar")]
        public async Task<IActionResult> Duplicar(int id, [FromBody] DuplicarChecklistRequest? req)
        {
            try
            {
                var semPermissao = ChecarPermissaoEscrita();
                if (semPermissao != null) return semPermissao;

                var __scope = await _service.GetById(id);
                if (__scope == null || __scope.EmpresaId != User.GetEmpresaId()) return NotFound("Checklist não encontrado.");
                var result = await _service.Duplicar(id, req?.Nome);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
