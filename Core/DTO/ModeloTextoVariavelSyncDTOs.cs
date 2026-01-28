using System.Collections.Generic;

namespace Core.DTO
{
    public class SyncModeloTextoVariavelRequest
    {
        public int EmpresaId { get; set; }
        public int ModeloTextoId { get; set; }
        public List<string> Tokens { get; set; } = new();
    }

    public class SyncModeloTextoVariavelResponse
    {
        public int ModeloTextoId { get; set; }
        public int CreatedVariables { get; set; }
        public int CreatedLinks { get; set; }
        public int DisabledLinks { get; set; }
        public int EnabledLinks { get; set; }
        public List<string> Tokens { get; set; } = new();
    }

    public class ModeloTextoVariavelByModeloDTO
    {
        public int Id { get; set; } // VariavelId
        public int EmpresaId { get; set; }
        public string Nome { get; set; } = "";
        public string NomeAmigavel { get; set; } = "";
        public string NomePropriedade { get; set; } = "";
        public int Categoria { get; set; }
        public string? Classe { get; set; }
        public string? Valor { get; set; }
        public int Status { get; set; }

        public int VinculoId { get; set; }
        public int VinculoStatus { get; set; }
        public int ModeloTextoId { get; set; }
    }

    public class RenderModeloTextoRequest
    {
        /// <summary>
        /// Ex: { "{{DATA}}": "28/01/2026", "{{CLIENTE.NOME}}": "João" }
        /// </summary>
        public Dictionary<string, string?> Values { get; set; } = new();
    }

    public class RenderModeloTextoResponse
    {
        public int ModeloTextoId { get; set; }
        public string Html { get; set; } = "";
    }
}