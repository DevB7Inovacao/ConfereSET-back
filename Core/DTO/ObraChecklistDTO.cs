namespace Core.DTO
{
	public class ObraChecklistDTO
	{
		public int Id { get; set; }
		public int ObraId { get; set; }
		public int ChecklistId { get; set; }
		public string? ChecklistNome { get; set; }
		public int Status { get; set; }
		public List<ObraChecklistItemDTO> Itens { get; set; } = new();
		// [Conferelist v2]
		public string? Titulo { get; set; }
		public DateTime? ConcluidoEm { get; set; }
		public int? ConcluidoPorUserId { get; set; }
		public string? ConcluidoPorNome { get; set; }
		public string? ResponsavelNome { get; set; }
		public string? AssinaturaUrl { get; set; }
		public string? ObservacaoGeral { get; set; }
		public string? ChecklistDescricao { get; set; }
		public string? ChecklistCategoria { get; set; }
		public string? ObraNome { get; set; }
		public DateTime CreatedDate { get; set; }
	}

	/// <summary>Vínculo de checklist com um resumo da obra (evita expor a entidade Obra completa).</summary>
	public class ObraChecklistEmpresaDTO : ObraChecklistDTO
	{
		public ObraChecklistObraResumoDTO? Obra { get; set; }
	}

	public class ObraChecklistObraResumoDTO
	{
		public int Id { get; set; }
		public string? Name { get; set; }
		public int Status { get; set; }
	}

	public class ObraChecklistItemDTO
	{
		public int Id { get; set; }
		public int ObraChecklistId { get; set; }
		public int ChecklistItemId { get; set; }
		public string? Descricao { get; set; }
		public int Ordem { get; set; }
		/// <summary>0 = pendente, 1 = conforme, 2 = não conforme, 3 = não se aplica</summary>
		public int Resposta { get; set; }
		public string? Observacao { get; set; }
		public string? Empresa { get; set; }
		public string? DataHora { get; set; }
		public string? Equipamento { get; set; }
		public string? Marca { get; set; }
		// [Conferelist v2] Configuração vinda do ChecklistItem (modelo) + resposta estendida.
		public int Tipo { get; set; } = 1;
		public string? Grupo { get; set; }
		public bool Obrigatorio { get; set; }
		public bool ExigirFotoNaoConforme { get; set; }
		public bool ExigirObservacaoNaoConforme { get; set; }
		public string? Ajuda { get; set; }
		public string? Opcoes { get; set; }
		public string? Valor { get; set; }
		public int? RespondidoPorUserId { get; set; }
		public string? RespondidoPorNome { get; set; }
		public DateTime? RespondidoEm { get; set; }
		public List<ObraChecklistItemFotoDTO> Fotos { get; set; } = new();
	}

	public class ObraChecklistItemFotoDTO
	{
		public int Id { get; set; }
		public int ObraChecklistItemId { get; set; }
		public string S3Url { get; set; } = string.Empty;
		public string? ContentType { get; set; }
		public string? NomeArquivo { get; set; }
		public string? Legenda { get; set; }
		public DateTime CreatedDate { get; set; }
	}

	public class AddChecklistToObraRequest
	{
		public required int ObraId { get; set; }
		public required int ChecklistId { get; set; }
		/// <summary>[v2] Opcional. Padrão: nome do checklist + " – dd/MM/yyyy" quando já houver execução anterior.</summary>
		public string? Titulo { get; set; }
	}

	/// <summary>[v2] Item do POST /api/ObraChecklist/item/{itemId}/fotos (body = array).</summary>
	public class AddChecklistItemFotoRequest
	{
		public string? ImagemBase64 { get; set; }
		public string? ContentType { get; set; }
		public string? NomeArquivo { get; set; }
		public string? Legenda { get; set; }
	}

	public class UpdateChecklistItemFotoRequest
	{
		public string? Legenda { get; set; }
	}

	public class ConcluirObraChecklistRequest
	{
		public string? ResponsavelNome { get; set; }
		/// <summary>PNG em data URL ("data:image/png;base64,...") ou base64 puro.</summary>
		public string? AssinaturaBase64 { get; set; }
		public string? ObservacaoGeral { get; set; }
	}

	public class PendenciaChecklistDTO
	{
		public int ItemId { get; set; }
		public string? Descricao { get; set; }
		public string Motivo { get; set; } = string.Empty;
	}

	/// <summary>Escopo leve de uma execução (para autorização no controller).</summary>
	public class ObraChecklistEscopoDTO
	{
		public int ObraChecklistId { get; set; }
		public int ObraId { get; set; }
		public bool Concluido { get; set; }
	}

	public class ResponderChecklistItemRequest
	{
		/// <summary>0 = pendente, 1 = conforme/sim, 2 = não conforme/não, 3 = N/A. [v2] Opcional.</summary>
		public int? Resposta { get; set; }
		/// <summary>[v2] Resposta dos tipos Texto/Numero/Data/Selecao.</summary>
		public string? Valor { get; set; }
		public string? Observacao { get; set; }
		public string? Empresa { get; set; }
		public string? DataHora { get; set; }
		public string? Equipamento { get; set; }
		public string? Marca { get; set; }
	}
}