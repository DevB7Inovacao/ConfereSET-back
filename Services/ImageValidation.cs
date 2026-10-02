namespace Services
{
	/// <summary>Imagem já validada (bytes decodificados + tipo real detectado + nome seguro para o S3).</summary>
	public sealed record ImagemValidada(byte[] Bytes, string ContentType, string NomeArquivo);

	/// <summary>
	/// Validação compartilhada de uploads de imagem (fotos de relatório, fotos de checklist, assinaturas):
	/// base64 (tolera data URI), limite de 10 MB e tipo real por magic bytes (JPEG/PNG/WEBP).
	/// O ContentType enviado pelo cliente é sempre ignorado.
	/// </summary>
	public static class ImageValidation
	{
		public const int MaxBytes = 10 * 1024 * 1024; // 10 MB por imagem

		/// <summary>
		/// Detecta o tipo real pelos magic bytes (JPEG FFD8FF, PNG 89504E47, WEBP RIFF....WEBP).
		/// Retorna <c>null</c> para qualquer outro formato.
		/// </summary>
		public static (string ContentType, string Extensao)? DetectarTipoImagem(byte[] b)
		{
			if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)
				return ("image/jpeg", ".jpg");
			if (b.Length >= 4 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47)
				return ("image/png", ".png");
			if (b.Length >= 12
				&& b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46      // "RIFF"
				&& b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50)  // "WEBP"
				return ("image/webp", ".webp");
			return null;
		}

		/// <summary>Nome seguro para a key do S3 (sem path/espaços/acentos) com a extensão do tipo detectado.</summary>
		public static string SanitizarNomeArquivo(string? nome, string extensao, string padrao = "foto")
		{
			var baseName = Path.GetFileNameWithoutExtension(Path.GetFileName(nome ?? string.Empty));
			var limpo = new string(baseName.Select(c => char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_' ? c : '_').ToArray());
			if (string.IsNullOrWhiteSpace(limpo.Trim('_'))) limpo = padrao;
			if (limpo.Length > 80) limpo = limpo[..80];
			return limpo + extensao;
		}

		/// <summary>
		/// Valida e decodifica uma imagem em base64. Lança <see cref="Exception"/> com mensagem amigável
		/// (PT-BR) quando vazia, inválida, maior que 10 MB ou de formato não suportado.
		/// </summary>
		/// <param name="rotulo">Substantivo feminino usado nas mensagens ("foto", "assinatura").</param>
		public static ImagemValidada Validar(string? imagemBase64, string? nomeArquivo, string rotulo = "foto")
		{
			var nomeExibicao = string.IsNullOrWhiteSpace(nomeArquivo) ? "sem nome" : nomeArquivo!;
			if (string.IsNullOrWhiteSpace(imagemBase64))
				throw new Exception($"A {rotulo} '{nomeExibicao}' está vazia.");

			var base64 = imagemBase64.Trim();
			// Tolera data URI ("data:image/png;base64,....").
			var virgula = base64.IndexOf(',');
			if (base64.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && virgula >= 0)
				base64 = base64[(virgula + 1)..];

			// Checagem barata antes de decodificar (base64 ≈ 4/3 do binário).
			if ((long)base64.Length * 3 / 4 > MaxBytes + 3)
				throw new Exception($"A {rotulo} '{nomeExibicao}' excede o tamanho máximo de 10 MB.");

			byte[] bytes;
			try { bytes = Convert.FromBase64String(base64); }
			catch (FormatException) { throw new Exception($"A {rotulo} '{nomeExibicao}' não está em um formato válido."); }

			if (bytes.Length == 0)
				throw new Exception($"A {rotulo} '{nomeExibicao}' está vazia.");
			if (bytes.Length > MaxBytes)
				throw new Exception($"A {rotulo} '{nomeExibicao}' excede o tamanho máximo de 10 MB.");

			var tipo = DetectarTipoImagem(bytes);
			if (tipo == null)
				throw new Exception($"Formato de imagem não suportado em '{nomeExibicao}'. Envie apenas {(rotulo == "foto" ? "fotos" : "imagens")} JPEG, PNG ou WEBP.");

			return new ImagemValidada(bytes, tipo.Value.ContentType, SanitizarNomeArquivo(nomeArquivo, tipo.Value.Extensao, rotulo));
		}
	}
}
