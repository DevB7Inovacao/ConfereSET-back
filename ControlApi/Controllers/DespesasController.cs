using Core.DTO;
using Core.Models;
using Infrastructure.Authenticate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;
using System;
using System.Threading.Tasks;

namespace ControlApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DespesasController : ControllerBase
    {
        private readonly IJWTManager _jWTManager;
        IDespesasService _despesasService;

        public DespesasController(IJWTManager jWTManager, IDespesasService despesasService)
        {
            this._jWTManager = jWTManager;
            this._despesasService = despesasService;
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateDespesa([FromBody] CreateDespesaRequest req)
        {
            try
            {
                var despesa = new Despesas
                {
                    Name = req.Name,
                    Amount = req.Amount,
                    Date = req.Date,
                    Category = req.Category,
                    Description = req.Description,
                    ObraId = req.ObraId,
                    Status = 1
                };

                var result = await _despesasService.CreateDespesa(despesa);

                if (result.Id > 0)
                    return Ok("Despesa cadastrada com sucesso.");
                else
                    return BadRequest("Erro ao cadastrar despesa.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("getDespesasPaged")]
        public async Task<IActionResult> GetDespesasPaged([FromQuery] FiltersDespesasDTO filtersDTO)
        {
            var result = await _despesasService.GetDespesasPaged(filtersDTO);
            if (result != null)
                return Ok(result);
            else
                return BadRequest();
        }

        [AllowAnonymous]
        [HttpPut("{despesaId}")]
        public async Task<IActionResult> UpdateDespesa(int despesaId, [FromBody] UpdateDespesaRequest req)
        {
            if (despesaId <= 0) return BadRequest("despesaId inválido.");
            if (req == null) return BadRequest("Payload inválido.");

            var existing = await _despesasService.GetDespesaById(despesaId);
            if (existing == null) return NotFound("Despesa não encontrada.");

            if (req.Name != null) existing.Name = string.IsNullOrWhiteSpace(req.Name) ? existing.Name : req.Name;
            if (req.Amount.HasValue) existing.Amount = req.Amount.Value;
            if (req.Date.HasValue) existing.Date = req.Date.Value;

            if (req.Category != null) existing.Category = string.IsNullOrWhiteSpace(req.Category) ? null : req.Category;
            if (req.Description != null) existing.Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description;

            if (req.ObraId.HasValue && req.ObraId.Value > 0) existing.ObraId = req.ObraId.Value;
            if (req.Status.HasValue) existing.Status = req.Status.Value;

            var result = await _despesasService.UpdateDespesa(existing, despesaId);
            if (result) return Ok(true);

            return BadRequest("Falha ao atualizar despesa.");
        }

        [AllowAnonymous]
        [HttpDelete]
        [Route("delete/{id}")]
        public async Task<IActionResult> DeleteDespesa(int id)
        {
            try
            {
                bool result = await _despesasService.DeleteDespesa(id);
                if (result)
                    return Ok("Despesa excluída com sucesso.");
                else
                    return BadRequest("Falha ao excluir despesa.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("toggle-status/{id}")]
        public async Task<IActionResult> ToggleDespesaStatus(int id)
        {
            try
            {
                bool result = await _despesasService.ToggleDespesaStatus(id);
                if (result)
                    return Ok("Status da despesa alterado com sucesso.");
                else
                    return BadRequest("Falha ao alterar o status da despesa.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet("getById/{despesaId}")]
        public async Task<IActionResult> GetById(int despesaId)
        {
            if (despesaId <= 0) return BadRequest("despesaId inválido.");

            var despesa = await _despesasService.GetDespesaById(despesaId);
            if (despesa == null) return NotFound("Despesa não encontrada.");

            var dto = new DespesaDTO
            {
                Id = despesa.Id,
                Name = despesa.Name,
                Amount = despesa.Amount,
                Date = despesa.Date,
                Category = despesa.Category,
                Description = despesa.Description,
                ObraId = despesa.ObraId,
                Status = despesa.Status
            };

            return Ok(dto);
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("simple")]
        public async Task<IActionResult> GetSimple([FromQuery] int? obraId)
        {
            try
            {
                var result = await _despesasService.GetDespesasSimple(obraId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}