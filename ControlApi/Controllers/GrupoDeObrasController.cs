using Core.DTO;
using Core.Models;
using Infrastructure.Authenticate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace ControlApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class GrupoDeObrasController : ControllerBase
    {
        private readonly IJWTManager _jWTManager;
        private readonly IGrupoDeObrasService _service;

        public GrupoDeObrasController(IJWTManager jWTManager, IGrupoDeObrasService service)
        {
            _jWTManager = jWTManager;
            _service = service;
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> Create([FromBody] CreateGrupoDeObrasRequest req)
        {
            try
            {
                var grupo = new GrupoDeObras
                {
                    Name = req.Name,
                    Status = 1
                };

                var result = await _service.CreateGrupo(grupo);

                if (result.Id > 0)
                    return Ok("Grupo cadastrado com sucesso.");
                else
                    return BadRequest("Erro ao cadastrar grupo.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("getPaged")]
        public async Task<IActionResult> GetPaged([FromQuery] FiltersGrupoDeObrasDTO filtersDTO)
        {
            try
            {
                var result = await _service.GetGrupoPaged(filtersDTO);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet("getById/{groupId}")]
        public async Task<IActionResult> GetById(int groupId)
        {
            if (groupId <= 0) return BadRequest("groupId inválido.");

            var grupo = await _service.GetGrupoById(groupId);
            if (grupo == null) return NotFound("Grupo não encontrado.");

            var dto = new GrupoDeObrasDTO
            {
                Id = grupo.Id,
                Name = grupo.Name,
                Status = grupo.Status,
                ObrasIds = grupo.Obras?.Select(x => x.ObraId).ToList() ?? new()
            };

            return Ok(dto);
        }

        [AllowAnonymous]
        [HttpPut("{groupId}")]
        public async Task<IActionResult> Update(int groupId, [FromBody] UpdateGrupoDeObrasRequest req)
        {
            try
            {
                if (groupId <= 0) return BadRequest("groupId inválido.");
                if (req == null) return BadRequest("Payload inválido.");

                var result = await _service.UpdateGrupo(groupId, req);
                if (result) return Ok(true);

                return BadRequest("Falha ao atualizar grupo.");
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [AllowAnonymous]
        [HttpDelete]
        [Route("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _service.DeleteGrupo(id);
                if (result)
                    return Ok("Grupo excluído com sucesso.");
                else
                    return BadRequest("Falha ao excluir grupo.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("toggle-status/{id}")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var result = await _service.ToggleGrupoStatus(id);
                if (result)
                    return Ok("Status do grupo alterado com sucesso.");
                else
                    return BadRequest("Falha ao alterar o status do grupo.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPost("{groupId}/obras/{obraId}")]
        public async Task<IActionResult> AddObraToGrupo(int groupId, int obraId)
        {
            try
            {
                var result = await _service.AddObraToGrupo(groupId, obraId);
                if (result) return Ok(true);
                return BadRequest("Falha ao adicionar obra ao grupo.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpDelete("{groupId}/obras/{obraId}")]
        public async Task<IActionResult> RemoveObraFromGrupo(int groupId, int obraId)
        {
            try
            {
                var result = await _service.RemoveObraFromGrupo(groupId, obraId);
                if (result) return Ok(true);
                return BadRequest("Falha ao remover obra do grupo.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet("{groupId}/obras")]
        public async Task<IActionResult> GetObrasIdsByGrupo(int groupId)
        {
            try
            {
                var result = await _service.GetObrasIdsByGrupo(groupId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}