using System;
using System.Collections.Generic;

namespace Core.DTO
{
    public class RelatorioResumoDTO
    {
        public decimal TotalGeral { get; set; }
        public int QuantidadeDespesas { get; set; }
        public decimal MediaPorDespesa { get; set; }
        public List<ResumoPorObraDTO> ResumosPorObra { get; set; } = new();
        public List<ResumoPorCategoriaDTO> ResumosPorCategoria { get; set; } = new();
        public DateTime? PeriodoInicio { get; set; }
        public DateTime? PeriodoFim { get; set; }
    }

    public class ResumoPorObraDTO
    {
        public int ObraId { get; set; }
        public string? ObraNome { get; set; }
        public decimal TotalObra { get; set; }
        public int QuantidadeDespesas { get; set; }
        public decimal PercentualDoTotal { get; set; }
    }

    public class ResumoPorCategoriaDTO
    {
        public string? Categoria { get; set; }
        public decimal TotalCategoria { get; set; }
        public int QuantidadeDespesas { get; set; }
        public decimal PercentualDoTotal { get; set; }
    }
}