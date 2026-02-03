using Core.DTO;
using Core.Models;
using Infrastructure.Authenticate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;
using SharpCompress;
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
                    ClientDocument = obras.ClientDocument
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
                ClientDocument = obra.ClientDocument
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
    }
}