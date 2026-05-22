using Core.Enums;

namespace Core.DTO
{
    /// <summary>
    /// [v2] Payload de bulk update do relatório (título + seções num único PUT).
    /// Substitui a necessidade de chamadas granulares para cada item/foto/comentário
    /// durante a edição pelo operador. Endpoints granulares continuam ativos por compat.
    /// </summary>
    public class UpdateRelatorioV2Request
    {
        public string? Titulo { get; set; }
        public List<UpdateRelatorioSecaoV2Request> Secoes { get; set; } = new();
    }

    public class UpdateRelatorioSecaoV2Request
    {
        /// <summary>
        /// ID existente da seção. Quando 0 ou null, é uma seção nova.
        /// </summary>
        public int? Id { get; set; }
        public TipoSecao TipoSecao { get; set; }
        public int Ordem { get; set; } = 0;
        public string? Titulo { get; set; }
        public string DataSecao { get; set; } = string.Empty;
        public string? ConteudoJson { get; set; }
        public int? TipoOcorrenciaId { get; set; }
    }
}
