namespace Core.DTO
{
    public class UpdateChecklistRequest
    {
        public string? Nome { get; set; }
        public int? Status { get; set; }

        // [Conferelist v2] Campo ausente no JSON = mantém; presente (inclusive null/"") = grava.
        private string? _descricao;
        public string? Descricao { get => _descricao; set { _descricao = value; DescricaoInformada = true; } }
        [System.Text.Json.Serialization.JsonIgnore] public bool DescricaoInformada { get; private set; }

        private string? _categoria;
        public string? Categoria { get => _categoria; set { _categoria = value; CategoriaInformada = true; } }
        [System.Text.Json.Serialization.JsonIgnore] public bool CategoriaInformada { get; private set; }
    }
}