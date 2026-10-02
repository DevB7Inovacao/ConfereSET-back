using ControlApi;
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
        private const string MsgSomenteLeitura = "Usuários somente leitura não podem alterar despesas.";
        private const string MsgNaoVinculado = "Você não está vinculado a esta obra.";

        private readonly IJWTManager _jWTManager;
        IDespesasService _despesasService;
        private readonly IObrasService _obrasService;

        public DespesasController(IJWTManager jWTManager, IDespesasService despesasService, IObrasService obrasService)
        {
            this._jWTManager = jWTManager;
            this._despesasService = despesasService;
            _obrasService = obrasService;
        }

        // Operadores lançam despesas; apenas somente leitura é bloqueado.
        private IActionResult? ChecarPermissaoEscrita()
        {
            return User.IsReadOnly() ? StatusCode(StatusCodes.Status403Forbidden, MsgSomenteLeitura) : null;
        }

        private async Task<bool> ObraPertenceAEmpresa(int obraId)
        {
            var obra = await _obrasService.GetObraById(obraId);
            return obra != null && obra.EmpresaId == User.GetEmpresaId();
        }

        /// <summary>
        /// Operador (type 2): ids das obras às quais está vinculado. Para os demais perfis retorna
        /// <c>null</c> (sem restrição além da empresa).
        /// </summary>
        private async Task<List<int>?> ObrasVinculadasDoOperador()
        {
            if (!User.IsOperador()) return null;
            return await _obrasService.GetObraIdsByOperadorId(User.GetUserId());
        }

        /// <summary>Despesa visível ao chamador: mesma empresa e, para operador, obra vinculada.</summary>
        private async Task<bool> DespesaVisivel(Despesas? despesa)
        {
            if (despesa == null || despesa.EmpresaId != User.GetEmpresaId()) return false;
            if (!User.IsOperador()) return true;
            return await _obrasService.IsOperadorVinculado(despesa.ObraId, User.GetUserId());
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateDespesa([FromBody] CreateDespesaRequest req)
        {
            try
            {
                var semPermissao = ChecarPermissaoEscrita();
                if (semPermissao != null) return semPermissao;

                if (req == null) return BadRequest("Payload inválido.");
                if (req.ObraId > 0 && !await ObraPertenceAEmpresa(req.ObraId)) return NotFound("Obra não encontrada.");

                // Operador só lança despesa em obra à qual está vinculado.
                if (User.IsOperador())
                {
                    if (req.ObraId <= 0) return BadRequest("Obra é obrigatória.");
                    if (!await _obrasService.IsOperadorVinculado(req.ObraId, User.GetUserId()))
                        return StatusCode(StatusCodes.Status403Forbidden, MsgNaoVinculado);
                }

                var despesa = new Despesas
                {
                    Name = req.Name,
                    Amount = req.Amount,
                    Date = req.Date,
                    Category = req.Category,
                    Description = req.Description,
                    ObraId = req.ObraId,
                    EmpresaId = User.GetEmpresaId(),
                    Status = 1
                };

                var result = await _despesasService.CreateDespesa(despesa);

                // [v2] Devolve o id para o relatório vincular a despesa recém-lançada.
                if (result.Id > 0)
                    return Ok(new { id = result.Id, message = "Despesa cadastrada com sucesso." });
                else
                    return BadRequest("Erro ao cadastrar despesa.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("getDespesasPaged")]
        public async Task<IActionResult> GetDespesasPaged([FromQuery] FiltersDespesasDTO filtersDTO)
        {
            // Multi-tenant: força o EmpresaId do JWT, ignorando query string.
            filtersDTO.EmpresaId = User.GetEmpresaId();
            // Operador: apenas despesas das obras vinculadas (valor da query é sempre sobrescrito).
            filtersDTO.ObraIds = await ObrasVinculadasDoOperador();
            var result = await _despesasService.GetDespesasPaged(filtersDTO);
            if (result != null)
                return Ok(result);
            else
                return BadRequest();
        }

        [HttpPut("{despesaId}")]
        public async Task<IActionResult> UpdateDespesa(int despesaId, [FromBody] UpdateDespesaRequest req)
        {
            var semPermissao = ChecarPermissaoEscrita();
            if (semPermissao != null) return semPermissao;

            if (despesaId <= 0) return BadRequest("despesaId inválido.");
            if (req == null) return BadRequest("Payload inválido.");

            var existing = await _despesasService.GetDespesaById(despesaId);
            if (existing == null) return NotFound("Despesa não encontrada.");
            if (existing.EmpresaId != User.GetEmpresaId()) return NotFound("Despesa não encontrado.");
            if (!await DespesaVisivel(existing)) return NotFound("Despesa não encontrada.");

            if (req.ObraId.HasValue && req.ObraId.Value > 0 && !await ObraPertenceAEmpresa(req.ObraId.Value))
                return NotFound("Obra não encontrada.");

            // Operador não pode mover a despesa para uma obra à qual não está vinculado.
            if (User.IsOperador() && req.ObraId.HasValue && req.ObraId.Value > 0
                && !await _obrasService.IsOperadorVinculado(req.ObraId.Value, User.GetUserId()))
                return StatusCode(StatusCodes.Status403Forbidden, MsgNaoVinculado);

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

        [HttpDelete]
        [Route("delete/{id}")]
        public async Task<IActionResult> DeleteDespesa(int id)
        {
            try
            {
                var semPermissao = ChecarPermissaoEscrita();
                if (semPermissao != null) return semPermissao;

                var __scope = await _despesasService.GetDespesaById(id);
                if (!await DespesaVisivel(__scope)) return NotFound("Despesa não encontrado.");
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

        [HttpPost]
        [Route("toggle-status/{id}")]
        public async Task<IActionResult> ToggleDespesaStatus(int id)
        {
            try
            {
                var semPermissao = ChecarPermissaoEscrita();
                if (semPermissao != null) return semPermissao;

                var __scope = await _despesasService.GetDespesaById(id);
                if (!await DespesaVisivel(__scope)) return NotFound("Despesa não encontrado.");
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

        [HttpGet("getById/{despesaId}")]
        public async Task<IActionResult> GetById(int despesaId)
        {
            if (despesaId <= 0) return BadRequest("despesaId inválido.");

            var despesa = await _despesasService.GetDespesaById(despesaId);
            if (despesa == null) return NotFound("Despesa não encontrada.");
            if (despesa.EmpresaId != User.GetEmpresaId()) return NotFound("Despesa não encontrado.");
            if (!await DespesaVisivel(despesa)) return NotFound("Despesa não encontrada.");

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

        [HttpGet]
        [Route("simple")]
        public async Task<IActionResult> GetSimple([FromQuery] int? obraId)
        {
            try
            {
                var result = await _despesasService.GetDespesasSimple(obraId, User.GetEmpresaId());
                var vinculadas = await ObrasVinculadasDoOperador();
                if (vinculadas != null)
                    result = result.Where(d => vinculadas.Contains(d.ObraId)).ToList();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("relatorios/resumo")]
        public async Task<IActionResult> GetRelatorioResumo([FromQuery] FiltrosRelatorioDTO filtros)
        {
            try
            {
                // Multi-tenant: força o EmpresaId do JWT, ignorando query string.
                filtros.EmpresaId = User.GetEmpresaId();
                // Operador: apenas obras vinculadas.
                filtros.ObraIds = await ObrasVinculadasDoOperador();
                var relatorio = await _despesasService.GetRelatorioResumo(filtros);
                return Ok(relatorio);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("relatorios/detalhado")]
        public async Task<IActionResult> GetRelatorioDetalhado([FromQuery] FiltrosRelatorioDTO filtros)
        {
            try
            {
                // Multi-tenant: força o EmpresaId do JWT, ignorando query string.
                filtros.EmpresaId = User.GetEmpresaId();
                // Operador: apenas obras vinculadas.
                filtros.ObraIds = await ObrasVinculadasDoOperador();
                var relatorio = await _despesasService.GetRelatorioDetalhado(filtros);
                return Ok(relatorio);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}