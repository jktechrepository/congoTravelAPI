using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CongoTravel.Helpers
{
    /// <summary>
    /// Sérialise <see cref="TimeOnly"/> en <c>HH:mm:ss</c>.
    /// En lecture : <c>HH:mm:ss</c>, <c>HH:mm</c>, fraction de secondes, ou extrait d'un ISO datetime.
    /// </summary>
    public sealed class TimeOnlyJsonConverter : JsonConverter<TimeOnly>
    {
        private const string TimeFormatHms = "HH:mm:ss";
        private const string TimeFormatHm = "HH:mm";

        private static readonly string[] ExactFormats =
        {
            TimeFormatHms,
            TimeFormatHm,
            "HH:mm:ss.FFFFFFF",
            "HH:mm:ss.fff"
        };

        public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                throw new JsonException("La valeur TimeOnly ne peut pas être null.");
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var value = reader.GetString();
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new JsonException("La valeur TimeOnly ne peut pas être vide.");
                }

                value = value.Trim();

                if (TimeOnly.TryParseExact(
                        value,
                        ExactFormats,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var exact))
                {
                    return exact;
                }

                // Swagger / clients parfois envoient un ISO datetime : extraire l'heure.
                if (DateTimeOffset.TryParse(
                        value,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind,
                        out var dto))
                {
                    return TimeOnly.FromDateTime(dto.DateTime);
                }

                if (DateTime.TryParse(
                        value,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var dt))
                {
                    return TimeOnly.FromDateTime(dt);
                }

                if (TimeOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                {
                    return parsed;
                }

                throw new JsonException(
                    $"Format d'heure invalide '{value}'. Utilisez '{TimeFormatHms}' ou '{TimeFormatHm}' (ex. 12:00:00).");
            }

            throw new JsonException(
                $"Token JSON inattendu pour TimeOnly ({reader.TokenType}). Attendu: chaîne '{TimeFormatHms}'.");
        }

        public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(TimeFormatHms, CultureInfo.InvariantCulture));
        }
    }

    /// <summary>Version nullable du convertisseur <see cref="TimeOnly"/>.</summary>
    public sealed class NullableTimeOnlyJsonConverter : JsonConverter<TimeOnly?>
    {
        private readonly TimeOnlyJsonConverter _inner = new();

        public override TimeOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            return _inner.Read(ref reader, typeof(TimeOnly), options);
        }

        public override void Write(Utf8JsonWriter writer, TimeOnly? value, JsonSerializerOptions options)
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
