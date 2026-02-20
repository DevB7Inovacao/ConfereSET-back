using System.Collections.Generic;

namespace Core.DTO
{
    public class SyncChecklistVariavelRequest
    {
        public int EmpresaId { get; set; }
        public int ChecklistId { get; set; }
        public List<string> Tokens { get; set; } = new();
    }

    public class SyncChecklistVariavelResponse
    {
        public int ChecklistId { get; set; }
        public int CreatedVariables { get; set; }
        public int CreatedLinks { get; set; }
        public int DisabledLinks { get; set; }
        public int EnabledLinks { get; set; }
        public List<string> Tokens { get; set; } = new();
    }

    public class ChecklistVariavelByChecklistDTO
    {
        public int Id { get; set; }
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
        public int ChecklistId { get; set; }
    }

    public class RenderChecklistRequest
    {
        public Dictionary<string, string?> Values { get; set; } = new();
    }

    public class RenderChecklistResponse
    {
        public int ChecklistId { get; set; }
        public string Html { get; set; } = "";
    }
}