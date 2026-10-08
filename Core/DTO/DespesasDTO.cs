namespace Core.DTO
{
	public class DespesaDTO
	{
		public int Id { get; set; }
		public string? Name { get; set; }
		public decimal Amount { get; set; }
		public DateTime Date { get; set; }
		public string? Category { get; set; }
		public string? Description { get; set; }
		public int ObraId { get; set; }
		public int Status { get; set; }
		public List<DespesaComprovanteDTO> Comprovantes { get; set; } = new();
	}

	public class DespesaComprovanteDTO
	{
		public int Id { get; set; }
		public int DespesaId { get; set; }
		public string Url { get; set; } = string.Empty;
		public string ContentType { get; set; } = string.Empty;
		public string? NomeArquivo { get; set; }
		public DateTime CreatedDate { get; set; }

		public static DespesaComprovanteDTO De(Core.Models.DespesaComprovante c) => new()
		{
			Id = c.Id,
			DespesaId = c.DespesaId,
			Url = c.S3Url,
			ContentType = c.ContentType,
			NomeArquivo = c.NomeArquivo,
			CreatedDate = c.CreatedDate,
		};
	}

	/// <summary>Foto do comprovante enviada em base64 (mesmo formato das fotos do Conferelist).</summary>
	public class AddDespesaComprovanteRequest
	{
		public string? ImagemBase64 { get; set; }
		public string? NomeArquivo { get; set; }
	}

	public class DespesasPagedDTO
	{
		public int PageCount { get; set; }
		public IList<DespesaDTO>? Result { get; set; }
	}
}