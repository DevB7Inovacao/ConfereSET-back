namespace Core.Models
{
    public class ChecklistItem : BaseModel
    {
        public required int EmpresaId { get; set; }
        public required int ChecklistId { get; set; }
        public Checklist? Checklist { get; set; }
        public required string Descricao { get; set; }
        public int Ordem { get; set; } = 0;
        public int Status { get; set; } = 1;
        // [Conferelist v2] Configuração do item no modelo.
        /// <summary>Ver <see cref="Core.Enums.TipoItemChecklist"/>. 1 = Conforme/Não conforme/N/A (legado).</summary>
        public int Tipo { get; set; } = 1;
        public string? Grupo { get; set; }
        public bool Obrigatorio { get; set; } = false;
        public bool ExigirFotoNaoConforme { get; set; } = false;
        public bool ExigirObservacaoNaoConforme { get; set; } = false;
        public string? Ajuda { get; set; }
        /// <summary>JSON array de strings (Tipo = Selecao).</summary>
        public string? Opcoes { get; set; }
    }
}