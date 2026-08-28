using CongoTravel.Models;
using CongoTravel.Models.DTOs;

namespace CongoTravel.Helpers
{
    /// <summary>Résultat binaire pour GET .../photos/{id}/content.</summary>
    public sealed class PhotoContentPayload
    {
        public byte[] Content { get; init; } = Array.Empty<byte>();
        public string ContentType { get; init; } = "image/jpeg";
        public string? FileName { get; init; }
    }

    public static class PhotoContentHelper
    {
        public static string ResolveContentType(string? typeMime) =>
            string.IsNullOrWhiteSpace(typeMime) ? "image/jpeg" : typeMime!;

        public static string EncodeBase64IfRequested(
            byte[]? photoData,
            string? typeMime,
            bool includePhotoBase64)
        {
            if (!includePhotoBase64 || photoData == null || photoData.Length == 0)
                return string.Empty;

            return VehiculePhotoBase64Helper.ToDataUrl(photoData, ResolveContentType(typeMime));
        }

        public static void ApplyBase64(PhotoVehiculeDto dto, PhotoVehicule photo, bool includePhotoBase64)
        {
            dto.PhotoBase64 = EncodeBase64IfRequested(photo.PhotoData, photo.TypeMIME, includePhotoBase64);
        }
    }
}
