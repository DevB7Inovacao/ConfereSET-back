namespace Core.DTO
{
    public class FiltersTiposOcorrenciaDTO
    {
        public string? Search { get; set; }
        public int? Status { get; set; }
        public int? Gravidade { get; set; }
        public int pageNumber { get; set; }
        public int pageSize { get; set; }

        public FiltersTiposOcorrenciaDTO()
        {
            this.pageNumber = 1;
            this.pageSize = 9;
        }
    }
}