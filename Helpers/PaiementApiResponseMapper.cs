using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Pagination;

namespace CongoTravel.Helpers
{
    /// <summary>Projection des paiements pour les endpoints GET <c>/api/Paiement</c>.</summary>
    public static class PaiementApiResponseMapper
    {
        public static PaiementResponseDto Map(Paiement paiement) =>
            new()
            {
                IdPaiement = paiement.IdPaiement,
                MontantAPaye = paiement.MontantAPaye,
                MontantPaye = paiement.MontantPaye,
                ResteAPaye = paiement.ResteAPaye,
                ResteAPayeCalcule = paiement.ResteAPayeCalcule,
                MethodePaiement = paiement.MethodePaiement,
                ReferenceTransaction = paiement.ReferenceTransaction,
                Statut = paiement.Statut,
                DateCreation = paiement.DateCreation,
                DateModification = paiement.DateModification,
                EstComplet = paiement.EstComplet,
                EstPartiel = paiement.EstPartiel,
                IdUtilisateur = paiement.IdUtilisateur,
                NomUtilisateur = paiement.Utilisateur?.NomComplet,
                IdReservation = paiement.IdReservation,
                CodeReservation = paiement.IdReservation.HasValue
                    ? $"RES-{paiement.IdReservation.Value:D6}"
                    : null,
                IdSociete = paiement.IdSociete,
                NomSociete = paiement.Societe?.Nom,
                IdClient = paiement.Reservation?.IdClient,
                NomClient = paiement.Reservation?.Client?.NomClient,
                Origine = paiement.Origine,
                OrigineGroupe = OrigineOperationGroupeHelper.ToGroupe(paiement.Origine)
            };

        public static List<PaiementResponseDto> Map(IEnumerable<Paiement> paiements) =>
            paiements.Select(Map).ToList();

        public static PagedResult<PaiementResponseDto> Map(PagedResult<Paiement> paged) =>
            new(
                Map(paged.Data),
                paged.TotalCount,
                paged.PageNumber,
                paged.PageSize);
    }
}
