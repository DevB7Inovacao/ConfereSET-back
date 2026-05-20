using ControlApi;
﻿using Core.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace ControlApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ModeloTextoController : ControllerBase
    {
        private readonly IModeloTextoService _service;
    private readonly IRelatorioService _relatorioService;

        public ModeloTextoController(IModeloTextoService service,IRelatorioService relatorioService)
        {
            _service = service;
      _relatorioService = relatorioService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateModeloTextoRequest req)
        {
            try
            {
                var created = await _service.Create(req);
                return Ok(created.Id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("getPaged")]
        public async Task<IActionResult> GetPaged([FromQuery] FiltersModeloTextoDTO filters)
        {
            // Multi-tenant: força o EmpresaId do JWT, ignorando query string.
            filters.EmpresaId = User.GetEmpresaId();
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

        [HttpGet("getById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var model = await _service.GetById(id);
                if (model == null) return NotFound("Modelo não encontrado.");
            if (model.EmpresaId != User.GetEmpresaId()) return NotFound("Modelo não encontrado.");
                return Ok(model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateModeloTextoRequest req)
        {
            try
            {
                var __scope = await _service.GetById(id);
                if (__scope == null || __scope.EmpresaId != User.GetEmpresaId()) return NotFound("Modelo não encontrado.");
                var ok = await _service.Update(id, req);
                return ok ? Ok(true) : BadRequest("Falha ao atualizar.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var __scope = await _service.GetById(id);
                if (__scope == null || __scope.EmpresaId != User.GetEmpresaId()) return NotFound("Modelo não encontrado.");
                var ok = await _service.Delete(id);
                return ok ? Ok(true) : BadRequest("Falha ao excluir.");
            }
            catch (Exception ex)
            {
                return BadRequest("Não foi possível excluir o modelo de texto.");
            }
        }

        [HttpPost("toggle-status/{id}")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var __scope = await _service.GetById(id);
                if (__scope == null || __scope.EmpresaId != User.GetEmpresaId()) return NotFound("Modelo não encontrado.");
                var ok = await _service.ToggleStatus(id);
                return ok ? Ok(true) : BadRequest("Falha ao alternar status.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}