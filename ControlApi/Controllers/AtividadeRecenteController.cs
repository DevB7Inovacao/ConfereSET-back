using Core.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace ControlApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AtividadeRecenteController : ControllerBase
    {
        private readonly IAtividadeRecenteService _service;

        public AtividadeRecenteController(IAtividadeRecenteService service)
        {
            _service = service;
        }

        [AllowAnonymous]
        [HttpGet("operador/{operadorId}")]
        public async Task<IActionResult> GetByOperador(int operadorId, [FromQuery] FiltersAtividadeRecenteDTO filters)
        {
            try
            {
                if (operadorId <= 0) return BadRequest("operadorId inválido.");
                var result = await _service.GetPagedByOperadorId(operadorId, filters);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet("empresa/{empresaId}")]
        public async Task<IActionResult> GetByEmpresa(int empresaId, [FromQuery] FiltersAtividadeRecenteDTO filters)
        {
            try
            {
                if (empresaId <= 0) return BadRequest("empresaId inválido.");
                var result = await _service.GetPagedByEmpresaId(empresaId, filters);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}