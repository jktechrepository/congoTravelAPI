using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement;

namespace CongoTravel.Helpers.Evenement
{
    public static class EvenementClasseMapper
    {
        public static EvenementClasseResponseDto ToResponseDto(EvenementClasse classe) =>
            new()
            {
                IdEvenementClasse = classe.IdEvenementClasse,
                IdSociete = classe.IdSociete,
                CodeClasse = classe.CodeClasse,
                Libelle = classe.Libelle,
                Description = classe.Description,
                Statut = classe.Statut,
                DateCreation = classe.DateCreation
            };
    }
}
