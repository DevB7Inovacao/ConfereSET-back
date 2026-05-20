using ControlApi;
﻿using Core.DTO;
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
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("getPaged")]
        public async Task<IActionResult> GetPaged([FromQuery] FiltersEquipamentosDTO filtersDTO)
        {
            // Multi-tenant: força o EmpresaId do JWT, ignorando query string.
            filtersDTO.EmpresaId = User.GetEmpresaId();
            var result = await _equipamentosService.GetEquipamentosPaged(filtersDTO);
            if (result != null)
                return Ok(result);

            return BadRequest();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateEquipamentoRequest req)
        {
            if (id <= 0) return BadRequest("id inválido.");
            if (req == null) return BadRequest("Payload inválido.");

            var existing = await _equipamentosService.GetEquipamentoById(id);
            if (existing == null) return NotFound("Equipamento não encontrado.");
            if (existing.EmpresaId != User.GetEmpresaId()) return NotFound("Equipamento não encontrado.");

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

        [HttpDelete]
        [Route("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var __e = await _equipamentosService.GetEquipamentoById(id);
                if (__e == null || __e.EmpresaId != User.GetEmpresaId()) return NotFound("Equipamento não encontrado.");
                var ok = await _equipamentosService.DeleteEquipamento(id);
                if (ok) return Ok("Equipamento excluído com sucesso.");
                return BadRequest("Falha ao excluir equipamento.");
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
                var __e = await _equipamentosService.GetEquipamentoById(id);
                if (__e == null || __e.EmpresaId != User.GetEmpresaId()) return NotFound("Equipamento não encontrado.");
                var ok = await _equipamentosService.ToggleEquipamentoStatus(id);
                if (ok) return Ok("Status do equipamento alterado com sucesso.");
                return BadRequest("Falha ao alterar o status do equipamento.");
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

            var item = await _equipamentosService.GetEquipamentoById(id);
            if (item == null) return NotFound("Equipamento não encontrado.");
            if (item.EmpresaId != User.GetEmpresaId()) return NotFound("Equipamento não encontrado.");

            var dto = new EquipamentosDTO
            {
                Id = item.Id,
                Nome = item.Nome,
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
                var result = await _equipamentosService.GetEquipamentosSimple();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}