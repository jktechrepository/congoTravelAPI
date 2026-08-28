using Microsoft.AspNetCore.Http;

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

            return ValidateBytes(bytes, contentType, suggestedFileName);
        }

        /// <summary>Valide un fichier multipart (JPG/PNG, ≤ 1 Mo).</summary>
        public static async Task<(byte[] Bytes, string Extension, string ContentType)> ParseAndValidateFileAsync(
            IFormFile file,
            string? suggestedFileName = null,
            CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Le fichier photo est obligatoire.");

            if (file.Length > MaxImageBytes)
                throw new InvalidOperationException($"Fichier trop volumineux. Taille maximum : {MaxImageBytes / (1024 * 1024)} Mo.");

            await using var stream = file.OpenReadStream();
            using var memory = new MemoryStream(capacity: (int)Math.Min(file.Length, MaxImageBytes));
            await stream.CopyToAsync(memory, cancellationToken);
            var bytes = memory.ToArray();

            var fileName = string.IsNullOrWhiteSpace(suggestedFileName) ? file.FileName : suggestedFileName;
            string? contentType = null;
            if (!string.IsNullOrWhiteSpace(file.ContentType))
            {
                var ct = file.ContentType.Trim().ToLowerInvariant();
                if (ct is "image/jpeg" or "image/jpg")
                    contentType = "image/jpeg";
                else if (ct == "image/png")
                    contentType = "image/png";
                else if (ct is not "application/octet-stream")
                    throw new InvalidOperationException("Type d'image non autorisé. Formats acceptés : JPG, PNG.");
            }

            return ValidateBytes(bytes, contentType, fileName);
        }

        public static (byte[] Bytes, string Extension, string ContentType) ValidateBytes(
            byte[] bytes,
            string? contentType,
            string? suggestedFileName = null)
        {
            if (bytes == null || bytes.Length == 0)
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

            if (contentType is not ("image/jpeg" or "image/png"))
                throw new InvalidOperationException("Type d'image non autorisé. Formats acceptés : JPG, PNG.");

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
