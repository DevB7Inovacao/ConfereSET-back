using ControlApi;
﻿using Core.DTO;
using Core.Models;
using Infrastructure.Authenticate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;
using System;

namespace ControlApi.Controllers
{
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
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("getPaged")]
        public async Task<IActionResult> GetPaged([FromQuery] FiltersTiposOcorrenciaDTO filtersDTO)
        {
            // Multi-tenant: força o EmpresaId do JWT, ignorando query string.
            filtersDTO.EmpresaId = User.GetEmpresaId();
            var result = await _service.GetPaged(filtersDTO);
            if (result != null)
                return Ok(result);

            return BadRequest();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateTipoOcorrenciaRequest req)
        {
            if (id <= 0) return BadRequest("id inválido.");
            if (req == null) return BadRequest("Payload inválido.");

            var existing = await _service.GetById(id);
            if (existing == null) return NotFound("Tipo de ocorrência não encontrado.");
            if (existing.EmpresaId != User.GetEmpresaId()) return NotFound("Tipo de ocorrência não encontrado.");

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

        [HttpDelete]
        [Route("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var __e = await _service.GetById(id);
                if (__e == null || __e.EmpresaId != User.GetEmpresaId()) return NotFound("Tipo de ocorrência não encontrado.");
                var ok = await _service.Delete(id);
                if (ok) return Ok("Tipo de ocorrência excluído com sucesso.");
                return BadRequest("Falha ao excluir tipo de ocorrência.");
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
                var __e = await _service.GetById(id);
                if (__e == null || __e.EmpresaId != User.GetEmpresaId()) return NotFound("Tipo de ocorrência não encontrado.");
                var ok = await _service.ToggleStatus(id);
                if (ok) return Ok("Status do tipo de ocorrência alterado com sucesso.");
                return BadRequest("Falha ao alterar o status do tipo de ocorrência.");
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

            var item = await _service.GetById(id);
            if (item == null) return NotFound("Tipo de ocorrência não encontrado.");
            if (item.EmpresaId != User.GetEmpresaId()) return NotFound("Tipo de ocorrência não encontrado.");

            var dto = new TiposOcorrenciaDTO
            {
                Id = item.Id,
                Nome = item.Nome,
                Descricao = item.Descricao,
                Gravidade = item.Gravidade,
                Requisitos = item.Requisitos,
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
                var result = await _service.GetSimple();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}