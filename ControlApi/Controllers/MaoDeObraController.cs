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
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("getPaged")]
        public async Task<IActionResult> GetPaged([FromQuery] FiltersMaoDeObraDTO filtersDTO)
        {
            var result = await _maoDeObraService.GetMaoDeObraPaged(filtersDTO);
            if (result != null)
                return Ok(result);

            return BadRequest();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateMaoDeObraRequest req)
        {
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

        [HttpDelete]
        [Route("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var ok = await _maoDeObraService.DeleteMaoDeObra(id);
                if (ok) return Ok("Mão de obra excluída com sucesso.");
                return BadRequest("Falha ao excluir mão de obra.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("toggle-status/{id}")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var ok = await _maoDeObraService.ToggleMaoDeObraStatus(id);
                if (ok) return Ok("Status da mão de obra alterado com sucesso.");
                return BadRequest("Falha ao alterar o status da mão de obra.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("getById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0) return BadRequest("id inválido.");

            var item = await _maoDeObraService.GetMaoDeObraById(id);
            if (item == null) return NotFound("Mão de obra não encontrada.");

            var dto = new MaoDeObraDTO
            {
                Id = item.Id,
                Funcao = item.Funcao,
                Descricao = item.Descricao,
                Status = item.Status
            };

            return Ok(dto);
        }

        [HttpGet]
        [Route("simple")]
        public async Task<IActionResult> GetSimple()
        {
            try
            {
                var result = await _maoDeObraService.GetMaoDeObraSimple();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}