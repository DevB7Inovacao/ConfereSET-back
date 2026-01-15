namespace Core.DTO
{
    public class FiltersDTO
    {
        public string? Name { get; set; }
        public int pageNumber { get; set; }
        public int pageSize { get; set; }

        public FiltersDTO()
        {
            this.pageNumber = 1;
            this.pageSize = 9;
        }
    }
}
