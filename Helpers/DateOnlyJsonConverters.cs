using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CongoTravel.Helpers
{
    /// <summary>
    /// Sérialise <see cref="DateOnly"/> en <c>yyyy-MM-dd</c>.
    /// En lecture : date pure, ISO datetime, ou parse invariant.
    /// </summary>
    public sealed class DateOnlyJsonConverter : JsonConverter<DateOnly>
    {
        private const string DateFormat = "yyyy-MM-dd";

        public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                throw new JsonException("La valeur DateOnly ne peut pas être null.");
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var value = reader.GetString();
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new JsonException("La valeur DateOnly ne peut pas être vide.");
                }

                value = value.Trim();

                if (DateOnly.TryParseExact(
                        value,
                        DateFormat,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var exact))
                {
                    return exact;
                }

                if (DateTimeOffset.TryParse(
                        value,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind,
                        out var dto))
                {
                    return DateOnly.FromDateTime(dto.UtcDateTime);
                }

                if (DateTime.TryParse(
                        value,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                        out var dt))
                {
                    return DateOnly.FromDateTime(dt);
                }

                if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                {
                    return parsed;
                }

                throw new JsonException(
                    $"Format de date invalide '{value}'. Utilisez '{DateFormat}' (ex. 2026-09-15).");
            }

            // Swagger / clients parfois envoient un nombre (jours depuis epoch) — non supporté.
            throw new JsonException(
                $"Token JSON inattendu pour DateOnly ({reader.TokenType}). Attendu: chaîne '{DateFormat}'.");
        }

        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(DateFormat, CultureInfo.InvariantCulture));
        }
    }

    /// <summary>Version nullable du convertisseur <see cref="DateOnly"/>.</summary>
    public sealed class NullableDateOnlyJsonConverter : JsonConverter<DateOnly?>
    {
        private readonly DateOnlyJsonConverter _inner = new();

        public override DateOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            return _inner.Read(ref reader, typeof(DateOnly), options);
        }

        public override void Write(Utf8JsonWriter writer, DateOnly? value, JsonSerializerOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNullValue();
                return;
            }

            _inner.Write(writer, value.Value, options);
        }
    }
}
