namespace Core.DTO
{
    public class ChecklistDTO
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public string? Nome { get; set; }
        public int Status { get; set; }
        // [Conferelist v2]
        public string? Descricao { get; set; }
        public string? Categoria { get; set; }
        /// <summary>Quantidade de itens ativos do modelo.</summary>
        public int TotalItens { get; set; }
    }

    public class DuplicarChecklistRequest
    {
        public string? Nome { get; set; }
    }

    public class ChecklistPagedDTO
    {
        public int PageCount { get; set; }
        public IList<ChecklistDTO> Result { get; set; } = new List<ChecklistDTO>();
    }
}