using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CongoTravel.Helpers
{
    /// <summary>
    /// Sérialise un TimeSpan au format "HH:mm:ss".
    /// Accepte aussi "c" en lecture pour compatibilité.
    /// </summary>
    public sealed class TimeSpanHmsJsonConverter : JsonConverter<TimeSpan>
    {
        private static readonly string[] AcceptedFormats = { @"hh\:mm\:ss", "c" };

        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Le format attendu pour TimeSpan est une chaîne 'HH:mm:ss'.");
            }

            var value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new JsonException("La valeur TimeSpan ne peut pas être vide.");
            }

            if (TimeSpan.TryParseExact(value, AcceptedFormats, CultureInfo.InvariantCulture, out var result) ||
                TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out result))
            {
                return result;
            }

            throw new JsonException("Format invalide. Utilisez 'HH:mm:ss'.");
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture));
        }
    }

    /// <summary>
    /// Version nullable du convertisseur TimeSpan.
    /// </summary>
    public sealed class NullableTimeSpanHmsJsonConverter : JsonConverter<TimeSpan?>
    {
        private readonly TimeSpanHmsJsonConverter _inner = new();

        public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            return _inner.Read(ref reader, typeof(TimeSpan), options);
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options)
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

