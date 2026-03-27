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

        public async Task<MPPreapprovalResponse> GetPreapproval(string subscriptionId)
        {
            var response = await _http.GetAsync($"preapproval/{subscriptionId}");
            await EnsureSuccess(response);
            var json = await response.Content.ReadAsStringAsync();
            return Deserialize<MPPreapprovalResponse>(json);
        }

        public async Task CancelPreapproval(string subscriptionId)
        {
            await PutAsync($"preapproval/{subscriptionId}", new { status = "cancelled" });
        }

        public async Task<MPPaymentResponse> GetPayment(string paymentId)
        {
            var response = await _http.GetAsync($"v1/payments/{paymentId}");
            await EnsureSuccess(response);
            var json = await response.Content.ReadAsStringAsync();
            return Deserialize<MPPaymentResponse>(json);
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
                throw new Exception($"Mercado Pago error {(int)response.StatusCode}: {body}");
            }
        }

        private static T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions)
                ?? throw new Exception("Resposta inválida do Mercado Pago.");
        }
    }

    public interface IMercadoPagoClient
    {
        Task<MPPreapprovalPlanResponse> CreatePreapprovalPlan(MPCreatePreapprovalPlanRequest req);
        Task<MPPreapprovalPlanResponse> UpdatePreapprovalPlan(string planId, MPUpdatePreapprovalPlanRequest req);
        Task<MPPreapprovalResponse> CreatePreapproval(MPCreatePreapprovalRequest req);
        Task<MPPreapprovalResponse> GetPreapproval(string subscriptionId);
        Task CancelPreapproval(string subscriptionId);
        Task<MPPaymentResponse> GetPayment(string paymentId);
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

    public class MPCreatePreapprovalRequest
    {
        public string PreapprovalPlanId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string PayerEmail { get; set; } = string.Empty;
        public string BackUrl { get; set; } = string.Empty;
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
    }

    public class MPPaymentResponse
    {
        public long Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TransactionAmount { get; set; }
        public DateTime? DateApproved { get; set; }
        public string? PreapprovalId { get; set; }
    }
}