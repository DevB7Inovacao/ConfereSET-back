namespace Core.Models
{
	/// <summary>Foto do comprovante de uma despesa (cupom fiscal, nota, recibo) — arquivo no S3.</summary>
	public class DespesaComprovante : BaseModel
	{
		public required int DespesaId { get; set; }
		public Despesas? Despesa { get; set; }
		public string S3Url { get; set; } = string.Empty;
		public required string ContentType { get; set; }
		public string? NomeArquivo { get; set; }
		public int? CriadoPorUserId { get; set; }
	}
}
