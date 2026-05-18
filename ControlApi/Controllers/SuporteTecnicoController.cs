using Core.DTO;
using Core.Models;
using Infrastructure.Authenticate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace ControlApi.Controllers
{
    /// <summary>
    /// Chamados de suporte técnico. Todos os endpoints exigem autenticação; o
    /// <c>EmpresaId</c> do recurso é sempre derivado do JWT (o body é ignorado) para impedir
    /// que um usuário abra ou liste chamados em nome de outra empresa.
    /// </summary>
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

        [HttpPost]
        [Route("create")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> Create([FromForm] CreateSupportTicketRequest req)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.Title)) return BadRequest("Título é obrigatório.");
                if (string.IsNullOrWhiteSpace(req.Description)) return BadRequest("Descrição é obrigatória.");

                var empresaJwt = User.GetEmpresaId();

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
                    // EmpresaId sempre vem do JWT — body ignorado por segurança.
                    EmpresaId = empresaJwt,
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
        [Route("getPaged")]
        public async Task<IActionResult> GetPaged([FromQuery] FiltersSupportTicketsDTO filtersDTO)
        {
            try
            {
                filtersDTO ??= new FiltersSupportTicketsDTO();
                // Escopo de empresa forçado pelo JWT.
                filtersDTO.EmpresaId = User.GetEmpresaId();

                var result = await _supportTicketsService.GetPaged(filtersDTO);
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

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateSupportTicketRequest req)
        {
            try
            {
                if (id <= 0) return BadRequest("id inválido.");
                if (req == null) return BadRequest("Payload inválido.");

                if (!User.IsAdminOrGerente())
                    return StatusCode(StatusCodes.Status403Forbidden, "Apenas admin/gerente podem alterar chamados.");

                var existing = await _supportTicketsService.GetById(id);
                if (existing == null) return NotFound("Chamado não encontrado.");

                var empresaJwt = User.GetEmpresaId();
                if (existing.EmpresaId != empresaJwt)
                    return StatusCode(StatusCodes.Status403Forbidden, "Chamado pertence a outra empresa.");

                if (req.Subject.HasValue) existing.Subject = req.Subject.Value;
                if (req.Status.HasValue) existing.Status = req.Status.Value;

                if (req.Title != null) existing.Title = string.IsNullOrWhiteSpace(req.Title) ? existing.Title : req.Title;
                if (req.Description != null) existing.Description = string.IsNullOrWhiteSpace(req.Description) ? existing.Description : req.Description;

                var result = await _supportTicketsService.Update(existing, id);
                if (result) return Ok(true);

                return BadRequest("Falha ao atualizar chamado.");
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
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id <= 0) return BadRequest("id inválido.");

                if (!User.IsAdminOrGerente())
                    return StatusCode(StatusCodes.Status403Forbidden, "Apenas admin/gerente podem excluir chamados.");

                var existing = await _supportTicketsService.GetById(id);
                if (existing == null) return NotFound("Chamado não encontrado.");

                var empresaJwt = User.GetEmpresaId();
                if (existing.EmpresaId != empresaJwt)
                    return StatusCode(StatusCodes.Status403Forbidden, "Chamado pertence a outra empresa.");

                bool result = await _supportTicketsService.Delete(id);
                return result ? Ok("Chamado excluído com sucesso.") : BadRequest("Falha ao excluir chamado.");
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
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                if (id <= 0) return BadRequest("id inválido.");

                if (!User.IsAdminOrGerente())
                    return StatusCode(StatusCodes.Status403Forbidden, "Apenas admin/gerente podem alterar o status.");

                var existing = await _supportTicketsService.GetById(id);
                if (existing == null) return NotFound("Chamado não encontrado.");

                var empresaJwt = User.GetEmpresaId();
                if (existing.EmpresaId != empresaJwt)
                    return StatusCode(StatusCodes.Status403Forbidden, "Chamado pertence a outra empresa.");

                bool result = await _supportTicketsService.ToggleS