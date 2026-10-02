namespace Core.Enums
{
    public enum TipoSecao
    {
        Local = 1,
        MaoDeObra = 2,
        Equipamentos = 3,
        TextoLivre = 4,
        Fotos = 5,
        Comentarios = 6,
        Ocorrencias = 7,
        // [v2] Blocos configuráveis pelo modelo (data-config).
        Clima = 8,
        Assinatura = 9,
        Formulario = 10,
        Checklist = 11,
        // [v2] Despesas da obra vinculadas ao relatório (ids em ConteudoJson: { "despesaIds": [..] }).
        Despesas = 12
    }
}