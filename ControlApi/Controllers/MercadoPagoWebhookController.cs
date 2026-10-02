using Core.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Services;
using System.Security.Cryptography;
using System.Text;

namespace ControlApi.Controllers
{
	/// <summary>
	/// Webhook do Mercado Pago. Recebe notificações de mudança de status de preapprovals
	/// (subscription_preapproval) e de pagamentos (payment).
	/// <para>
	/// Quando a configuração <c>MercadoPago:WebhookSecret</c> está presente, valida a assinatura
	/// HMAC SHA-256 enviada nos headers <c>x-signature</c> e <c>x-request-id</c>, conforme
	/// documentado em
	/// https://www.mercadopago.com.br/developers/pt/docs/your-integrations/notifications/webhooks
	/// </para>
	/// <para>
	/// Em desenvolvimento, se o segredo não estiver configurado, a validação é pulada (mas com log
	/// de aviso) para não atrapalhar testes locais.
	/// </para>
	/// </summary>
	[Route("api/webhook/mercadopago")]
	[ApiController]
	public class MercadoPagoWebhookController : ControllerBase
	{
		private readonly IAssinaturaService _assinaturaService;
		private readonly string? _webhookSecret;
		private readonly ILogger<MercadoPagoWebhookController> _logger;

		public MercadoPagoWebhookController(
			IAssinaturaService assinaturaService,
			IConfiguration configuration,
			ILogger<MercadoPagoWebhookController> logger)
		{
			_assinaturaService = assinaturaService;
			_webhookSecret = configuration["MercadoPago:WebhookSecret"];
			_logger = logger;
		}

		[AllowAnonymous]
		[HttpPost]
		public async Task<IActionResult> Receive(
			[FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] MercadoPagoWebhookPayload? payload,
			[FromQuery(Name = "data.id")] string? dataIdFromQuery,
			[FromQuery(Name = "type")] string? typeFromQuery,
			[FromQuery(Name = "topic")] string? topicFromQuery,
			[FromQuery(Name = "id")] string? idFromQuery)
		{
			// Formatos aceitos: webhook novo (JSON com type + data.id, e data.id também na query)
			// e IPN antigo (?topic=preapproval&id=...).
			var dataId = (dataIdFromQuery ?? payload?.Data?.Id ?? idFromQuery ?? string.Empty).Trim();
			var tipo = (payload?.Type ?? typeFromQuery ?? topicFromQuery ?? string.Empty).Trim().ToLowerInvariant();
			if (string.IsNullOrEmpty(dataId) || string.IsNullOrEmpty(tipo))
				return Ok();

			if (!IsSignatureValid(dataId, out var motivo))
			{
				_logger.LogWarning("Webhook MP rejeitado: {Motivo} (tipo={Tipo} id={Id})", motivo, tipo, dataId);
				// 200 para o MP não reenviar sem parar; o evento fica no log.
				return Ok();
			}

			try
			{
				switch (tipo)
				{
					case "subscription_preapproval":
					case "preapproval":
						await _assinaturaService.ProcessarWebhookAssinatura(dataId);
						break;
					case "subscription_authorized_payment":
					case "authorized_payment":
						await _assinaturaService.ProcessarWebhookCobrancaAssinatura(dataId);
						break;
					case "payment":
						await _assinaturaService.ProcessarWebhookPagamento(dataId);
						break;
					default:
						_logger.LogInformation("Webhook MP ignorado: tipo={Tipo} id={Id}", tipo, dataId);
						break;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Erro processando webhook MP tipo={Tipo} id={Id}", tipo, dataId);
				// 500 → o MP tenta de novo mais tarde (falha temporária: rede, MP instável, banco).
				return StatusCode(StatusCodes.Status500InternalServerError);
			}

			return Ok();
		}

		/// <summary>
		/// Implementa a verificação HMAC do MP. Retorna <c>true</c> também quando o segredo não
		/// está configurado — útil em desenvolvimento.
		/// </summary>
		private bool IsSignatureValid(string dataId, out string motivo)
		{
			motivo = string.Empty;

			if (string.IsNullOrWhiteSpace(_webhookSecret))
			{
				_logger.LogWarning("MercadoPago:WebhookSecret não configurado — validação HMAC desativada.");
				return true;
			}

			var xSignature = Request.Headers["x-signature"].ToString();
			var xRequestId = Request.Headers["x-request-id"].ToString();

			if (string.IsNullOrEmpty(xSignature) || string.IsNullOrEmpty(xRequestId))
			{
				motivo = "Headers x-signature/x-request-id ausentes.";
				return false;
			}

			// x-signature vem no formato "ts=1234567890,v1=abcdef..."
			string? ts = null, v1 = null;
			foreach (var raw in xSignature.Split(','))
			{
				var kv = raw.Trim().Split('=', 2);
				if (kv.Length != 2) continue;
				if (kv[0] == "ts") ts = kv[1];
				else if (kv[0] == "v1") v1 = kv[1];
			}

			if (string.IsNullOrEmpty(ts) || string.IsNullOrEmpty(v1))
			{
				motivo = "x-signature mal formatado.";
				return false;
			}

			// String a assinar conforme docs do MP:
			// id:{data.id};request-id:{x-request-id};ts:{ts};
			var manifest = $"id:{dataId.ToLowerInvariant()};request-id:{xRequestId};ts:{ts};";

			using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_webhookSecret!));
			var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest));
			var calculated = Convert.ToHexString(hash).ToLowerInvariant();

			if (!CryptographicOperations.FixedTimeEquals(
				Encoding.UTF8.GetBytes(calculated),
				Encoding.UTF8.GetBytes(v1!.ToLowerInvariant())))
			{
				motivo = "Assinatura HMAC não confere.";
				return false;
			}

			return true;
		}
	}
}