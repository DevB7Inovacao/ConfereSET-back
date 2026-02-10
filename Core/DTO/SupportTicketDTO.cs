using System;

namespace Core.DTO
{
    public class SupportTicketDTO
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public int Subject { get; set; }
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int Status { get; set; }
        public bool HasAttachment { get; set; }
        public string? AttachmentFileName { get; set; }
        public string? AttachmentContentType { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }

    public class SupportTicketPagedDTO
    {
        public int PageCount { get; set; }
        public IList<SupportTicketDTO> Result { get; set; } = new List<SupportTicketDTO>();
    }
}