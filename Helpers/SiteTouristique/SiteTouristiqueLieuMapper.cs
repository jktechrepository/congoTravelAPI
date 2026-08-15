using CongoTravel.Helpers;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique;

namespace CongoTravel.Helpers.SiteTouristique
{
    public static class SiteTouristiqueLieuMapper
    {
        public static SiteTouristiqueLieuListItemDto ToListItemDto(SiteTouristiqueLieu lieu) =>
            new()
            {
                IdSiteTouristique = lieu.IdSiteTouristique,
                IdSociete = lieu.IdSociete,
                NomSociete = lieu.Societe?.Nom,
                IdSite = lieu.IdSite,
                NomSite = lieu.Site?.NomSite,
                CodeLieu = lieu.CodeLieu,
                Nom = lieu.Nom,
                Description = lieu.Description,
                Province = lieu.Province,
                Ville = lieu.Ville,
                Adresse = lieu.Adresse,
                Telephone = lieu.Telephone,
                HeureOuverture = lieu.HeureOuverture,
                HeureFermeture = lieu.HeureFermeture,
                JourOuverture = lieu.JourOuverture,
                Status = lieu.Status.ToString(),
                DateCreation = lieu.DateCreation,
                DateModification = lieu.DateModification,
                PhotoCouverture = ResolveCoverPhoto(lieu)
            };

        public static SiteTouristiqueLieuResponseDto ToResponseDto(SiteTouristiqueLieu lieu)
        {
            var dto = new SiteTouristiqueLieuResponseDto
            {
                IdSiteTouristique = lieu.IdSiteTouristique,
                IdSociete = lieu.IdSociete,
                NomSociete = lieu.Societe?.Nom,
                IdSite = lieu.IdSite,
                NomSite = lieu.Site?.NomSite,
                CodeLieu = lieu.CodeLieu,
                Nom = lieu.Nom,
                Description = lieu.Description,
                Province = lieu.Province,
                Ville = lieu.Ville,
                Adresse = lieu.Adresse,
                Telephone = lieu.Telephone,
                HeureOuverture = lieu.HeureOuverture,
                HeureFermeture = lieu.HeureFermeture,
                JourOuverture = lieu.JourOuverture,
                Status = lieu.Status.ToString(),
                DateCreation = lieu.DateCreation,
                DateModification = lieu.DateModification,
                JourneesCount = lieu.Journees?.Count ?? 0,
                PhotoCouverture = ResolveCoverPhoto(lieu)
            };

            if (lieu.Photos != null && lieu.Photos.Count > 0)
            {
                dto.Photos = lieu.Photos
                    .Where(p => p.Statut)
                    .OrderBy(p => p.Ordre)
                    .Select(ToPhotoDto)
                    .ToList();
            }

            return dto;
        }

        public static SiteTouristiqueLieuPhotoDto ToPhotoDto(SiteTouristiqueLieuPhoto photo)
        {
            var contentType = string.IsNullOrWhiteSpace(photo.TypeMIME)
                ? "image/jpeg"
                : photo.TypeMIME!;

            return new SiteTouristiqueLieuPhotoDto
            {
                IdSiteTouristiqueLieuPhoto = photo.IdSiteTouristiqueLieuPhoto,
                IdSiteTouristique = photo.IdSiteTouristique,
                PhotoBase64 = photo.PhotoData.Length > 0
                    ? VehiculePhotoBase64Helper.ToDataUrl(photo.PhotoData, contentType)
                    : string.Empty,
                Ordre = photo.Ordre,
                OriginalFileName = photo.OriginalFileName,
                TypeMIME = photo.TypeMIME,
                FileSize = photo.FileSize,
                Statut = photo.Statut,
                DateCreation = photo.DateCreation,
                DateModification = photo.DateModification
            };
        }

        private static SiteTouristiqueLieuPhotoDto? ResolveCoverPhoto(SiteTouristiqueLieu lieu)
        {
            var cover = lieu.Photos?
                .Where(p => p.Statut)
                .OrderBy(p => p.Ordre)
                .FirstOrDefault();

            return cover == null ? null : ToPhotoDto(cover);
        }
    }
}
