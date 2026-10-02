using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.MercadoPago
{
	public class MercadoPagoClient : IMercadoPagoClient
	{
		private readonly HttpClient _http;
		private readonly ILogger<MercadoPagoClient> _logger;

		private static readonly JsonSerializerOptions JsonOptions = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			NumberHandling = JsonNumberHandling.AllowReadingFromString
		};

		public MercadoPagoClient(HttpClient http, IConfiguration configuration, ILogger<MercadoPagoClient> logger)
		{
			_http = http;
			_logger = logger;
			var accessToken = configuration["MercadoPago:AccessToken"]
					?? throw new InvalidOperationException("MercadoPago:AccessToken não configurado.");
			_http.BaseAddress = new Uri("https://api.mercadopago.com/");
			_http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
		}

		public async Task<MPPreapprovalPlanResponse> CreatePreapprovalPlan(MPCreatePreapprovalPlanRequest req)
		{
			var json = await PostAsync("preapproval_plan", req);
			return Deserialize<MPPreapprovalPlanResponse>(json);
		}

		public async Task<MPPreapprovalPlanResponse> UpdatePreapprovalPlan(string planId, MPUpdatePreapprovalPlanRequest req)
		{
			var json = await PutAsync($"preapproval_plan/{planId}", req);
			return Deserialize<MPPreapprovalPlanResponse>(json);
		}

		public async Task<MPPreapprovalResponse> CreatePreapproval(MPCreatePreapprovalRequest req)
		{
			var json = await PostAsync("preapproval", req);
			return Deserialize<MPPreapprovalResponse>(json);
		}

		public async Task<MPPreapprovalResponseSimplified> GetPreapproval(string subscriptionId)
		{
			var response = await _http.GetAsync($"preapproval/{subscriptionId}");
			await EnsureSuccess(response);
			var json = await response.Content.ReadAsStringAsync();
			return Deserialize<MPPreapprovalResponseSimplified>(json);
		}

		public async Task CancelPreapproval(string subscriptionId)
		{
			await PutAsync($"preapproval/{subscriptionId}", new { status = "cancelled" });
		}

		/// <summary>Cobrança recorrente de uma assinatura (evento subscription_authorized_payment).</summary>
		public async Task<MPAuthorizedPaymentResponse> GetAuthorizedPayment(string authorizedPaymentId)
		{
			var response = await _http.GetAsync($"authorized_payments/{authorizedPaymentId}");
			await EnsureSuccess(response);
			var json = await response.Content.ReadAsStringAsync();
			return Deserialize<MPAuthorizedPaymentResponse>(json);
		}

		public async Task<MPPaymentResponse> GetPayment(string paymentId)
		{
			var response = await _http.GetAsync($"v1/payments/{paymentId}");
			await EnsureSuccess(response);
			var json = await response.Content.ReadAsStringAsync();
			return Deserialize<MPPaymentResponse>(json);
		}
		public async Task<MPPreapprovalPlanResponseSimplified> GetPreapprovalPlan(string id)
		{
			var response = await _http.GetAsync($"preapproval_plan/{id}");
			await EnsureSuccess(response);
			var json = await response.Content.ReadAsStringAsync();
			return Deserialize<MPPreapprovalPlanResponseSimplified>(json);
		}
		public async Task<MPPreapprovalResponseSimplified> UpdatePreapproval(string preapprovalId, object body)
		{
			var json = await PutAsync($"preapproval/{preapprovalId}", body);
			return Deserialize<MPPreapprovalResponseSimplified>(json);
		}

		private async Task<string> PostAsync<T>(string url, T body)
		{
			var json = SerializeInvariant(body);
			_logger.LogDebug("MP POST {Url} body: {Json}", url, json);
			var content = new StringContent(json, Encoding.UTF8, "application/json");
			var response = await _http.PostAsync(url, content);
			await EnsureSuccess(response);
			return await response.Content.ReadAsStringAsync();
		}

		private async Task<string> PutAsync<T>(string url, T body)
		{
			var json = SerializeInvariant(body);
			_logger.LogDebug("MP PUT {Url} body: {Json}", url, json);
			var content = new StringContent(json, Encoding.UTF8, "application/json");
			var response = await _http.PutAsync(url, content);
			await EnsureSuccess(response);
			return await response.Content.ReadAsStringAsync();
		}


		private static string SerializeInvariant<T>(T body)
		{
			var originalCulture = CultureInfo.CurrentCulture;
			try
			{
				CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
				return JsonSerializer.Serialize(body, JsonOptions);
			}
			finally
			{
				CultureInfo.CurrentCulture = originalCulture;
			}
		}

		private async Task EnsureSuccess(HttpResponseMessage response)
		{
			if (!response.IsSuccessStatusCode)
			{
				var body = await response.Content.ReadAsStringAsync();
				_logger.LogError("MP error {Status}: {Body}", (int)response.StatusCode, body);
				throw MercadoPagoException.From((int)response.StatusCode, body);
			}
		}

		private static T Deserialize<T>(string json)
		{
			return JsonSerializer.Deserialize<T>(json, JsonOptions)
					?? throw new Exception("Resposta inválida do Mercado Pago.");
		}

	}

	/// <summary>
	/// Erro devolvido pela API do Mercado Pago, com o corpo original (para log) e uma
	/// mensagem em português para mostrar ao usuário.
	/// </summary>
	public class MercadoPagoException : Exception
	{
		public int StatusCode { get; }
		public string RawBody { get; }
		public string MensagemUsuario { get; }

		private MercadoPagoException(int status, string raw, string usuario)
			: base($"Mercado Pago {status}: {raw}")
		{
			StatusCode = status;
			RawBody = raw;
			MensagemUsuario = usuario;
		}

		public static MercadoPagoException From(int status, string body)
		{
			string? message = null, cause = null;
			try
			{
				using var doc = JsonDocument.Parse(body);
				var root = doc.RootElement;
				if (root.TryGetProperty("message", out var m)) message = m.GetString();
				if (root.TryGetProperty("cause", out var c) && c.ValueKind == JsonValueKind.Array && c.GetArrayLength() > 0)
				{
					var first = c[0];
					if (first.TryGetProperty("description", out var d)) cause = d.GetString();
					else if (first.TryGetProperty("code", out var code)) cause = code.ToString();
				}
			}
			catch { /* corpo não-JSON */ }

			var texto = $"{message} {cause} {body}".ToLowerInvariant();
			string usuario;
			if (status == 401 || status == 403 || texto.Contains("invalid access token") || texto.Contains("unauthorized"))
				usuario = "A integração com o Mercado Pago não está configurada corretamente (credencial inválida). Avise o suporte.";
			else if (texto.Contains("both payer and collector must be real or test users") || texto.Contains("test user"))
				usuario = "A conta de pagamento está em modo de teste: use um usuário e um cartão de teste do Mercado Pago, ou ative as credenciais de produção.";
			else if (texto.Contains("payer_email") || texto.Contains("payer email") || texto.Contains("invalid email"))
				usuario = "E-mail do pagador inválido. Use o e-mail da sua conta do Mercado Pago.";
			else if (texto.Contains("card_token") || texto.Contains("card token") || texto.Contains("invalid token"))
				usuario = "Não foi possível validar os dados do cartão. Confira os dados e tente de novo.";
			else if (texto.Contains("rejected") || texto.Contains("insufficient"))
				usuario = "O pagamento foi recusado pelo cartão. Tente outro cartão ou fale com o seu banco.";
			else if (texto.Contains("back_url"))
				usuario = "Endereço de retorno do pagamento inválido na configuração. Avise o suporte.";
			else if (texto.Contains("transaction_amount"))
				usuario = "Valor do plano inválido para o Mercado Pago. Avise o suporte.";
			else if (status >= 500)
				usuario = "O Mercado Pago está instável no momento. Tente novamente em alguns minutos.";
			else
				usuario = string.IsNullOrWhiteSpace(message)
					? "O Mercado Pago recusou a operação. Tente novamente."
					: "O Mercado Pago recusou a operação: " + message;

			return new MercadoPagoException(status, body, usuario);
		}
	}

	public interface IMercadoPagoClient
	{
		Task<MPAuthorizedPaymentResponse> GetAuthorizedPayment(string authorizedPaymentId);
		Task<MPPreapprovalPlanResponse> CreatePreapprovalPlan(MPCreatePreapprovalPlanRequest req);
		Task<MPPreapprovalPlanResponse> UpdatePreapprovalPlan(string planId, MPUpdatePreapprovalPlanRequest req);
		Task<MPPreapprovalResponse> CreatePreapproval(MPCreatePreapprovalRequest req);
		Task<MPPreapprovalResponseSimplified> GetPreapproval(string subscriptionId);
		Task CancelPreapproval(string subscriptionId);
		Task<MPPaymentResponse> GetPayment(string paymentId);
		Task<MPPreapprovalPlanResponseSimplified> GetPreapprovalPlan(string id);
		Task<MPPreapprovalResponseSimplified> UpdatePreapproval(string preapprovalId, object body);

	}

	public class MPCreatePreapprovalPlanRequest
	{
		public string Reason { get; set; } = string.Empty;
		public MPAutoRecurring AutoRecurring { get; set; } = new();
		public string BackUrl { get; set; } = string.Empty;
	}

	public class MPUpdatePreapprovalPlanRequest
	{
		public string? Reason { get; set; }
		public MPAutoRecurring? AutoRecurring { get; set; }
		public string? Status { get; set; }
	}

	public class MPAutoRecurring
	{
		public int Frequency { get; set; }
		public string FrequencyType { get; set; } = "months";
		public decimal TransactionAmount { get; set; }
		public string CurrencyId { get; set; } = "BRL";
	}

	/// <summary>
	/// Assinatura SEM plano associado (POST /preapproval). Com <see cref="CardTokenId"/> e status
	/// "authorized" a cobrança começa na hora; sem cartão e com status "pending" o MP devolve um
	/// init_point para a pessoa pagar no site dele. (Assinatura COM preapproval_plan_id exige
	/// card_token_id já na criação — por isso o fluxo antigo dava erro ao pagar.)
	/// </summary>
	public class MPCreatePreapprovalRequest
	{
		public string? PreapprovalPlanId { get; set; }
		public string Reason { get; set; } = string.Empty;
		public string PayerEmail { get; set; } = string.Empty;
		public string? CardTokenId { get; set; }
		public MPAutoRecurring AutoRecurring { get; set; } = new();
		public string BackUrl { get; set; } = string.Empty;
		public string Status { get; set; } = "pending"; // "pending" ou "authorized"
		public string ExternalReference { get; set; } = string.Empty;
	}

	public class MPPreapprovalPlanResponse
	{
		public string Id { get; set; } = string.Empty;
		public string Status { get; set; } = string.Empty;
	}

	public class MPPreapprovalResponse
	{
		public string Id { get; set; } = string.Empty;
		public string Status { get; set; } = string.Empty;
		public string InitPoint { get; set; } = string.Empty;
		public DateTime? NextPaymentDate { get; set; }
	}

	public class MPAuthorizedPaymentResponse
	{
		public long Id { get; set; }
		public string? PreapprovalId { get; set; }
		public string Status { get; set; } = string.Empty;
		public decimal TransactionAmount { get; set; }
		public DateTime? DateCreated { get; set; }
		public DateTime? DebitDate { get; set; }
		public MPAuthorizedPaymentInfo? Payment { get; set; }
	}

	public class MPAuthorizedPaymentInfo
	{
		public long? Id { get; set; }
		public string? Status { get; set; }
		public string? StatusDetail { get; set; }
	}

	public class MPPaymentResponse
	{
		public long Id { get; set; }
		public string Status { get; set; } = string.Empty;
		public decimal TransactionAmount { get; set; }
		public DateTime? DateApproved { get; set; }
		public string? PreapprovalId { get; set; }
		/// <summary>Pagamentos de assinatura trazem o id dela em metadata.preapproval_id.</summary>
		public Dictionary<string, JsonElement>? Metadata { get; set; }
	}
	public class MPPreapprovalPlanResponseSimplified
	{
		[JsonPropertyName("id")]
		public string Id { get; set; } = string.Empty;

		[JsonPropertyName("reason")]
		public string Reason { get; set; } = string.Empty;

		[JsonPropertyName("auto_recurring")]
		public MPAutoRecurringPlanSimplified AutoRecurring { get; set; } = new();

		[JsonPropertyName("init_point")]
		public string InitPoint { get; set; } = string.Empty;

		[JsonPropertyName("date_created")]
		public DateTime DateCreated { get; set; }

		[JsonPropertyName("last_modified")]
		public DateTime LastModified { get; set; }

		[JsonPropertyName("status")]
		public string Status { get; set; } = string.Empty;
	}

	public class MPAutoRecurringPlanSimplified
	{
		[JsonPropertyName("frequency")]
		public int Frequency { get; set; }

		[JsonPropertyName("frequency_type")]
		public string FrequencyType { get; set; } = string.Empty;

		[JsonPropertyName("repetitions")]
		public int? Repetitions { get; set; }

		[JsonPropertyName("transaction_amount")]
		public decimal TransactionAmount { get; set; } = 0;

		[JsonPropertyName("currency_id")]
		public string CurrencyId { get; set; } = string.Empty;
	}
	public class MPPreapprovalResponseSimplified
	{
		[JsonPropertyName("id")]
		public string Id { get; set; } = string.Empty;

		[JsonPropertyName("preapproval_plan_id")]
		public string PreapprovalPlanId { get; set; } = string.Empty;

		[JsonPropertyName("reason")]
		public string Reason { get; set; } = string.Empty;

		[JsonPropertyName("external_reference")]
		public string ExternalReference { get; set; } = string.Empty;

		[JsonPropertyName("init_point")]
		public string InitPoint { get; set; } = string.Empty;

		[JsonPropertyName("auto_recurring")]
		public MPAutoRecurringSimplified AutoRecurring { get; set; } = new();

		[JsonPropertyName("payment_method_id")]
		public string PaymentMethodId { get; set; } = string.Empty;

		[JsonPropertyName("next_payment_date")]
		public DateTime? NextPaymentDate { get; set; }

		[JsonPropertyName("date_created")]
		public DateTime DateCreated { get; set; }

		[JsonPropertyName("last_modified")]
		public DateTime LastModified { get; set; }

		[JsonPropertyName("status")]
		public string Status { get; set; } = string.Empty;
	}

	public class MPAutoRecurringSimplified
	{
		[JsonPropertyName("frequency")]
		public int Frequency { get; set; }

		[JsonPropertyName("frequency_type")]
		public string FrequencyType { get; set; } = string.Empty;

		[JsonPropertyName("currency_id")]
		public string CurrencyId { get; set; } = string.Empty;

		[JsonPropertyName("transaction_amount")]
		public decimal TransactionAmount { get; set; }
	}
}