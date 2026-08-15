using System.Text.Json.Serialization;

namespace CongoTravel.Models.DTOs.SiteTouristique
{
    public class SiteTouristiqueLieuPhotoDto
    {
        public int IdSiteTouristiqueLieuPhoto { get; set; }

        public int IdSiteTouristique { get; set; }

        public string PhotoBase64 { get; set; } = string.Empty;

        public int Ordre { get; set; }

        public string? OriginalFileName { get; set; }

        public string? TypeMIME { get; set; }

        public long? FileSize { get; set; }

        public bool Statut { get; set; }

        public DateTime DateCreation { get; set; }

        public DateTime? DateModification { get; set; }
    }

    public class AddSiteTouristiqueLieuPhotoDto
    {
        /// <summary>Image encodée en base64 (avec ou sans préfixe data:image/...;base64,).</summary>
        public string PhotoBase64 { get; set; } = string.Empty;

        [JsonPropertyName("filePath")]
        public string? FilePath
        {
            set => CoalesceBase64(value);
        }

        [JsonPropertyName("photo")]
        public string? Photo
        {
            set => CoalesceBase64(value);
        }

        [JsonPropertyName("image")]
        public string? Image
        {
            set => CoalesceBase64(value);
        }

        [JsonPropertyName("base64")]
        public string? Base64
        {
            set => CoalesceBase64(value);
        }

        /// <summary>Position 1 à 3. Si omis, première position libre.</summary>
        public int? Ordre { get; set; }

        /// <summary>Nom de fichier suggéré (ex: photo.jpg) pour la validation du type.</summary>
        public string? FileName { get; set; }

        private void CoalesceBase64(string? value)
        {
            if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(PhotoBase64))
                PhotoBase64 = value.Trim();
        }
    }

    public class UpdateSiteTouristiqueLieuPhotoOrdreDto
    {
        public int Ordre { get; set; }
    }
}
