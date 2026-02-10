using System;

namespace Core.DTO
{
    public class FiltersSupportTicketsDTO
    {
        public int? EmpresaId { get; set; }
        public int? Subject { get; set; }
        public int? Status { get; set; }
        public string? Title { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public int pageNumber { get; set; }
        public int pageSize { get; set; }

        public FiltersSupportTicketsDTO()
        {
            this.pageNumber = 1;
            this.pageSize = 9;
        }
    }
}