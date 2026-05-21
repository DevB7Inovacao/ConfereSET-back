using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using BCryptNet = BCrypt.Net.BCrypt;

namespace Infrastructure.Security
{
	/// <summary>
	/// Operações de senha do sistema.
	/// <para>
	/// Algoritmo padrão atual: <b>BCrypt</b> (hash unidirecional com salt embutido).
	/// </para>
	/// <para>
	/// Compatibilidade com a base existente: senhas gravadas anteriormente foram cifradas com AES
	/// (reversível) e ficaram registradas em base64 estritamente alfanumérica/+/=. O método
	/// <see cref="VerifyPassword"/> reconhece os dois formatos e indica via <c>needsUpgrade</c>
	/// quando o caller deve regravar a senha como BCrypt — assim a migração é transparente para o
	/// usuário.
	/// </para>
	/// <para>
	/// O método legado <see cref="EncryptPassword"/> continua disponível por compatibilidade de
	/// build com chamadores antigos, mas <b>não deve</b> ser usado para novas senhas.
	/// </para>
	/// </summary>
	public static class Encrypt
	{
		// Custo do BCrypt. 12 é o padrão moderno (≈250ms em CPU comum, suficiente contra força bruta).
		private const int BCryptWorkFactor = 12;

		// Hashes BCrypt começam com $2a$, $2b$ ou $2y$. Usamos isso como discriminador de algoritmo.
		private static bool IsBCrypt(string hash) =>
			!string.IsNullOrEmpty(hash) &&
			(hash.StartsWith("$2a$") || hash.StartsWith("$2b$") || hash.StartsWith("$2y$"));

		/// <summary>
		/// Gera o hash da senha em texto puro usando BCrypt. Use sempre este método ao gravar uma
		/// nova senha (criação de usuário, atualização, reset).
		/// </summary>
		public static string HashPassword(string plainText)
		{
			if (string.IsNullOrEmpty(plainText))
				throw new ArgumentException("Senha vazia não pode ser hasheada.", nameof(plainText));

			return BCryptNet.HashPassword(plainText, BCryptWorkFactor);
		}

		/// <summary>
		/// Verifica a senha em texto puro contra o hash armazenado.
		/// </summary>
		/// <returns>
		/// <c>ok</c>: indica se a senha confere.
		/// <c>needsUpgrade</c>: indica que o hash armazenado está no algoritmo legado e o caller
		/// deve regravar usando <see cref="HashPassword"/>.
		/// </returns>
		public static (bool ok, bool needsUpgrade) VerifyPassword(string plainText, string stored)
		{
			if (string.IsNullOrEmpty(plainText) || string.IsNullOrEmpty(stored))
				return (false, false);

			if (IsBCrypt(stored))
			{
				try
				{
					return (BCryptNet.Verify(plainText, stored), false);
				}
				catch
				{
					return (false, false);
				}
			}

			// Caminho legado: o hash atual foi gerado por EncryptPassword (AES). Comparamos o
			// resultado da cifragem AES do plain com o que está gravado. Se bate, autenticamos e
			// sinalizamos upgrade — o caller deve regravar como BCrypt.
			try
			{
				var legacy = LegacyAesEncrypt(plainText);
				if (string.Equals(legacy, stored, StringComparison.Ordinal))
					return (true, true);
			}
			catch { /* segue para o último fallback */ }

			// Último fallback: senha armazenada em texto puro (legado muito antigo / imports).
			// Se bate, autentica e sinaliza upgrade pra regravar como BCrypt.
			if (string.Equals(plainText, stored, StringComparison.Ordinal))
				return (true, true);

			return (false, false);
		}

		// ---------------------------------------------------------------------
		// Algoritmo legado (AES). Mantido apenas para validar logins existentes.
		// NÃO use para criar novas senhas.
		// ---------------------------------------------------------------------

		/// <summary>
		/// Cifra a senha com AES (algoritmo legado). Mantido por compatibilidade. Não use para novas
		/// senhas — prefira <see cref="HashPassword"/>.
		/// </summary>
		[Obsolete("Use HashPassword (BCrypt) para novas senhas.")]
		public static string EncryptPassword(string plainText) => LegacyAesEncrypt(plainText);

		private static string LegacyAesEncrypt(string plainText)
		{
			var encryptionKeyBytes = CreateKey("@2023@rfe3stB7I");
			byte[] iv = new byte[16];
			byte[] array;

			using (Aes aes = Aes.Create())
			{
				aes.Key = encryptionKeyBytes;
				aes.IV = iv;

				ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

				using (MemoryStream memoryStream = new())
				{
					using (CryptoStream cryptoStream = new(memoryStream, encryptor, CryptoStreamMode.Write))
					{
						using (StreamWriter streamWriter = new(cryptoStream))
						{
							streamWriter.Write(plainText);
						}

						array = memoryStream.ToArray();
					}
				}
			}

			return Convert.ToBase64String(array);
		}

		public static string Decrypt(string cipherText, byte[] encryptionKeyBytes)
		{
			byte[] iv = new byte[16];
			byte[] buffer = Convert.FromBase64String(cipherText);

			using (Aes aes = Aes.Create())
			{
				aes.Key = encryptionKeyBytes;
				aes.IV = iv;
				ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

				using (MemoryStream memoryStream = new(buffer))
				{
					using (CryptoStream cryptoStream = new(memoryStream, decryptor, CryptoStreamMode.Read))
					{
						using (StreamReader streamReader = new(cryptoStream))
						{
							return streamReader.ReadToEnd();
						}
					}
				}
			}
		}

		private static readonly byte[] Salt = new byte[] { 10, 20, 30, 40, 50, 60, 70, 80 };
		public static byte[] CreateKey(string password, int keyBytes = 32)
		{
			const int Iterations = 300;
			var keyGenerator = new Rfc2898DeriveBytes(password, Salt, Iterations, HashAlgorithmName.SHA1);
			return keyGenerator.GetBytes(keyBytes);
		}
	}
}