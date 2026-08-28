using System.Text.Json.Serialization;

namespace CongoTravel.Models.DTOs.Hotel
{
    public class HotelPhotoDto
    {
        public int IdHotelPhoto { get; set; }
        public int IdHotel { get; set; }
        public string? PhotoUrl { get; set; }
        public string PhotoBase64 { get; set; } = string.Empty;
        public int Ordre { get; set; }
        public string? OriginalFileName { get; set; }
        public string? TypeMIME { get; set; }
        public long? FileSize { get; set; }
        public bool Statut { get; set; }
        public DateTime DateCreation { get; set; }
        public DateTime? DateModification { get; set; }
    }

    public class AddHotelPhotoDto
    {
        public string PhotoBase64 { get; set; } = string.Empty;
        [JsonPropertyName("filePath")]
        public string? FilePath { set => CoalesceBase64(value); }
        [JsonPropertyName("photo")]
        public string? Photo { set => CoalesceBase64(value); }
        [JsonPropertyName("image")]
        public string? Image { set => CoalesceBase64(value); }
        [JsonPropertyName("base64")]
        public string? Base64 { set => CoalesceBase64(value); }
        public int? Ordre { get; set; }
        public string? FileName { get; set; }

        private void CoalesceBase64(string? value)
        {
            if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(PhotoBase64))
                PhotoBase64 = value.Trim();
        }
    }

    public class UpdateHotelPhotoOrdreDto
    {
        public int Ordre { get; set; }
    }
}
