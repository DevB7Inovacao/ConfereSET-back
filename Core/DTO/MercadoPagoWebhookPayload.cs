using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core.DTO
{
    /// <summary>Notificação (webhook) do Mercado Pago.</summary>
    public class MercadoPagoWebhookPayload
    {
        // O MP manda "id" e "data.id" ora como número, ora como texto — aceitamos os dois.
        [JsonPropertyName("id")]
        [JsonConverter(typeof(TextoOuNumeroConverter))]
        public string? Id { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("data")]
        public MercadoPagoWebhookData? Data { get; set; }
    }

    public class MercadoPagoWebhookData
    {
        [JsonPropertyName("id")]
        [JsonConverter(typeof(TextoOuNumeroConverter))]
        public string? Id { get; set; }
    }

    /// <summary>Lê string ou número JSON como string.</summary>
    public class TextoOuNumeroConverter : JsonConverter<string?>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.Number => reader.TryGetInt64(out var l) ? l.ToString() : reader.GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture),
                JsonTokenType.Null => null,
                _ => JsonDocument.ParseValue(ref reader).RootElement.ToString(),
            };
        }

        public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        {
            if (value == null) writer.WriteNullValue();
            else writer.WriteStringValue(value);
        }
    }
}
