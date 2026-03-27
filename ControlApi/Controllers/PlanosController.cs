using Core.DTO;
using Infrastructure.Authenticate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace ControlApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlanosController : ControllerBase
    {
        private readonly IPlanoService _planoService;

        public PlanosController(IPlanoService planoService)
        {
            _planoService = planoService;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAtivos()
        {
            var planos = await _planoService.GetAtivos();
            return Ok(planos);
        }

        [AllowAnonymous]
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var planos = await _planoService.GetAll();
            return Ok(planos);
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var plano = await _planoService.GetById(id);
            if (plano == null) return NotFound("Plano não encontrado.");
            return Ok(plano);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePlanoRequest req)
        {
            try
            {
                var plano = await _planoService.Create(req);
                return Ok(plano);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePlanoRequest req)
        {
            try
            {
                var plano = await _planoService.Update(id, req);
                return Ok(plano);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}