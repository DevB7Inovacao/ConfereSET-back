using System.Text.Json.Serialization;

namespace Core.DTO
{
    public class MercadoPagoWebhookPayload
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("data")]
        public MercadoPagoWebhookData? Data { get; set; }
    }

    public class MercadoPagoWebhookData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }
}