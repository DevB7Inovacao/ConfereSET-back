namespace Core.DTO
{
    public class ChecklistItemDTO
    {
        public int Id { get; set; }
        public int EmpresaId { get; set; }
        public int ChecklistId { get; set; }
        public string? Descricao { get; set; }
        public int Ordem { get; set; }
        public int Status { get; set; }
        // [Conferelist v2]
        public int Tipo { get; set; } = 1;
        public string? Grupo { get; set; }
        public bool Obrigatorio { get; set; }
        public bool ExigirFotoNaoConforme { get; set; }
        public bool ExigirObservacaoNaoConforme { get; set; }
        public string? Ajuda { get; set; }
        /// <summary>JSON array de strings (Tipo = Selecao).</summary>
        public string? Opcoes { get; set; }
    }

    public class CreateChecklistItemRequest
    {
        public required int EmpresaId { get; set; }
        public required int ChecklistId { get; set; }
        public required string Descricao { get; set; }
        public int Ordem { get; set; } = 0;
        // [Conferelist v2] Todos opcionais (front antigo não envia).
        public int? Tipo { get; set; }
        public string? Grupo { get; set; }
        public bool? Obrigatorio { get; set; }
        public bool? ExigirFotoNaoConforme { get; set; }
        public bool? ExigirObservacaoNaoConforme { get; set; }
        public string? Ajuda { get; set; }
        public string? Opcoes { get; set; }
    }

    public class UpdateChecklistItemRequest
    {
        public string? Descricao { get; set; }
        public int? Ordem { get; set; }
        public int? Status { get; set; }
        // [Conferelist v2] Campo ausente no JSON = mantém; presente (inclusive null) = grava.
        public int? Tipo { get; set; }
        public bool? Obrigatorio { get; set; }
        public bool? ExigirFotoNaoConforme { get; set; }
        public bool? ExigirObservacaoNaoConforme { get; set; }

        private string? _grupo;
        public string? Grupo { get => _grupo; set { _grupo = value; GrupoInformado = true; } }
        [System.Text.Json.Serialization.JsonIgnore] public bool GrupoInformado { get; private set; }

        private string? _ajuda;
        public string? Ajuda { get => _ajuda; set { _ajuda = value; AjudaInformada = true; } }
        [System.Text.Json.Serialization.JsonIgnore] public bool AjudaInformada { get; private set; }

        private string? _opcoes;
        public string? Opcoes { get => _opcoes; set { _opcoes = value; OpcoesInformadas = true; } }
        [System.Text.Json.Serialization.JsonIgnore] public bool OpcoesInformadas { get; private set; }
    }

    /// <summary>Item do POST /api/ChecklistItem/bulk.</summary>
    public class BulkChecklistItemRequest
    {
        public string? Descricao { get; set; }
        public int? Tipo { get; set; }
        public string? Grupo { get; set; }
        public bool? Obrigatorio { get; set; }
        public bool? ExigirFotoNaoConforme { get; set; }
        public bool? ExigirObservacaoNaoConforme { get; set; }
        public string? Ajuda { get; set; }
        public string? Opcoes { get; set; }
    }

    public class BulkCreateChecklistItensRequest
    {
        public int ChecklistId { get; set; }
        public List<BulkChecklistItemRequest> Itens { get; set; } = new();
    }

    public class ReorderChecklistItensRequest
    {
        public int ChecklistId { get; set; }
        public List<int> ItemIds { get; set; } = new();
    }
}