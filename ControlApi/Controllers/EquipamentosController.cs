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
    /// Catálogo de Equipamentos.
    /// <para>
    /// Regras de autorização:
    /// <list type="bullet">
    /// <item>Todos os endpoints exigem JWT válido (<c>[Authorize]</c>).</item>
    /// <item>Operações de escrita exigem admin/gerente.</item>
    /// <item>O modelo <see cref="Equipamentos"/> não possui <c>EmpresaId</c> no schema atual (catálogo global) —
    /// portanto, não há validação de escopo por empresa pós-fetch; apenas role-gate.</item>
    /// </list>
    /// </para>
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EquipamentosController : ControllerBase
    {
        private readonly IJWTManager _jWTManager;
        private readonly IEquipamentosService _equipamentosService;

        public EquipamentosController(IJWTManager jWTManager, IEquipamentosService equipamentosService)
        {
            _jWTManager = jWTManager;
            _equipamentosService = equipamentosService;
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> Create([FromBody] CreateEquipamentoRequest req)
        {
            try
            {
                if (!User.IsAdminOrGerente())
                    return StatusCode(StatusCodes.Status403Forbidden, "Apenas admin/gerente podem realizar esta ação.");

                if (req == null) return BadRequest("Payload inválido.");
                if (string.IsNullOrWhiteSpace(req.Nome)) return BadRequest("Nome é obrigatório.");

                var item = new Equipamentos
                {
                    Nome = req.Nome.Trim(),
                    Descricao = string.IsNullOrWhiteSpace(req.Descricao) ? null : req.Descricao.Trim(),
                    Status = 1
                };

                var result = await _equipamentosService.CreateEquipamento(item);

                if (result.Id > 0)
                    return Ok("Equipamento cadastrado com sucesso.");
                else
                    return BadRequest("Erro ao cadastrar equipamento.");
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
        public async Task<IActionResult> GetPaged([FromQuery] FiltersEquipamentosDTO filtersDTO)
        {
            try
            {
                _ = User.GetEmpresaId();

                var result = await _equipamentosService.GetEquipamentosPaged(filtersDTO);
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
        public async Task<IActionResult> Update(int id, [FromBody] UpdateEquipamentoRequest req)
        {
            try
            {
                if (!User.IsAdminOrGerente())
                    return StatusCode(StatusCodes.Status403Forbidden, "Apenas admin/gerente podem realizar esta ação.");

                if (id <= 0) return BadRequest("id inválido.");
                if (req == null) return BadRequest("Payload inválido.");

                var existing = await _equipamentosService.GetEquipamentoById(id);
                if (existing == null) return NotFound("Equipamento não encontrado.");

                if (req.Nome != null && !string.IsNullOrWhiteSpace(req.Nome))
                    existing.Nome = req.Nome.Trim();

                if (req.Descricao != null)
                    existing.Descricao = string.IsNullOrWhiteSpace(req.Descricao) ? null : req.Descricao.Trim();

                if (req.Status.HasValue)
                    existing.Status = req.Status.Value;

                var ok = await _equipamentosService.UpdateEquipamento(existing, id);
                if (ok) return Ok(true);

                return BadRequest("Falha ao atualizar equipamento.");
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

                var ok = await _equipamentosService.DeleteEquipamento(id);
          