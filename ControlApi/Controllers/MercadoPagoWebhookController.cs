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
		public async Task<IActionResult> Receive([FromBody] MercadoPagoWebhookPayload payload, [FromQuery(Name = "data.id")] string? dataIdFromQuery)
		{
			if (payload?.Data?.Id == null && string.IsNullOrEmpty(dataIdFromQuery))
				return Ok();

			var dataId = payload?.Data?.Id ?? dataIdFromQuery ?? string.Empty;

			if (!IsSignatureValid(dataId, out var motivo))
			{
				_logger.LogWarning("Webhook MP rejeitado: {Motivo}", motivo);
				// Em vez de 401 (que faria o MP repetir indefinidamente), devolvemos 200 mas
				// não processamos nada. Já temos log do evento.
				return Ok();
			}

			try
			{
				if (payload?.Type == "subscription_preapproval")
					await _assinaturaService.ProcessarWebhookAssinatura(dataId);

				if (payload?.Type == "payment")
					await _assinaturaService.ProcessarWebhookPagamento(dataId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Erro processando webhook MP tipo={Type} id={Id}", payload?.Type, dataId);
				// Não relançamos para não disparar reentregas — o erro pode ser de dado, não de canal.
				return Ok();
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
			var manifest = $"id:{dataId};request-id:{xRequestId};ts:{ts};";

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
