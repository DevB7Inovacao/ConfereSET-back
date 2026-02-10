using System;

namespace Core.Models
{
    public class SupportTicket : BaseModel
    {
        public int EmpresaId { get; set; }
        public int Subject { get; set; }
        public required string Title { get; set; }
        public required string Description { get; set; }
        public int Status { get; set; } = 1;
        public string? AttachmentFileName { get; set; }
        public string? AttachmentContentType { get; set; }
        public byte[]? AttachmentBytes { get; set; }
    }
}