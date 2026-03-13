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
    public class RelatorioController : ControllerBase
    {
        private readonly IRelatorioService _service;

        public RelatorioController(IRelatorioService service)
        {
            _service = service;
        }

        [AllowAnonymous]
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateRelatorioRequest req)
        {
            try
            {
                if (req == null) return BadRequest("Payload inválido.");
                var result = await _service.Create(req);
                return Ok(result.Id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet("getPaged")]
        public async Task<IActionResult> GetPaged([FromQuery] FiltersRelatorioDTO filters)
        {
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

        [AllowAnonymous]
        [HttpGet("getById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                if (id <= 0) return BadRequest("id inválido.");
                var result = await _service.GetById(id);
                if (result == null) return NotFound("Relatório não encontrado.");
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateRelatorioStatusRequest req)
        {
            try
            {
                if (id <= 0) return BadRequest("id inválido.");
                if (req == null) return BadRequest("Payload inválido.");
                var ok = await _service.UpdateStatus(id, req);
                return ok ? Ok(true) : BadRequest("Falha ao atualizar status.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id <= 0) return BadRequest("id inválido.");
                var ok = await _service.Delete(id);
                return ok ? Ok("Relatório excluído com sucesso.") : BadRequest("Falha ao excluir.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPut("item/{itemId}")]
        public async Task<IActionResult> UpdateItem(int itemId, [FromBody] UpdateRelatorioSecaoItemRequest req)
        {
            try
            {
                if (itemId <= 0) return BadRequest("itemId inválido.");
                if (req == null) return BadRequest("Payload inválido.");
                var ok = await _service.UpdateItem(itemId, req);
                return ok ? Ok(true) : BadRequest("Falha ao atualizar item.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPost("item/{itemId}/foto")]
        public async Task<IActionResult> AddFoto(int itemId, [FromBody] AddFotoToItemRequest req)
        {
            try
            {
                if (itemId <= 0) return BadRequest("itemId inválido.");
                if (req == null) return BadRequest("Payload inválido.");
                var ok = await _service.AddFotoToItem(itemId, req);
                return ok ? Ok("Foto adicionada com sucesso.") : BadRequest("Falha ao adicionar foto.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpDelete("foto/{fotoId}")]
        public async Task<IActionResult> DeleteFoto(int fotoId)
        {
            try
            {
                if (fotoId <= 0) return BadRequest("fotoId inválido.");
                var ok = await _service.DeleteFoto(fotoId);
                return ok ? Ok("Foto excluída com sucesso.") : BadRequest("Falha ao excluir foto.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPost("secao/{secaoId}/comentario")]
        public async Task<IActionResult> AddComentario(int secaoId, [FromBody] AddComentarioRequest req)
        {
            try
            {
                if (secaoId <= 0) return BadRequest("secaoId inválido.");
                if (req == null) return BadRequest("Payload inválido.");
                var result = await _service.AddComentario(secaoId, req);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPut("comentario/{comentarioId}")]
        public async Task<IActionResult> UpdateComentario(int comentarioId, [FromBody] UpdateComentarioRequest req)
        {
            try
            {
                if (comentarioId <= 0) return BadRequest("comentarioId inválido.");
                if (req == null) return BadRequest("Payload inválido.");
                var ok = await _service.UpdateComentario(comentarioId, req);
                return ok ? Ok(true) : BadRequest("Falha ao atualizar comentário.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpDelete("comentario/{comentarioId}")]
        public async Task<IActionResult> DeleteComentario(int comentarioId)
        {
            try
            {
                if (comentarioId <= 0) return BadRequest("comentarioId inválido.");
                var ok = await _service.DeleteComentario(comentarioId);
                return ok ? Ok("Comentário excluído com sucesso.") : BadRequest("Falha ao excluir comentário.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}