using Core.DTO;
using Core.Models;
using Infrastructure.Authenticate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;
using System.Security.Claims;

namespace ControlApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ObrasController : ControllerBase
    {
        private readonly IJWTManager _jWTManager;
        IObrasService _obrasService;

        public ObrasController(IJWTManager jWTManager, IObrasService obrasService)
        {
            this._jWTManager = jWTManager;
            this._obrasService = obrasService;
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateObra([FromBody] CreateObraRequest obras)
        {
            try
            {
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
                    EmpresaId = obras.EmpresaId,
                    StartDate = obras.StartDate,
                    ProgressPercentage = 0
                };

                var result = await _obrasService.CreateObra(obra);

                if (result.Id > 0)
                    return Ok("Obra cadastrada com sucesso.");
                else
                    return BadRequest("Erro ao cadastrar obra.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("getObrasPaged")]
        public async Task<IActionResult> GetObrasPaged([FromQuery] FiltersObrasDTO filtersDTO)
        {
            var result = await _obrasService.GetObrasPaged(filtersDTO);
            if (result != null)
                return Ok(result);
            else
                return BadRequest();
        }

        [AllowAnonymous]
        [HttpPut("{obraId}")]
        public async Task<IActionResult> UpdateObra(int obraId, [FromBody] UpdateObraRequest req)
        {
            if (obraId <= 0) return BadRequest("obraId inválido.");
            if (req == null) return BadRequest("Payload inválido.");

            var existing = await _obrasService.GetObraById(obraId);
            if (existing == null) return NotFound("Obra não encontrada.");

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

            if (req.StartDate.HasValue) existing.StartDate = req.StartDate;

            var result = await _obrasService.UpdateObra(existing, obraId);
            if (result) return Ok(true);

            return BadRequest("Falha ao atualizar obra.");
        }

        [AllowAnonymous]
        [HttpDelete]
        [Route("delete/{id}")]
        public async Task<IActionResult> DeleteObra(int id)
        {
            try
            {
                bool result = await _obrasService.DeleteObra(id);
                if (result)
                    return Ok("Obra excluída com sucesso.");
                else
                    return BadRequest("Falha ao excluir obra.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("toggle-status/{id}")]
        public async Task<IActionResult> ToggleObraStatus(int id)
        {
            try
            {
                bool result = await _obrasService.ToggleObraStatus(id);
                if (result)
                    return Ok("Status da obra alterado com sucesso.");
                else
                    return BadRequest("Falha ao alterar o status da obra.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet("getById/{obraId}")]
        public async Task<IActionResult> GetById(int obraId)
        {
            if (obraId <= 0) return BadRequest("obraId inválido.");

            var obra = await _obrasService.GetObraById(obraId);
            if (obra == null) return NotFound("Obra não encontrada.");

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
                ProgressPercentage = obra.ProgressPercentage
            };

            return Ok(dto);
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("simple")]
        public async Task<IActionResult> GetSimple()
        {
            try
            {
                var result = await _obrasService.GetObrasSimple();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPost("{obraId}/operadores/{operadorId}")]
        public async Task<IActionResult> AddOperadorToObra(int obraId, int operadorId)
        {
            try
            {
                var result = await _obrasService.AddOperadorToObra(obraId, operadorId);
                if (result)
                    return Ok("Operador adicionado à obra com sucesso.");
                else
                    return BadRequest("Falha ao adicionar operador à obra.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpDelete("{obraId}/operadores/{operadorId}")]
        public async Task<IActionResult> RemoveOperadorFromObra(int obraId, int operadorId)
        {
            try
            {
                var result = await _obrasService.RemoveOperadorFromObra(obraId, operadorId);
                if (result)
                    return Ok("Operador removido da obra com sucesso.");
                else
                    return BadRequest("Falha ao remover operador da obra.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet("{obraId}/operadores")]
        public async Task<IActionResult> GetOperadoresByObraId(int obraId)
        {
            try
            {
                if (obraId <= 0) return BadRequest("obraId inválido.");

                var result = await _obrasService.GetOperadoresByObraId(obraId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet("operador/{operadorId}")]
        public async Task<IActionResult> GetObrasByOperadorId(int operadorId)
        {
            try
            {
                if (operadorId <= 0) return BadRequest("operadorId inválido.");

                var result = await _obrasService.GetObrasByOperadorId(operadorId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet("{obraId}/with-operadores")]
        public async Task<IActionResult> GetObraWithOperadores(int obraId)
        {
            try
            {
                if (obraId <= 0) return BadRequest("obraId inválido.");

                var result = await _obrasService.GetObraWithOperadores(obraId);
                if (result == null) return NotFound("Obra não encontrada.");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet("cards/empresa/{empresaId}")]
        public async Task<IActionResult> GetObrasCardsByEmpresaId(int empresaId)
        {
            try
            {
                if (empresaId <= 0) return BadRequest("empresaId inválido.");

                var result = await _obrasService.GetObrasCardsByEmpresaId(empresaId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet("cards/operador/{operadorId}")]
        public async Task<IActionResult> GetObrasCardsByOperadorId(int operadorId)
        {
            try
            {
                if (operadorId <= 0) return BadRequest("operadorId inválido.");

                var result = await _obrasService.GetObrasCardsByOperadorId(operadorId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPost("{obraId}/mao-de-obra/{maoDeObraId}")]
        public async Task<IActionResult> AddMaoDeObraToObra(int obraId, int maoDeObraId)
        {
            try
            {
                var result = await _obrasService.AddMaoDeObraToObra(obraId, maoDeObraId);
                if (result)
                    return Ok("Mão de obra adicionada à obra com sucesso.");
                else
                    return BadRequest("Falha ao adicionar mão de obra à obra.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpDelete("{obraId}/mao-de-obra/{maoDeObraId}")]
        public async Task<IActionResult> RemoveMaoDeObraFromObra(int obraId, int maoDeObraId)
        {
            try
            {
                var result = await _obrasService.RemoveMaoDeObraFromObra(obraId, maoDeObraId);
                if (result)
                    return Ok("Mão de obra removida da obra com sucesso.");
                else
                    return BadRequest("Falha ao remover mão de obra da obra.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet("{obraId}/mao-de-obra")]
        public async Task<IActionResult> GetMaoDeObraByObraId(int obraId)
        {
            try
            {
                if (obraId <= 0) return BadRequest("obraId inválido.");

                var result = await _obrasService.GetMaoDeObraByObraId(obraId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPost("{obraId}/equipamentos/{equipamentoId}")]
        public async Task<IActionResult> AddEquipamentoToObra(int obraId, int equipamentoId)
        {
            try
            {
                var result = await _obrasService.AddEquipamentoToObra(obraId, equipamentoId);
                if (result)
                    return Ok("Equipamento adicionado à obra com sucesso.");
                else
                    return BadRequest("Falha ao adicionar equipamento à obra.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpDelete("{obraId}/equipamentos/{equipamentoId}")]
        public async Task<IActionResult> RemoveEquipamentoFromObra(int obraId, int equipamentoId)
        {
            try
            {
                var result = await _obrasService.RemoveEquipamentoFromObra(obraId, equipamentoId);
                if (result)
                    return Ok("Equipamento removido da obra com sucesso.");
                else
                    return BadRequest("Falha ao remover equipamento da obra.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet("{obraId}/equipamentos")]
        public async Task<IActionResult> GetEquipamentosByObraId(int obraId)
        {
            try
            {
                if (obraId <= 0) return BadRequest("obraId inválido.");

                var result = await _obrasService.GetEquipamentosByObraId(obraId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPost("{obraId}/tipos-ocorrencia/{tipoOcorrenciaId}")]
        public async Task<IActionResult> AddTipoOcorrenciaToObra(int obraId, int tipoOcorrenciaId)
        {
            try
            {
                var result = await _obrasService.AddTipoOcorrenciaToObra(obraId, tipoOcorrenciaId);
                if (result)
                    return Ok("Tipo de ocorrência adicionado à obra com sucesso.");
                else
                    return BadRequest("Falha ao adicionar tipo de ocorrência à obra.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpDelete("{obraId}/tipos-ocorrencia/{tipoOcorrenciaId}")]
        public async Task<IActionResult> RemoveTipoOcorrenciaFromObra(int obraId, int tipoOcorrenciaId)
        {
            try
            {
                var result = await _obrasService.RemoveTipoOcorrenciaFromObra(obraId, tipoOcorrenciaId);
                if (result)
                    return Ok("Tipo de ocorrência removido da obra com sucesso.");
                else
                    return BadRequest("Falha ao remover tipo de ocorrência da obra.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet("{obraId}/tipos-ocorrencia")]
        public async Task<IActionResult> GetTiposOcorrenciaByObraId(int obraId)
        {
            try
            {
                if (obraId <= 0) return BadRequest("obraId inválido.");

                var result = await _obrasService.GetTiposOcorrenciaByObraId(obraId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPost("{obraId}/modelos-texto/{modeloTextoId}")]
        public async Task<IActionResult> AddModeloTextoToObra(int obraId, int modeloTextoId)
        {
            try
            {
                var result = await _obrasService.AddModeloTextoToObra(obraId, modeloTextoId);
                if (result)
                    return Ok("Modelo de texto adicionado à obra com sucesso.");
                else
                    return BadRequest("Falha ao adicionar modelo de texto à obra.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpDelete("{obraId}/modelos-texto/{modeloTextoId}")]
        public async Task<IActionResult> RemoveModeloTextoFromObra(int obraId, int modeloTextoId)
        {
            try
            {
                var result = await _obrasService.RemoveModeloTextoFromObra(obraId, modeloTextoId);
                if (result)
                    return Ok("Modelo de texto removido da obra com sucesso.");
                else
                    return BadRequest("Falha ao remover modelo de texto da obra.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet("{obraId}/modelos-texto")]
        public async Task<IActionResult> GetModelosTextoByObraId(int obraId)
        {
            try
            {
                if (obraId <= 0) return BadRequest("obraId inválido.");

                var result = await _obrasService.GetModelosTextoByObraId(obraId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPost("{obraId}/despesas/{despesaId}")]
        public async Task<IActionResult> AddDespesaToObra(int obraId, int despesaId)
        {
            try
            {
                var result = await _obrasService.AddDespesaToObra(obraId, despesaId);
                if (result)
                    return Ok("Despesa adicionada à obra com sucesso.");
                else
                    return BadRequest("Falha ao adicionar despesa à obra.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpDelete("{obraId}/despesas/{despesaId}")]
        public async Task<IActionResult> RemoveDespesaFromObra(int obraId, int despesaId)
        {
            try
            {
                var result = await _obrasService.RemoveDespesaFromObra(obraId, despesaId);
                if (result)
                    return Ok("Despesa removida da obra com sucesso.");
                else
                    return BadRequest("Falha ao remover despesa da obra.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet("{obraId}/despesas")]
        public async Task<IActionResult> GetDespesasByObraId(int obraId)
        {
            try
            {
                if (obraId <= 0) return BadRequest("obraId inválido.");

                var result = await _obrasService.GetDespesasByObraId(obraId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}