using ControlApi;
using Core.DTO;
using Core.Enums;
using Infrastructure.Authenticate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace ControlApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class OcorrenciaController : ControllerBase
    {
        private readonly IOcorrenciaService _service;

        public OcorrenciaController(IOcorrenciaService service)
        {
            _service = service;
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateOcorrenciaRequest req)
        {
            try
            {
                if (req == null) return BadRequest("Payload inválido.");
                if (string.IsNullOrWhiteSpace(req.Titulo)) return BadRequest("Título é obrigatório.");
                var empresaId = User.GetEmpresaId();

                var result = await _service.Create(req, empresaId);
                return Ok(result.Id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("getPaged")]
        public async Task<IActionResult> GetPaged([FromQuery] FiltersOcorrenciaDTO filters)
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
                if (id <= 0) return BadRequest("id inválido.");
                var empresaId = User.GetEmpresaId();
                var result = await _service.GetById(id, empresaId);
                if (result == null) return NotFound("Ocorrência não encontrada.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("obra/{obraId}")]
        public async Task<IActionResult> GetByObraId(int obraId)
        {
            try
            {
                if (obraId <= 0) return BadRequest("obraId inválido.");
                var empresaId = User.GetEmpresaId();
                var result = await _service.GetByObraId(obraId, empresaId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateOcorrenciaRequest req)
        {
            try
            {
                if (id <= 0) return BadRequest("id inválido.");
                if (req == null) return BadRequest("Payload inválido.");

                var empresaId = User.GetEmpresaId();
                var ok = await _service.Update(id, req, empresaId);
                return ok ? Ok(true) : BadRequest("Falha ao atualizar ocorrência.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOcorrenciaStatusRequest req)
        {
            try
            {
                if (id <= 0) return BadRequest("id inválido.");
                if (req == null) return BadRequest("Payload inválido.");

                var empresaId = User.GetEmpresaId();
                var ok = await _service.UpdateStatus(id, req.Status, empresaId);
                return ok ? Ok(true) : BadRequest("Falha ao atualizar status.");
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
                if (id <= 0) return BadRequest("id inválido.");
                var empresaId = User.GetEmpresaId();
                var ok = await _service.Delete(id, empresaId);
                return ok ? Ok("Ocorrência excluída com sucesso.") : BadRequest("Falha ao excluir ocorrência.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}