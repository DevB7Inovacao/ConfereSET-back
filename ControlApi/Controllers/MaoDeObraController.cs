using Core.DTO;
using Core.Models;
using Infrastructure.Authenticate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlApi.Controllers
{
    /// <summary>
    /// Catálogo de Mão de Obra.
    /// <para>
    /// Regras de autorização (validadas neste controller, não em filtros globais):
    /// <list type="bullet">
    /// <item>Todos os endpoints exigem JWT válido (<c>[Authorize]</c>).</item>
    /// <item>Operações de escrita (<c>Create</c>, <c>Update</c>, <c>Delete</c>, <c>ToggleStatus</c>) exigem admin/gerente.</item>
    /// <item>O modelo <see cref="MaoDeObra"/> não possui <c>EmpresaId</c> no schema atual (catálogo global) —
    /// por isso não há validação de escopo por empresa pós-fetch; apenas role-gate.</item>
    /// </list>
    /// </para>
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MaoDeObraController : ControllerBase
    {
        private readonly IJWTManager _jWTManager;
        private readonly IMaoDeObraService _maoDeObraService;

        public MaoDeObraController(IJWTManager jWTManager, IMaoDeObraService maoDeObraService)
        {
            _jWTManager = jWTManager;
            _maoDeObraService = maoDeObraService;
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> Create([FromBody] CreateMaoDeObraRequest req)
        {
            try
            {
                if (!User.IsAdminOrGerente())
                    return StatusCode(StatusCodes.Status403Forbidden, "Apenas admin/gerente podem realizar esta ação.");

                if (req == null) return BadRequest("Payload inválido.");
                if (string.IsNullOrWhiteSpace(req.Funcao)) return BadRequest("Função é obrigatória.");

                var item = new MaoDeObra
                {
                    Funcao = req.Funcao.Trim(),
                    Descricao = string.IsNullOrWhiteSpace(req.Descricao) ? null : req.Descricao.Trim(),
                    Status = 1
                };

                var result = await _maoDeObraService.CreateMaoDeObra(item);

                if (result.Id > 0)
                    return Ok("Mão de obra cadastrada com sucesso.");
                else
                    return BadRequest("Erro ao cadastrar mão de obra.");
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
        public async Task<IActionResult> GetPaged([FromQuery] FiltersMaoDeObraDTO filtersDTO)
        {
            try
            {
                // Garante que o JWT é válido (lança 403 se claim faltar).
                _ = User.GetEmpresaId();

                var result = await _maoDeObraService.GetMaoDeObraPaged(filtersDTO);
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
        public async Task<IActionResult> Update(int id, [FromBody] UpdateMaoDeObraRequest req)
        {
            try
            {
                if (!User.IsAdminOrGerente())
                    return StatusCode(StatusCodes.Status403Forbidden, "Apenas admin/gerente podem realizar esta ação.");

                if (id <= 0) return BadRequest("id inválido.");
                if (req == null) return BadRequest("Payload inválido.");

                var existing = await _maoDeObraService.GetMaoDeObraById(id);
                if (existing == null) return NotFound("Mão de obra não encontrada.");

                if (req.Funcao != null && !string.IsNullOrWhiteSpace(req.Funcao))
                    existing.Funcao = req.Funcao.Trim();

                if (req.Descricao != null)
                    existing.Descricao = string.IsNullOrWhiteSpace(req.Descricao) ? null : req.Descricao.Trim();

                if (req.Status.HasValue)
                    existing.Status = req.Status.Value;

                var ok = await _maoDeObraService.UpdateMaoDeObra(existing, id);
                if (ok) return Ok(true);

                return BadRequest("Falha ao atualizar mão de obra.");
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