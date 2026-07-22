using System.Text.Json;
using System.Text.Json.Serialization;
using CongoTravel.Models.DTOs;

namespace CongoTravel.Helpers
{
    /// <summary>
    /// Accepte <c>photos</c> comme tableau d'objets <see cref="AddPhotoVehiculeDto"/> ou de chaînes base64.
    /// </summary>
    public sealed class AddPhotoVehiculeDtoListJsonConverter : JsonConverter<List<AddPhotoVehiculeDto>?>
    {
        public override List<AddPhotoVehiculeDto>? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("Le champ photos doit être un tableau.");

            var list = new List<AddPhotoVehiculeDto>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    break;

                if (reader.TokenType == JsonTokenType.String)
                {
                    var s = reader.GetString();
                    if (!string.IsNullOrWhiteSpace(s))
                        list.Add(new AddPhotoVehiculeDto { PhotoBase64 = s });
                    continue;
                }

                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    var dto = JsonSerializer.Deserialize<AddPhotoVehiculeDto>(ref reader, options);
                    if (dto != null)
                        list.Add(dto);
                    continue;
                }

                throw new JsonException(
                    $"Élément photos invalide (token {reader.TokenType}). Attendu : objet ou chaîne base64.");
            }

            return list.Count == 0 ? null : list;
        }

        public override void Write(
            Utf8JsonWriter writer,
            List<AddPhotoVehiculeDto>? value,
            JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, value, options);
    }
}
