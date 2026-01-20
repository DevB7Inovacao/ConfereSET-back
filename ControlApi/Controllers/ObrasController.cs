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
                    Country = obras.Country
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

            var merged = new Obras
            {
                Id = obraId,
                Name = string.IsNullOrWhiteSpace(req.Name) ? existing.Name : req.Name,
                Status = existing.Status,
                StreetAddress = string.IsNullOrWhiteSpace(req.StreetAddress) ? existing.StreetAddress : req.StreetAddress,
                Number = string.IsNullOrWhiteSpace(req.Number) ? existing.Number : req.Number,
                AddressLine2 = string.IsNullOrWhiteSpace(req.AddressLine2) ? existing.AddressLine2 : req.AddressLine2,
                Neighborhood = string.IsNullOrWhiteSpace(req.Neighborhood) ? existing.Neighborhood : req.Neighborhood,
                City = string.IsNullOrWhiteSpace(req.City) ? existing.City : req.City,
                State = string.IsNullOrWhiteSpace(req.State) ? existing.State : req.State,
                PostalCode = string.IsNullOrWhiteSpace(req.PostalCode) ? existing.PostalCode : req.PostalCode,
                Country = string.IsNullOrWhiteSpace(req.Country) ? existing.Country : req.Country
            };

            var result = await _obrasService.UpdateObra(merged, obraId);
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
                Country = obra.Country
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