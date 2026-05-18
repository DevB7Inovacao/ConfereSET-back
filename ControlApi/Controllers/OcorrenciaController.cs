using Core.DTO;
using Core.Enums;
using Core.Models;
using Infrastructure.Authenticate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;
using System.Linq;

namespace ControlApi.Controllers
{
    /// <summary>
    /// Endpoints de Ocorrência. Ocorrência não armazena <c>EmpresaId</c> diretamente —
    /// o escopo vem por <c>Ocorrencia.ObraId → Obras.EmpresaId</c>.
    /// <para>
    /// Regras:
    /// <list type="bullet">
    /// <item><b>Criar</b>: operador também pode registrar ocorrência (autor vem do JWT).</item>
    /// <item><b>Atualizar</b>: o próprio autor (operador) ou admin/gerente.</item>
    /// <item><b>UpdateStatus</b>: somente admin/gerente.</item>
    /// <item><b>Excluir</b>: somente admin/gerente.</item>
    /// </list>
    /// </para>
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class OcorrenciaController : ControllerBase
    {
        private readonly IOcorrenciaService _service;
        private readonly IObrasService _obrasService;

        public OcorrenciaController(IOcorrenciaService service, IObrasService obrasService)
        {
            _service = service;
            _obrasService = obrasService;
        }

        private async Task<(Obras? obra, IActionResult? denied)> LoadObraAndAssertEmpresa(int obraId)
        {
            var obra = await _obrasService.GetObraById(obraId);
            if (obra == null) return (null, BadRequest("Obra informada não encontrada."));

            var empresaJwt = User.GetEmpresaId();
            if (obra.EmpresaId != empresaJwt)
                return (null, StatusCode(StatusCodes.Status403Forbidden, "Obra pertence a outra empresa."));

            return (obra, null);
        }

        private async Task<(OcorrenciaDTO? ocorrencia, IActionResult? denied)> LoadOcorrenciaAndAssertEmpresa(int id)
        {
            var oc = await _service.GetById(id);
            if (oc == null) return (null, NotFound("Ocorrência não encontrada."));

            var (obra, denied) = await LoadObraAndAssertEmpresa(oc.ObraId);
            if (obra == null) return (null, denied);

            return (oc, null);
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateOcorrenciaRequest req)
        {
            try
            {
                if (req == null) return BadRequest("Payload inválido.");
                if (string.IsNullOrWhiteSpace(req.Titulo)) return BadRequest("Título é obrigatório.");

                // Valida que a obra pertence à empresa do JWT.
                var (obra, denied) = await LoadObraAndAssertEmpresa(req.ObraId);
                if (obra == null) return denied!;

                // Autoria vem do JWT — ignora valor enviado no body para evitar forja.
                req.CriadoPorUserId = User.GetUserId();

                var result = await _service.Create(req);
                return Ok(result.Id);
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

        [HttpGet("getPaged")]
        public async Task<IActionResult> GetPaged([FromQuery] FiltersOcorrenciaDTO filters)
        {
            try
            {
                filters ??= new FiltersOcorrenciaDTO();
                // EmpresaId é sempre forçado pelo JWT.
                filters.EmpresaId = User.GetEmpresaId();

                // Operador só vê as próprias ocorrências; admin/gerente vê todas da empresa.
                if (!User.IsAdminOrGerente())
                {
                    filters.CriadoPorUserId = User.GetUserId();
                }

                var result = await _service.GetPaged(filters);
                return Ok(result);
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

        [HttpGet("getBy