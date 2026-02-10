using Microsoft.AspNetCore.Http;

namespace Core.DTO
{
    public class CreateSupportTicketRequest
    {
        public int EmpresaId { get; set; }
        public int Subject { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public IFormFile? Attachment { get; set; }
    }
}