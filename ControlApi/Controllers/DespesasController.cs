using Core.DTO;
using Core.Models;
using Infrastructure.Authenticate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ControlApi.Controllers
{
    /// <summary>
    /// Endpoints transacionais de despesas. Despesa não armazena <c>EmpresaId</c> diretamente —
    /// o escopo é derivado pela <c>Obras.EmpresaId</c> referenciada em <c>Despesa.ObraId</c>.
    /// <para>
    /// Regras:
    /// <list type="bullet">
    /// <item><b>Criar/Atualizar</b>: qualquer usuário autenticado (operador também lança despesa).</item>
    /// <item><b>Excluir / Toggle-status</b>: somente <c>admin</c>/<c>gerente</c>.</item>
    /// <item><b>Listagens e relatórios</b>: filtrados pela empresa do JWT.</item>
    /// </list>
    /// </para>
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DespesasController : ControllerBase
    {
        private readonly IJWTManager _jWTManager;
        private readonly IDespesasService _despesasService;
        private readonly IObrasService _obrasService;

        public DespesasController(IJWTManager jWTManager, IDespesasService despesasService, IObrasService obrasService)
        {
            _jWTManager = jWTManager;
            _despesasService = despesasService;
            _obrasService = obrasService;
        }

        // Carrega a obra e valida que pertence à empresa do JWT.
        private async Task<(Obras? obra, IActionResult? denied)> LoadObraAndAssertEmpresa(int obraId)
        {
            var obra = await _obrasService.GetObraById(obraId);
            if (obra == null) return (null, BadRequest("Obra informada não encontrada."));

            var empresaJwt = User.GetEmpresaId();
            if (obra.EmpresaId != empresaJwt)
                return (null, StatusCode(StatusCodes.Status403Forbidden, "Obra pertence a outra empresa."));

            return (obra, null);
        }

        // Despesa → Obra → EmpresaId
        private async Task<(Despesas? despesa, IActionResult? denied)> LoadDespesaAndAssertEmpresa(int despesaId)
        {
            var despesa = await _despesasService.GetDespesaById(despesaId);
            if (despesa == null) return (null, NotFound("Despesa não encontrada."));

            var (obra, denied) = await LoadObraAndAssertEmpresa(despesa.ObraId);
            if (obra == null) return (null, denied);

            return (despesa, null);
        }

        private IActionResult? AssertAdminOrGerente()
        {
            if (!User.IsAdminOrGerente())
                return StatusCode(StatusCodes.Status403Forbidden, "Apenas admin/gerente podem realizar esta ação.");
            return null;
        }

        // ---------------------------------------------------------------------
        // CRUD
        // ---------------------------------------------------------------------

        /// <summary>
        /// Cria despesa. Operador também pode lançar — só exige que a obra seja da empresa do JWT.
        /// </summary>
        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateDespesa([FromBody] CreateDespesaRequest req)
        {
            try
            {
                if (req == null) return BadRequest("Payload inválido.");

                var (obra, denied) = await LoadObraAndAssertEmpresa(req.ObraId);
                if (obra == null) return denied!;

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
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Paginação de despesas. <c>FiltersDespesasDTO</c> não tem EmpresaId; o filtro é
        /// aplicado pós-query usando as obras da empresa do JWT. Se um <c>obraId</c> for
        /// passado, ele precisa pertencer à empresa.
        /// </summary>
        [HttpGet]
        [Route("getDespesasPaged")]
        public async Task<IActionResult> GetDespesasPaged([FromQuery] FiltersDespesasDTO filtersDTO)
        {
            try
            {
                filtersDTO ??= new FiltersDespesasDTO();
                var empresaJwt = User.GetEmpresaId();

                // Quando vem ObraId no filtro, valida posse antes de consultar.
                if (filtersDTO.ObraId.HasValue && filtersDTO.ObraId.Value > 0)
                {
                    var (obra, denied) = await LoadObraAndAssertEmpresa(filtersDTO.ObraId.Value);
                    if (obra == null) return denied!;
                }
                else
                {
                    // Sem ObraId: restringimos à empresa filtrando pelas obras dela.
                    var obrasEmpresa = await _obrasService.GetObrasSimple();
                    var obraIds = obrasEmpresa.Where(o => o.EmpresaId == empresaJwt).Select(o => o.Id).ToHashSet();

                    var result = await _despesasService.GetDespesasPaged(filtersDTO);
                    if (result == null) return BadRequest();

                    result.Result = result.Result.Where(d => obraIds.Contains(d.ObraId)).ToList();
                    return Ok(result);
                }

                var paged = await _despesasService.GetDespesasPaged(filtersDTO);
                if (paged != null) return Ok(paged);
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

        [HttpPut("{despesaId}")]
        public async Task<IActionResult> UpdateDespesa(int despesaId, [FromBody] UpdateDespesaRequest req)
        {
            try
            {
                if (despesaId <= 0) return BadRequest("despesaId inválido.");
                if (req == null) return BadRequest("Payload i