namespace Core.Models
{
	/// <summary>[Conferelist v2] Foto anexada a um item de checklist executado na obra (S3).</summary>
	public class ObraChecklistItemFoto : BaseModel
	{
		public required int ObraChecklistItemId { get; set; }
		public ObraChecklistItem? ObraChecklistItem { get; set; }
		public string S3Url { get; set; } = string.Empty;
		public required string ContentType { get; set; }
		public string? NomeArquivo { get; set; }
		public string? Legenda { get; set; }
		public int? CriadoPorUserId { get; set; }
	}
}
