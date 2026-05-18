using Core.DTO;
using Core.Models;
using Infrastructure.Authenticate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace ControlApi.Controllers
{
    /// <summary>
    /// Grupos de Obras.
    /// <para>
    /// Regras de autorização:
    /// <list type="bullet">
    /// <item>Todos os endpoints exigem JWT válido (<c>[Authorize]</c>).</item>
    /// <item>Operações de escrita exigem admin/gerente.</item>
    /// <item>O modelo <see cref="GrupoDeObras"/> não possui <c>EmpresaId</c> no schema atual (catálogo global) —
    /// portanto, não há validação de escopo por empresa pós-fetch; apenas role-gate.</item>
    /// </list>
    /// </para>
    /// </summary>
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

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> Create([FromBody] CreateGrupoDeObrasRequest req)
        {
            try
            {
                if (!User.IsAdminOrGerente())
                    return StatusCode(StatusCodes.Status403Forbidden, "Apenas admin/gerente podem realizar esta ação.");

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
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("getPaged")]
        public async Task<IActionResult> GetPaged([FromQuery] FiltersGrupoDeObrasDTO filtersDTO)
        {
            try
            {
                _ = User.GetEmpresaId();

                var result = await _service.GetGrupoPaged(filtersDTO);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("getById/{groupId}")]
        public async Task<IActionResult> GetById(int groupId)
        {
            try
            {
                _ = User.GetEmpresaId();

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
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{groupId}")]
        public async Task<IActionResult> Update(int groupId, [FromBody] UpdateGrupoDeObrasRequest req)
        {
            try
            {
                if (!User.IsAdminOrGerente())
                    return StatusCode(StatusCodes.Status403Forbidden, "Apenas admin/gerente podem realizar esta ação.");

                if (groupId <= 0) return BadRequest("groupId inválido.");
                if (req == null) return BadRequest("Payload inválido.");

                var result = await _service.UpdateGrupo(groupId, req);
                if (result) return Ok(true);

                return BadRequest("Falha ao atualizar grupo.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpDelete]
        [Route("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (!User.IsAdminOrGerente())
                    return StatusCode(StatusCodes.Status403Forbidden, "Apenas admin/gerente podem realizar esta ação.");

                var result = await _service.DeleteGrupo(id);
                if (result)
                    return Ok("Grupo excluído com sucesso.");
                else
                    return BadRequest("Falha ao excluir grupo.");
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("toggle-status/{id}")]
        publ