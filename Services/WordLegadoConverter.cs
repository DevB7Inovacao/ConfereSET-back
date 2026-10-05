using DocSharp.Binary.DocFileFormat;
using DocSharp.Binary.OpenXmlLib.WordprocessingML;
using DocSharp.Binary.StructuredStorage.Reader;
using DocSharp.Binary.WordprocessingMLMapping;

namespace Services
{
	/// <summary>
	/// Converte documentos do Word 97-2003 (.doc, binário) para .docx, em C# puro (DocSharp,
	/// sem Office nem LibreOffice). O front importa o .docx resultante pelo mesmo caminho do
	/// .docx enviado direto (textos, tabelas, imagens, cabeçalho e rodapé).
	/// </summary>
	public static class WordLegadoConverter
	{
		/// <summary>Tamanho máximo aceito para o .doc.</summary>
		public const long TamanhoMaximo = 30 * 1024 * 1024;

		/// <summary>Assinatura OLE/Compound File (D0 CF 11 E0 A1 B1 1A E1) — formato do .doc.</summary>
		private static readonly byte[] AssinaturaOle = { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };

		public static bool EhDocBinario(ReadOnlySpan<byte> inicio) =>
			inicio.Length >= AssinaturaOle.Length && inicio[..AssinaturaOle.Length].SequenceEqual(AssinaturaOle);

		/// <summary>
		/// Converte o .doc para .docx. Lança <see cref="InvalidDataException"/> com mensagem para o
		/// usuário quando o arquivo não é um .doc válido (corrompido, protegido por senha etc.).
		/// </summary>
		public static byte[] ConverterParaDocx(byte[] doc)
		{
			if (doc == null || doc.Length == 0) throw new InvalidDataException("O arquivo está vazio.");
			if (doc.Length > TamanhoMaximo) throw new InvalidDataException("O arquivo passa de 30 MB. Reduza as imagens no Word e tente de novo.");
			if (!EhDocBinario(doc)) throw new InvalidDataException("Este arquivo não é um documento do Word 97-2003 (.doc).");

			try
			{
				using var entrada = new MemoryStream(doc, writable: false);
				using var reader = new StructuredStorageReader(entrada);
				var word = new WordDocument(reader);
				if (word.FIB != null && word.FIB.fEncrypted)
					throw new InvalidDataException("O documento está protegido por senha. Tire a senha no Word e envie de novo.");

				using var saida = new MemoryStream();
				// Modelo (.dot) ou com macro (.docm) também viram documento comum: só o conteúdo importa.
				using (var docx = WordprocessingDocument.Create(saida, DocSharp.Binary.OpenXmlLib.WordprocessingDocumentType.Document))
					Converter.Convert(word, docx);
				return saida.ToArray();
			}
			catch (InvalidDataException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new InvalidDataException("Não foi possível ler este .doc. Abra no Word e use Arquivo → Salvar como → Documento do Word (.docx).", ex);
			}
		}
	}
}
