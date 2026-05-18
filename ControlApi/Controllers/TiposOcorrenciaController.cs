using Core.DTO;
using Core.Models;
using Infrastructure.Authenticate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;
using System;

namespace ControlApi.Controllers
{
    /// <summary>
    /// Catálogo de Tipos de Ocorrência.
    /// <para>
    /// Regras de autorização:
    /// <list type="bullet">
    /// <item>Todos os endpoints exigem JWT válido (<c>[Authorize]</c>).</item>
    /// <item>Operações de escrita exigem admin/gerente.</item>
    /// <item>O modelo <see cref="TiposOcorrencia"/> não possui <c>EmpresaId</c> no schema atual (catálogo global) —
    /// portanto, não há validação de escopo por empresa pós-fetch; apenas role-gate.</item>
    /// </list>
    /// </para>
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TiposOcorrenciaController : ControllerBase
    {
        private readonly IJWTManager _jWTManager;
        private readonly ITiposOcorrenciaService _service;

        public TiposOcorrenciaController(IJWTManager jWTManager, ITiposOcorrenciaService service)
        {
            _jWTManager = jWTManager;
            _service = service;
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> Create([FromBody] CreateTipoOcorrenciaRequest req)
        {
            try
            {
                if (!User.IsAdminOrGerente())
                    return StatusCode(StatusCodes.Status403Forbidden, "Apenas admin/gerente podem realizar esta ação.");

                if (req == null) return BadRequest("Payload inválido.");
                if (string.IsNullOrWhiteSpace(req.Nome)) return BadRequest("Nome é obrigatório.");
                if (req.Gravidade < 0 || req.Gravidade > 3) return BadRequest("Gravidade inválida.");

                var item = new TiposOcorrencia
                {
                    Nome = req.Nome.Trim(),
                    Descricao = string.IsNullOrWhiteSpace(req.Descricao) ? null : req.Descricao.Trim(),
                    Gravidade = req.Gravidade,
                    Requisitos = req.Requisitos,
                    Status = 1
                };

                var result = await _service.Create(item);

                if (result.Id > 0)
                    return Ok("Tipo de ocorrência cadastrado com sucesso.");
                else
                    return BadRequest("Erro ao cadastrar tipo de ocorrência.");
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
        public async Task<IActionResult> GetPaged([FromQuery] FiltersTiposOcorrenciaDTO filtersDTO)
        {
            try
            {
                _ = User.GetEmpresaId();

                var result = await _service.GetPaged(filtersDTO);
                if (result != null)
                    return Ok(result);

                return BadRequest();
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

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTipoOcorrenciaRequest req)
        {
            try
            {
                if (!User.IsAdminOrGerente())
                    return StatusCode(StatusCodes.Status403Forbidden, "Apenas admin/gerente podem realizar esta ação.");

                if (id <= 0) return BadRequest("id inválido.");
                if (req == null) return BadRequest("Payload inválido.");

                var existing = await _service.GetById(id);
                if (existing == null) return NotFound("Tipo de ocorrência não encontrado.");

                if (req.Nome != null && !string.IsNullOrWhiteSpace(req.Nome))
                    existing.Nome = req.Nome.Trim();

                if (req.Descricao != null)
                    existing.Descricao = string.IsNullOrWhiteSpace(req.Descricao) ? null : req.Descricao.Trim();

                if (req.Gravidade.HasValue)
                {
                    if (req.Gravidade.Value < 0 || req.Gravidade.Value > 3) return BadRequest("Gravidade inválida.");
                    existing.Gravidade = req.Gravidade.Value;
                }

                if (req.Requisitos.HasValue)
                    existing.Requisitos = req.Requisitos.Value;

                if (req.Status.HasValue)
                    existing.Status = req.Status.Value;

                var ok = await _service.Update(existing, id);
                if (ok) return Ok(true);

                return BadRequest("Falha ao atualizar tipo de ocorrência.");
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

        [HttpDelete]
        [Route("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (!User.IsAdminOrGerente())
                    return StatusCode(StatusCodes.Status403Forbidden, "Apenas admin/gerente podem realizar esta ação.");

                var ok = await _service.Delete(id);
                if (ok) return Ok(