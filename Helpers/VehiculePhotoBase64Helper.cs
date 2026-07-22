namespace CongoTravel.Helpers
{
    public static class VehiculePhotoBase64Helper
    {
        /// <summary>Taille maximale de l'image décodée (1 Mo).</summary>
        public const int MaxImageBytes = 1 * 1024 * 1024;

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png" };

        public static string ToBase64Payload(byte[] bytes) =>
            Convert.ToBase64String(bytes);

        public static (byte[] Bytes, string Extension, string ContentType) ParseAndValidate(
            string photoBase64,
            string? suggestedFileName = null)
        {
            if (string.IsNullOrWhiteSpace(photoBase64))
                throw new ArgumentException("La photo base64 est obligatoire.");

            var trimmed = photoBase64.Trim();
            string? contentType = null;
            var payload = trimmed;

            if (trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                var commaIndex = trimmed.IndexOf(',');
                if (commaIndex < 0)
                    throw new ArgumentException("Format data URL invalide.");

                var header = trimmed[..commaIndex];
                payload = trimmed[(commaIndex + 1)..];

                if (header.Contains("image/jpeg", StringComparison.OrdinalIgnoreCase) ||
                    header.Contains("image/jpg", StringComparison.OrdinalIgnoreCase))
                {
                    contentType = "image/jpeg";
                }
                else if (header.Contains("image/png", StringComparison.OrdinalIgnoreCase))
                {
                    contentType = "image/png";
                }
                else
                {
                    throw new InvalidOperationException("Type d'image non autorisé. Formats acceptés : JPG, PNG.");
                }
            }

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(payload);
            }
            catch (FormatException)
            {
                throw new ArgumentException("Contenu base64 invalide.");
            }

            if (bytes.Length == 0)
                throw new ArgumentException("La photo est vide.");

            if (bytes.Length > MaxImageBytes)
                throw new InvalidOperationException($"Fichier trop volumineux. Taille maximum : {MaxImageBytes / (1024 * 1024)} Mo.");

            var extension = ResolveExtension(contentType, suggestedFileName);
            if (!AllowedExtensions.Contains(extension))
                throw new InvalidOperationException("Type de fichier non autorisé. Formats acceptés : JPG, PNG.");

            contentType ??= extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };

            return (bytes, extension, contentType);
        }

        /// <summary>Construit une data URL utilisable directement dans un attribut src.</summary>
        public static string ToDataUrl(byte[] bytes, string contentType) =>
            $"data:{contentType};base64,{ToBase64Payload(bytes)}";

        private static string ResolveExtension(string? contentType, string? suggestedFileName)
        {
            if (!string.IsNullOrWhiteSpace(suggestedFileName))
            {
                var ext = Path.GetExtension(suggestedFileName).ToLowerInvariant();
                if (!string.IsNullOrEmpty(ext))
                    return ext;
            }

            return contentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                _ => ".jpg"
            };
        }
    }
}
