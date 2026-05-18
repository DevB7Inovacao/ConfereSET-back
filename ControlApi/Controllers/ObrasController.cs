using Core.DTO;
using Core.Models;
using Infrastructure.Authenticate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;
using System.Security.Claims;

namespace ControlApi.Controllers
{
    /// <summary>
    /// Endpoints transacionais de obras e seus relacionamentos (operadores, mão-de-obra,
    /// equipamentos, tipos de ocorrência, modelos de texto, despesas).
    /// <para>
    /// Regras de autorização (validadas neste controller):
    /// <list type="bullet">
    /// <item><b>Empresa</b>: somente recursos da própria empresa do JWT.</item>
    /// <item><b>Escrita</b>: criar/atualizar/excluir/toggle-status exige <c>admin</c>/<c>gerente</c>.</item>
    /// <item><b>Filhos</b>: validar que o recurso filho (operador, mão-de-obra, equipamento,
    /// tipo de ocorrência, modelo de texto, despesa) pertence à mesma empresa antes de vincular/desvincular.</item>
    /// </list>
    /// </para>
    /// Contratos (rotas/DTOs/métodos HTTP) preservados.
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ObrasController : ControllerBase
    {
        private readonly IJWTManager _jWTManager;
        private readonly IObrasService _obrasService;
        private readonly IUserService _userService;

        public ObrasController(IJWTManager jWTManager, IObrasService obrasService, IUserService userService)
        {
            _jWTManager = jWTManager;
            _obrasService = obrasService;
            _userService = userService;
        }

        // ---------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------

        /// <summary>
        /// Carrega a obra e valida que pertence à empresa do JWT.
        /// Retorna (null, NotFound) se não existe, (null, Forbidden) se de outra empresa.
        /// </summary>
        private async Task<(Obras? obra, IActionResult? denied)> LoadObraAndAssertEmpresa(int obraId)
        {
            var obra = await _obrasService.GetObraById(obraId);
            if (obra == null) return (null, NotFound("Obra não encontrada."));

            var empresaJwt = User.GetEmpresaId();
            if (obra.EmpresaId != empresaJwt)
                return (null, StatusCode(StatusCodes.Status403Forbidden, "Obra pertence a outra empresa."));

            return (obra, null);
        }

        private IActionResult? AssertAdminOrGerente()
        {
            if (!User.IsAdminOrGerente())
                return StatusCode(StatusCodes.Status403Forbidden, "Apenas admin/gerente podem realizar esta ação.");
            return null;
        }

        private async Task<IActionResult?> AssertOperadorBelongsToEmpresa(int operadorId)
        {
            var op = await _userService.GetUserById(operadorId);
            if (op == null) return NotFound("Operador não encontrado.");
            if (op.EmpresaId != User.GetEmpresaId())
                return StatusCode(StatusCodes.Status403Forbidden, "Operador pertence a outra empresa.");
            return null;
        }

        // ---------------------------------------------------------------------
        // CRUD principal
        // ---------------------------------------------------------------------

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateObra([FromBody] CreateObraRequest obras)
        {
            try
            {
                if (obras == null) return BadRequest("Payload inválido.");
                var deny = AssertAdminOrGerente();
                if (deny != null) return deny;

                var empresaJwt = User.GetEmpresaId();

                var obra = new Obras()
                {
                    Name = obras.Name,
                    Status = 1,
                    StreetAddress = obras.StreetAddress,
                    Number = obras.Number,
                    AddressLine2 = obras.AddressLine2,
                    Neighborhood = obras.Neighborhood,
                    City = obras.City,
                    State = obras.State,
                    PostalCode = obras.PostalCode,
                    Country = obras.Country,
                    ClientName = obras.ClientName,
                    ClientEmail = obras.ClientEmail,
                    ClientPhone = obras.ClientPhone,
                    ClientDocument = obras.ClientDocument,
                    // EmpresaId é sempre forçado pelo JWT, ignorando o body.
                    EmpresaId = empresaJwt,
                    StartDate = obras.StartDate,
                    ProgressPercentage = 0,
                    NameCompany = obras.NameCompany
                };

                var result = await _obrasService.CreateObra(obra);

                if (result.Id > 0)
                    return Ok("Obra cadastrada com sucesso.");
                else
                    return BadRequest("Erro ao cadastrar obra.");
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
        [Route("getObrasPaged")]
        public async Task<IActionResult> GetObrasPaged([FromQuery] FiltersObrasDTO filtersDTO)
        {
            try
            {
                filtersDTO ??= new FiltersObrasDTO();
                // Escopo de empresa é obrigatório e vem do JWT.
                filtersDTO.EmpresaId = User.GetEmpresaId();

                var result = await _obrasService.GetObrasPaged(filtersDTO);
                if (result != null)
                    return Ok(result);
                else
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

        [HttpPut("{obraId}")]
        public async Task<IActionResult> UpdateObra(int obraId, [FromBody] UpdateObraRequest req)
        {
            try
            {
                if (obraId <= 0) return BadRequest("obraId inválido.");
                if (req == null) return BadRequest("Payload inválido.");

                var deny = AssertAdminOrGerente();
                if (deny != null) return deny;

                var (existing, denied) = await LoadObraAndAssertEmpresa(obraId);
                if (existing == null) return denied!;

                if (!string.IsNullOrWhiteSpace(req.Name)) existing.Name = req.Name;

                if (req.StreetAddress != null) existing.StreetAddress = string.IsNullOrWhiteSpace(req.StreetAddress) ? null : req.StreetAddress;
                if (req.Number != null) existing.Number = string.IsNullOrWhiteSpace(req.Number) ? null : req.Number;
                if (req.AddressLine2 != null) existing.AddressLine2 = string.IsNullOrWhiteSpace(req.AddressLine2) ? null : req.AddressLine2;
                if (req.Neighborhood != null) existing.Neighborhood = string.IsNullOrWhiteSpace(req.Neighborhood) ? null : req.Neighborhood;
                if (req.City != null) existing.City = string.IsNullOrWhiteSpace(req.City) ? null : req.City;
                if (req.State != null) existing.State = string.IsNullOrWhiteSpace(req.State) ? null : req.State;
                if (req.PostalCode != null) existing.PostalCode = string.IsNullOrWhiteSpace(req.PostalCode) ? null : req.PostalCode;
                if (req.Country != null) existing.Country = string.IsNullOrWhiteSpace(req.Country) ? null : req.Country;

                if (req.ClientName != null) existing.ClientName = string.IsNullOrWhiteSpace(req.ClientName) ? null : req.ClientName;
                if (req.ClientEmail != null) existing.ClientEmail = string.IsNullOrWhiteSpace(req.ClientEmail) ? null : req.ClientEmail;
                if (req.ClientPhone != null) existing.ClientPhone = string.IsNullOrWhiteSpace(req.ClientPhone) ? null : req.ClientPhone;
                if (req.ClientDocument != null) existing.ClientDocument = string.IsNullOrWhiteSpace(req.ClientDocument) ? null : req.ClientDocument;
                if (req.NameCompany != null) existing.NameCompany = string.IsNullOrWhiteSpace(req.NameCompany) ? null : req.NameCompany;

                if (req.StartDate.HasValue) existing.StartDate = req.StartDate;

                var result = await _obrasService.UpdateObra(existing, obraId);
                if (result) return Ok(true);

                return BadRequest("Falha ao atualizar obra.");
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
        public async Task<IActionResult> DeleteObra(int id)
        {
            try
            {
                if (id <= 0) return BadRequest("id inválido.");
                var deny = AssertAdminOrGerente();
                if (deny != null) return deny;

                var (existing, denied) = await LoadObraAndAssertEmpresa(id);
                if (existing == null) return denied!;

                bool result = await _obrasService.DeleteObra(id);
                if (result)
                    return Ok("Obra excluída com sucesso.");
                else
                    return BadRequest("Falha ao excluir obra.");
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

        [HttpPost]
        [Route("toggle-status/{id}")]
        public async Task<IActionResult> ToggleObraStatus(int id)
        {
            try
            {
                if (id <= 0) return BadRequest("id inválido.");
                var deny = AssertAdminOrGerente();
                if (deny != null) return deny;

                var (existing, denied) = await LoadObraAndAssertEmpresa(id);
                if (existing == null) return denied!;

                bool result = await _obrasService.ToggleObraStatus(id);
                if (result)
                    return Ok("Status da obra alterado com sucesso.");
                else
                    return BadRequest("Falha ao alterar o status da obra.");
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

        [HttpGet("getById/{obraId}")]
        public async Task<IActionResult> GetById(int obraId)
        {
            try
            {
                if (obraId <= 0) return BadRequest("obraId inválido.");

                var (obra, denied) = await LoadObraAndAssertEmpresa(obraId);
                if (obra == null) return denied!;

                var dto = new ObrasDTO
                {
                    Id = obra.Id,
                    Name = obra.Name,
                    Status = obra.Status,
                    StreetAddress = obra.StreetAddress,
                    Number = obra.Number,
                    AddressLine2 = obra.AddressLine2,
                    Neighborhood = obra.Neighborhood,
                    City = obra.City,
                    State = obra.State,
                    PostalCode = obra.PostalCode,
                    Country = obra.Country,
                    ClientName = obra.ClientName,
                    ClientEmail = obra.ClientEmail,
                    ClientPhone = obra.ClientPhone,
                    ClientDocument = obra.ClientDocument,
                    EmpresaId = obra.EmpresaId,
                    StartDate = obra.StartDate,
                    ProgressPercentage = obra.ProgressPercentage,
                    NameCompany = obra.NameCompany
                };

                return Ok(dto);
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
        [Route("simple")]
        public async Task<IActionResult> GetSimple()
        {
            try
            {
                // GetObrasSimple não filtra por empresa no service; restringimos no controller.
                var empresaJwt = User.GetEmpresaId();
                var result = await _obrasService.GetObrasSimple();
                var filtered = result.Where(o => o.EmpresaId == empresaJwt).ToList();
                return Ok(filtered);
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

        // ---------------------------------------------------------------------
        // Operadores
        // ---------------------------------------------------------------------

        [HttpPost("{obraId}/operadores/{operadorId}")]
        public async Task<IActionResult> AddOperadorToObra(int obraId, int operadorId)
        {
            try
            {
                var deny = AssertAdminOrGerente();
                if (deny != null) return deny;

                var (obra, denied) = await LoadObraAndAssertEmpresa(obraId);
                if (obra == null) return denied!;

                var opDenied = await AssertOperadorBelongsToEmpresa(operadorId);
                if (opDenied != null) return opDenied;

                var result = await _obrasService.AddOperadorToObra(obraId, operadorId);
                if (result)
                    return Ok("Operador adicionado à obra com sucesso.");
                else
                    return BadRequest("Falha ao adicionar operador à obra.");
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

        [HttpDelete("{obraId}/operadores/{operadorId}")]
        public async Task<IActionResult> RemoveOperadorFromObra(int obraId, int operadorId)
        {
            try
            {
                var deny = AssertAdminOrGerente();
                if (deny != null) return deny;

                var (obra, denied) = await LoadObraAndAssertEmpresa(obraId);
                if (obra == null) return denied!;

                var opDenied = await AssertOperadorBelongsToEmpresa(operadorId);
                if (opDenied != null) return opDenied;

                var result = await _obrasService.RemoveOperadorFromObra(obraId, operadorId);
                if (result)
                    return Ok("Operador removido da obra com sucesso.");
                else
                    return BadRequest("Falha ao remover operador da obra.");
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

        [HttpGet("{obraId}/operadores")]
        public async Task<IActionResult> GetOperadoresByObraId(int obraId)
        {
            try
            {
                if (obraId <= 0) return BadRequest("obraId inválido.");

                var (obra, denied) = await LoadObraAndAssertEmpresa(obraId);
                if (obra == null) return denied!;

                var result = await _obrasService.GetOperadoresByObraId(obraId);
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

        [HttpGet("operador/{operadorId}")]
        public async Task<IActionResult> GetObrasByOperadorId(int operadorId)
        {
            try
            {
                if (operadorId <= 0) return BadRequest("operadorId inválido.");

                // Operador deve pertencer à empresa do JWT.
                var opDenied = await AssertOperadorBelongsToEmpresa(operadorId);
                if (opDenied != null) return opDenied;

                // Operador comum só pode listar as próprias obras.
                if (!User.IsAdminOrGerente() && User.GetUserId() != operadorId)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, "Você não pode listar obras de outro operador.");
                }

                var result = await _obrasService.GetObrasByOperadorId(operadorId);
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

        [HttpGet("{obraId}/with-operadores")]
        public async Task<IActionResult> GetObraWithOperadores(int obraId)
        {
            try
            {
                if (obraId <= 0) return BadRequest("obraId inválido.");

                var (obra, denied) = await LoadObraAndAssertEmpresa(obraId);
                if (obra == null) return denied!;

                var result = await _obrasService.GetObraWithOperadores(obraId);
                if (result == null) return NotFound("Obra não encontrada.");

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

        // ---------------------------------------------------------------------
        // Cards
        // ---------------------------------------------------------------------

        /// <summary>
        /// Lista cards de obras por empresa. O <c>empresaId</c> da URL é IGNORADO e
        /// sempre substituído pelo do JWT — evita bypass via troca de parâmetro.
        /// </summary>
        [HttpGet("cards/empresa/{empresaId}")]
        public async Task<IActionResult> GetObrasCardsByEmpresaId(int empresaId)
        {
            try
            {
                // Ignora o parâmetro de URL. Empresa vem sempre do JWT.
                var empresaJwt = User.GetEmpresaId();
                var result = await _obrasService.GetObrasCardsByEmpresaId(empresaJwt);
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

        [HttpGet("cards/operador/{operadorId}")]
        public async Task<IActionResult> GetObrasCardsByOperadorId(int operadorId)
        {
            try
            {
                if (operadorId <= 0) return BadRequest("operadorId inválido.");

                var opDenied = await AssertOperadorBelongsToEmpresa(operadorId);
                if (opDenied != null) return opDenied;

                // Operador comum só vê os próprios cards.
                if (!User.IsAdminOrGerente() && User.GetUserId() != operadorId)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, "Você não pode listar cards de outro operador.");
                }

                var result = await _obrasService.GetObrasCardsByOperadorId(operadorId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (Exception ex)
            {
