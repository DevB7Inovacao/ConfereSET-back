using System;

namespace Core.DTO
{
    public class FiltrosRelatorioDTO
    {
        public int? ObraId { get; set; }
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public int? Status { get; set; }
        public string? Categoria { get; set; }
    }
}