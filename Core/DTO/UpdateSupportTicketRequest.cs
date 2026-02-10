namespace Core.DTO
{
    public class UpdateSupportTicketRequest
    {
        public int? Subject { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? Status { get; set; }
    }
}