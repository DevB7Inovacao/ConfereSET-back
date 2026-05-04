using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Services
{
	public interface IS3Service
	{
		Task<string> UploadImageAsync(byte[] imageBytes, string fileName, string contentType);
		Task<bool> DeleteImageAsync(string fileUrl);
		string GetBucketName();
	}

	public class S3Service : IS3Service
	{
		private readonly IAmazonS3 _s3Client;
		private readonly string _bucketName;
		private readonly string _bucketFolder;
		private readonly ILogger<S3Service> _logger;

		public S3Service(IConfiguration configuration, ILogger<S3Service> logger)
		{
			var awsOptions = configuration.GetSection("AWS");
			_bucketName = awsOptions["BucketName"] ?? throw new Exception("BucketName não configurado");
			_bucketFolder = awsOptions["BucketFolder"] ?? "";

			var config = new AmazonS3Config
			{
				RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(awsOptions["Region"]),
				ForcePathStyle = false,
				UseHttp = false
			};

			_s3Client = new AmazonS3Client(awsOptions["AccessKey"], awsOptions["SecretKey"], config);
			_logger = logger;
		}

		public string GetBucketName() => _bucketName;

		public async Task<string> UploadImageAsync(byte[] imageBytes, string fileName, string contentType)
		{
			try
			{
				// Gerar nome único para o arquivo
				var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
				var key = string.IsNullOrEmpty(_bucketFolder)
						? uniqueFileName
						: $"{_bucketFolder}/{uniqueFileName}";

				using var stream = new MemoryStream(imageBytes);

				var request = new Amazon.S3.Model.PutObjectRequest
				{
					BucketName = _bucketName,
					Key = key,
					InputStream = stream,
					ContentType = contentType,
					AutoCloseStream = true,
					Headers = {
												CacheControl = "max-age=31536000" // 1 ano de cache
                    }
				};

				var response = await _s3Client.PutObjectAsync(request);

				if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
				{
					// Retornar URL pública
					var url = $"https://{_bucketName}.s3.{_s3Client.Config.RegionEndpoint.SystemName}.amazonaws.com/{key}";
					return url;
				}

				throw new Exception("Falha no upload para S3");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Erro ao fazer upload para S3: {FileName}", fileName);
				throw;
			}
		}

		public async Task<bool> DeleteImageAsync(string fileUrl)
		{
			try
			{
				// Extrair a key da URL
				var uri = new Uri(fileUrl);
				var key = uri.AbsolutePath.TrimStart('/');

				var request = new Amazon.S3.Model.DeleteObjectRequest
				{
					BucketName = _bucketName,
					Key = key
				};

				var response = await _s3Client.DeleteObjectAsync(request);
				return response.HttpStatusCode == System.Net.HttpStatusCode.OK ||
							 response.HttpStatusCode == System.Net.HttpStatusCode.NoContent;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Erro ao deletar imagem do S3: {FileUrl}", fileUrl);
				return false;
			}
		}
	}
}