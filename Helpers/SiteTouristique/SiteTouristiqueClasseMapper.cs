using CongoTravel.Models.DTOs.SiteTouristique;
using CongoTravel.Models.SiteTouristique;

namespace CongoTravel.Helpers.SiteTouristique
{
    public static class SiteTouristiqueClasseMapper
    {
        public static SiteTouristiqueClasseResponseDto ToResponseDto(SiteTouristiqueClasse classe) =>
            new()
            {
                IdSiteTouristiqueClasse = classe.IdSiteTouristiqueClasse,
                IdSociete = classe.IdSociete,
                Code = classe.Code,
                Libelle = classe.Libelle,
                Description = classe.Description,
                Actif = classe.Actif,
                DateCreation = classe.DateCreation
            };
    }
}
