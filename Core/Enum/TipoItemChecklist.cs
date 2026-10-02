namespace Core.Enums
{
    /// <summary>[Conferelist v2] Tipo de resposta de um item de checklist.</summary>
    public enum TipoItemChecklist
    {
        /// <summary>Usa Resposta: 0 pendente, 1 Conforme, 2 Não conforme, 3 N/A (legado, padrão).</summary>
        ConformeNaoConformeNA = 1,
        /// <summary>Usa Resposta: 0 pendente, 1 Sim, 2 Não, 3 N/A.</summary>
        SimNao = 2,
        /// <summary>Usa Valor.</summary>
        Texto = 3,
        /// <summary>Usa Valor (número em formato invariante, ex.: "12.5").</summary>
        Numero = 4,
        /// <summary>Usa Valor ("YYYY-MM-DD").</summary>
        Data = 5,
        /// <summary>Usa Valor = uma das Opcoes.</summary>
        Selecao = 6,
        /// <summary>Resposta = ter ao menos 1 foto (Resposta 1 com foto, 0 sem).</summary>
        Foto = 7
    }
}
