using System;
using System.Collections.Generic;

namespace Core.DTO
{
    public class RelatorioDetalhadoDTO
    {
        public List<DespesaRelatorioDTO> Despesas { get; set; } = new();
        public decimal TotalGeral { get; set; }
        public int QuantidadeTotal { get; set; }
        public DateTime? PeriodoInicio { get; set; }
        public DateTime? PeriodoFim { get; set; }
        public int? ObraIdFiltro { get; set; }
        public string? ObraNomeFiltro { get; set; }
    }

    public class DespesaRelatorioDTO
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string? Category { get; set; }
        public string? Description { get; set; }
        public int ObraId { get; set; }
        public string? ObraNome { get; set; }
        public int Status { get; set; }
    }
}