using ControlApi;
using Core.DTO;
using Core.Enums;
using Infrastructure.Authenticate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace ControlApi.Controllers
{
    /// <summary>
    /// Ocorrências de obra.
    /// <para>
    /// Regras por perfil (admin/gerente mantêm acesso total à empresa):
    /// <list type="bullet">
    /// <item><b>Somente leitura (3)</b>: lista/consulta toda a empresa; não cria, altera, muda status nem exclui.</item>
    /// <item><b>Operador (2)</b>: enxerga apenas ocorrências das obras às quais está vinculado (ObraOperador —
    /// mesma semântica do filtro <c>OperadorId</c> do repositório); cria só em obra vinculada; altera/muda status/
    /// exclui apenas as ocorrências que ele mesmo criou.</item>
    /// </list>
    /// </para>
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class OcorrenciaController : ControllerBase
    {
        private const string MsgSomenteLeitura = "Usuários somente leitura não podem alterar ocorrências.";
        private const string MsgNaoVinculado = "Você não está vinculado a esta obra.";
        private const string MsgNaoEncontrada = "Ocorrência não encontrada.";

        private readonly IOcorrenciaService _service;
        private readonly IObrasService _obrasService;

        public OcorrenciaController(IOcorrenciaService service, IObrasService obrasService)
        {
            _service = service;
            _obrasService = obrasService;
        }

        // Operadores registram ocorrências; apenas somente leitura é bloqueado.
        private IActionResult? ChecarPermissaoEscrita()
        {
            return User.IsReadOnly() ? StatusCode(StatusCodes.Status403Forbidden, MsgSomenteLeitura) : null;
        }

        private async Task<bool> OperadorVinculado(int obraId)
        {
            return await _obrasService.IsOperadorVinculado(obraId, User.GetUserId());
        }

        /// <summary>
        /// Para operador: a ocorrência precisa existir na empresa e ter sido criada por ele.
        /// Retorna 404 (não vaza existência) quando não atende. Admin/gerente: sem restrição extra.
        /// </summary>
        private async Task<IActionResult?> ChecarAutoriaOperador(int id)
        {
            if (!User.IsOperador()) return null;

            var ocorrencia = await _service.GetById(id, User.GetEmpresaId());
            if (ocorrencia == null || ocorrencia.CriadoPorUserId != User.GetUserId())
                return NotFound(MsgNaoEncontrada);

            return null;
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateOcorrenciaRequest req)
        {
            try
            {
                var semPermissao = ChecarPermissaoEscrita();
                if (semPermissao != null) return semPermissao;

                if (req == null) return BadRequest("Payload inválido.");
                if (string.IsNullOrWhiteSpace(req.Titulo)) return BadRequest("Título é obrigatório.");
                var empresaId = User.GetEmpresaId();

                if (User.IsOperador())
                {
                    var obra = await _obrasService.GetObraById(req.ObraId);
                    if (obra == null || obra.EmpresaId != empresaId) return NotFound("Obra não encontrada.");
                    if (!await OperadorVinculado(req.ObraId))
                        return StatusCode(StatusCodes.Status403Forbidden, MsgNaoVinculado);
                }

                // Autoria sempre do JWT — o CriadoPorUserId do body é ignorado.
                var result = await _service.Create(req, empresaId, User.GetUserId());
                return Ok(result.Id);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("getPaged")]
        public async Task<IActionResult> GetPaged([FromQuery] FiltersOcorrenciaDTO filters)
        {
            // Multi-tenant: força o EmpresaId do JWT, ignorando query string.
            filters.EmpresaId = User.GetEmpresaId();

            // Operador: só ocorrências das obras às quais está vinculado (ignora OperadorId da query).
            // Somente leitura e admin/gerente veem a empresa toda.
            if (User.IsOperador()) filters.OperadorId = User.GetUserId();

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
                if (id <= 0) return BadRequest("id inválido.");
                var empresaId = User.GetEmpresaId();
                var result = await _service.GetById(id, empresaId);
                if (result == null) return NotFound(MsgNaoEncontrada);

                // Mesmo escopo do getPaged: operador só abre ocorrência de obra vinculada.
                if (User.IsOperador() && !await OperadorVinculado(result.ObraId))
                    return NotFound(MsgNaoEncontrada);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("obra/{obraId}")]
        public async Task<IActionResult> GetByObraId(int obraId)
        {
            try
            {
                if (obraId <= 0) return BadRequest("obraId inválido.");
                var empresaId = User.GetEmpresaId();

                // Operador fora da obra recebe lista vazia (mesmo comportamento de obra de outra empresa).
                if (User.IsOperador() && !await OperadorVinculado(obraId))
                    return Ok(new List<OcorrenciaDTO>());

                var result = await _service.GetByObraId(obraId, empresaId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateOcorrenciaRequest req)
        {
            try
            {
                var semPermissao = ChecarPermissaoEscrita();
                if (semPermissao != null) return semPermissao;

                if (id <= 0) return BadRequest("id inválido.");
                if (req == null) return BadRequest("Payload inválido.");

                var negado = await ChecarAutoriaOperador(id);
                if (negado != null) return negado;

                var empresaId = User.GetEmpresaId();
                var ok = await _service.Update(id, req, empresaId);
                return ok ? Ok(true) : BadRequest("Falha ao atualizar ocorrência.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOcorrenciaStatusRequest req)
        {
            try
            {
                var semPermissao = ChecarPermissaoEscrita();
                if (semPermissao != null) return semPermissao;

                if (id <= 0) return BadRequest("id inválido.");
                if (req == null) return BadRequest("Payload inválido.");

                // Operador pode mudar o status apenas das ocorrências que ele criou.
                var negado = await ChecarAutoriaOperador(id);
                if (negado != null) return negado;

                var empresaId = User.GetEmpresaId();
                var ok = await _service.UpdateStatus(id, req.Status, empresaId);
                return ok ? Ok(true) : BadRequest("Falha ao atualizar status.");
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
                var semPermissao = ChecarPermissaoEscrita();
                if (semPermissao != null) return semPermissao;

                if (id <= 0) return BadRequest("id inválido.");

                var negado = await ChecarAutoriaOperador(id);
                if (negado != null) return negado;

                var empresaId = User.GetEmpresaId();
                var ok = await _service.Delete(id, empresaId);
                return ok ? Ok("Ocorrência excluída com sucesso.") : BadRequest("Falha ao excluir ocorrência.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
