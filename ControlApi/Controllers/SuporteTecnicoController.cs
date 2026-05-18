using Core.DTO;
using Core.Models;
using Infrastructure.Authenticate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace ControlApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SuporteTecnicoController : ControllerBase
    {
        private readonly IJWTManager _jWTManager;
        private readonly ISupportTicketsService _supportTicketsService;

        public SuporteTecnicoController(IJWTManager jWTManager, ISupportTicketsService supportTicketsService)
        {
            _jWTManager = jWTManager;
            _supportTicketsService = supportTicketsService;
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("create")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> Create([FromForm] CreateSupportTicketRequest req)
        {
            try
            {
                if (req.EmpresaId <= 0) return BadRequest("EmpresaId inválido.");
                if (string.IsNullOrWhiteSpace(req.Title)) return BadRequest("Título é obrigatório.");
                if (string.IsNullOrWhiteSpace(req.Description)) return BadRequest("Descrição é obrigatória.");

                byte[]? bytes = null;
                string? fileName = null;
                string? contentType = null;

                if (req.Attachment != null && req.Attachment.Length > 0)
                {
                    if (req.Attachment.Length > 10 * 1024 * 1024) return BadRequest("Anexo excede 10MB.");

                    using var ms = new MemoryStream();
                    await req.Attachment.CopyToAsync(ms);
                    bytes = ms.ToArray();
                    fileName = req.Attachment.FileName;
                    contentType = req.Attachment.ContentType;
                }

                var ticket = new SupportTicket
                {
                    EmpresaId = req.EmpresaId,
                    Subject = req.Subject,
                    Title = req.Title,
                    Description = req.Description,
                    Status = 1,
                    AttachmentBytes = bytes,
                    AttachmentFileName = fileName,
                    AttachmentContentType = contentType
                };

                var result = await _supportTicketsService.Create(ticket);

                if (result.Id > 0)
                    return Ok("Chamado enviado com sucesso.");
                else
                    return BadRequest("Erro ao enviar chamado.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("getPaged")]
        public async Task<IActionResult> GetPaged([FromQuery] FiltersSupportTicketsDTO filtersDTO)
        {
            try
            {
                var result = await _supportTicketsService.GetPaged(filtersDTO);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateSupportTicketRequest req)
        {
            if (id <= 0) return BadRequest("id inválido.");
            if (req == null) return BadRequest("Payload inválido.");

            var existing = await _supportTicketsService.GetById(id);
            if (existing == null) return NotFound("Chamado não encontrado.");

            if (req.Subject.HasValue) existing.Subject = req.Subject.Value;
            if (req.Status.HasValue) existing.Status = req.Status.Value;

            if (req.Title != null) existing.Title = string.IsNullOrWhiteSpace(req.Title) ? existing.Title : req.Title;
            if (req.Description != null) existing.Description = string.IsNullOrWhiteSpace(req.Description) ? existing.Description : req.Description;

            var result = await _supportTicketsService.Update(existing, id);
            if (result) return Ok(true);

            return BadRequest("Falha ao atualizar chamado.");
        }

        [AllowAnonymous]
        [HttpDelete]
        [Route("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                bool result = await _supportTicketsService.Delete(id);
                if (result)
                    return Ok("Chamado excluído com sucesso.");
                else
                    return BadRequest("Falha ao excluir chamado.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("toggle-status/{id}")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                bool result = await _supportTicketsService.ToggleStatus(id);
                if (result)
                    return Ok("Status do chamado alterado com sucesso.");
                else
                    return BadRequest("Falha ao alterar o status do chamado.");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet("getById/{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0) return BadRequest("id inválido.");

            var ticket = await _supportTicketsService.GetById(id);
            if (ticket == null) return NotFound("Chamado não encontrado.");

            var dto = new SupportTicketDTO
            {
                Id = ticket.Id,
                EmpresaId = ticket.EmpresaId,
                Subject = ticket.Subject,
                Title = ticket.Title,
                Description = ticket.Description,
                Status = ticket.Status,
                HasAttachment = ticket.AttachmentBytes != null && ticket.AttachmentBytes.Length > 0,
                AttachmentFileName = ticket.AttachmentFileName,
                AttachmentContentType = ticket.AttachmentContentType,
                CreatedDate = ticket.CreatedDate,
                UpdatedDate = ticket.UpdatedDate
            };

            return Ok(dto);
        }

        [AllowAnonymous]
        [HttpGet]
        [Route("simple")]
        public async Task<IActionResult> GetSimple([FromQuery] int empresaId)
        {
            try
            {
                if (empresaId <= 0) return BadRequest("empresaId inválido.");
                var result = await _supportTicketsService.GetSimple(empresaId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpGet("download-attachment/{id}")]
        public async Task<IActionResult> DownloadAttachment(int id)
        {
            if (id <= 0) return BadRequest("id inválido.");

            var ticket = await _supportTicketsService.GetById(id);
            if (ticket == null) return NotFound("Chamado não encontrado.");
            if (ticket.AttachmentBytes == null || ticket.AttachmentBytes.Length == 0) return NotFound("Chamado não possui anexo.");

            var contentType = string.IsNullOrWhiteSpace(ticket.AttachmentContentType) ? "application/octet-stream" : ticket.AttachmentContentType;
            var fileName = string.IsNullOrWhiteSpace(ticket.AttachmentFileName) ? $"anexo_chamado_{id}" : ticket.AttachmentFileName;

            return File(ticket.AttachmentBytes, contentType, fileName);
        }
    }
}