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
                Status = lieu.Status.ToString(),
                DateCreation = lieu.DateCreation,
                DateModification = lieu.DateModification
            };

        public static SiteTouristiqueLieuResponseDto ToResponseDto(SiteTouristiqueLieu lieu) =>
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
                Status = lieu.Status.ToString(),
                DateCreation = lieu.DateCreation,
                DateModification = lieu.DateModification,
                JourneesCount = lieu.Journees?.Count ?? 0
            };
    }
}
