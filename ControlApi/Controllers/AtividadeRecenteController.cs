using Core.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace ControlApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AtividadeRecenteController : ControllerBase
    {
        private readonly IAtividadeRecenteService _service;
        private readonly IUserService _userService;

        public AtividadeRecenteController(IAtividadeRecenteService service, IUserService userService)
        {
            _service = service;
            _userService = userService;
        }

        [HttpGet("operador/{operadorId}")]
        public async Task<IActionResult> GetByOperador(int operadorId, [FromQuery] FiltersAtividadeRecenteDTO filters)
        {
            try
            {
                if (operadorId <= 0) return BadRequest("operadorId inválido.");

                // Operador / somente leitura só podem consultar a própria atividade.
                if (!User.IsAdminOrGerente() && operadorId != User.GetUserId())
                    return StatusCode(StatusCodes.Status403Forbidden, "Sem permissão para consultar a atividade de outro usuário.");

                var operador = await _userService.GetUserById(operadorId);
                if (operador == null || (operador.EmpresaId != User.GetEmpresaId() && !User.IsPlatformAdmin()))
                    return NotFound("Operador não encontrado.");

                var result = await _service.GetPagedByOperadorId(operadorId, filters);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("empresa/{empresaId}")]
        public async Task<IActionResult> GetByEmpresa(int empresaId, [FromQuery] FiltersAtividadeRecenteDTO filters)
        {
            try
            {
                // Multi-tenant: só o admin da plataforma pode consultar outra empresa pelo path.
                if (!User.IsPlatformAdmin()) empresaId = User.GetEmpresaId();
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
