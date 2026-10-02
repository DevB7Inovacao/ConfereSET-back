using Core.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace ControlApi.Controllers
{
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

        private IActionResult? ChecarPermissaoEscrita()
        {
            return User.IsAdminOrGerente() ? null : StatusCode(StatusCodes.Status403Forbidden, "Apenas administradores da empresa podem alterar cadastros.");
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateModeloTextoVariavelRequest req)
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
        public async Task<IActionResult> GetPaged([FromQuery] FiltersModeloTextoVariavelDTO filters)
        {
            try
            {
                filters.EmpresaId = User.GetEmpresaId();
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
                if (model == null || model.EmpresaId != User.GetEmpresaId()) return NotFound("Variável não encontrada.");
                return Ok(model);
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
                var semPermissao = ChecarPermissaoEscrita();
                if (semPermissao != null) return semPermissao;

                var __scope = await _service.GetById(id);
                if (__scope == null || __scope.EmpresaId != User.GetEmpresaId()) return NotFound("Variável não encontrada.");
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
                if (__scope == null || __scope.EmpresaId != User.GetEmpresaId()) return NotFound("Variável não encontrada.");
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
                var semPermissao = ChecarPermissaoEscrita();
                if (semPermissao != null) return semPermissao;

                var __scope = await _service.GetById(id);
                if (__scope == null || __scope.EmpresaId != User.GetEmpresaId()) return NotFound("Variável não encontrada.");
                var ok = await _service.ToggleStatus(id);
                return ok ? Ok(true) : BadRequest("Falha ao alternar status.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("sync")]
        public async Task<IActionResult> Sync([FromBody] SyncModeloTextoVariavelRequest req)
        {
            try
            {
                var semPermissao = ChecarPermissaoEscrita();
                if (semPermissao != null) return semPermissao;

                if (req == null) return BadRequest("Payload inválido.");
                req.EmpresaId = User.GetEmpresaId();
                var result = await _service.Sync(req);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("getByModelo")]
        public async Task<IActionResult> GetByModelo([FromQuery] int empresaId, [FromQuery] int modeloTextoId, [FromQuery] bool onlyActiveLinks = true)
        {
            try
            {
                // Multi-tenant: força a empresa do JWT, ignorando a query string.
                empresaId = User.GetEmpresaId();
                var result = await _service.GetByModelo(empresaId, modeloTextoId, onlyActiveLinks);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("render/{modeloTextoId}")]
        public async Task<IActionResult> Render([FromRoute] int modeloTextoId, [FromQuery] int empresaId, [FromBody] RenderModeloTextoRequest req)
        {
            try
            {
                empresaId = User.GetEmpresaId();
                var result = await _service.Render(empresaId, modeloTextoId, req);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}