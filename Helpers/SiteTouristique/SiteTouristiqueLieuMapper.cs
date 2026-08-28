using CongoTravel.Helpers;
using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique;

namespace CongoTravel.Helpers.SiteTouristique
{
    public static class SiteTouristiqueLieuMapper
    {
        public static SiteTouristiqueLieuListItemDto ToListItemDto(
            SiteTouristiqueLieu lieu,
            bool includePhotoBase64 = false) =>
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
                PhotoCouverture = ResolveCoverPhoto(lieu, includePhotoBase64)
            };

        public static SiteTouristiqueLieuResponseDto ToResponseDto(
            SiteTouristiqueLieu lieu,
            bool includePhotoBase64 = false)
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
                PhotoCouverture = ResolveCoverPhoto(lieu, includePhotoBase64)
            };

            if (lieu.Photos != null && lieu.Photos.Count > 0)
            {
                dto.Photos = lieu.Photos
                    .Where(p => p.Statut)
                    .OrderBy(p => p.Ordre)
                    .Select(p => ToPhotoDto(p, includePhotoBase64))
                    .ToList();
            }

            return dto;
        }

        public static SiteTouristiqueLieuPhotoDto ToPhotoDto(
            SiteTouristiqueLieuPhoto photo,
            bool includePhotoBase64 = false)
        {
            return new SiteTouristiqueLieuPhotoDto
            {
                IdSiteTouristiqueLieuPhoto = photo.IdSiteTouristiqueLieuPhoto,
                IdSiteTouristique = photo.IdSiteTouristique,
                PhotoUrl = CongoTravelPhotoUrlBuilder.ForSiteTouristiqueLieu(
                    photo.IdSiteTouristique,
                    photo.IdSiteTouristiqueLieuPhoto),
                PhotoBase64 = PhotoContentHelper.EncodeBase64IfRequested(
                    photo.PhotoData,
                    photo.TypeMIME,
                    includePhotoBase64),
                Ordre = photo.Ordre,
                OriginalFileName = photo.OriginalFileName,
                TypeMIME = photo.TypeMIME,
                FileSize = photo.FileSize,
                Statut = photo.Statut,
                DateCreation = photo.DateCreation,
                DateModification = photo.DateModification
            };
        }

        private static SiteTouristiqueLieuPhotoDto? ResolveCoverPhoto(
            SiteTouristiqueLieu lieu,
            bool includePhotoBase64 = false)
        {
            var cover = lieu.Photos?
                .Where(p => p.Statut)
                .OrderBy(p => p.Ordre)
                .FirstOrDefault();

            return cover == null ? null : ToPhotoDto(cover, includePhotoBase64);
        }
    }
}
