using CongoTravel.Models;
using CongoTravel.Models.DTOs.Reservation;
using CongoTravel.Helpers;

namespace CongoTravel.Helpers
{
    public static class PaiementResponseMapper
    {
        public static PaiementResponseDto Map(Paiement paiement) =>
            new()
            {
                IdPaiement = paiement.IdPaiement,
                MontantAPaye = paiement.MontantAPaye,
                MontantPaye = paiement.MontantPaye ?? 0,
                ResteAPaye = paiement.ResteAPaye,
                MethodePaiement = paiement.MethodePaiement,
                ReferenceTransaction = paiement.ReferenceTransaction,
                Statut = paiement.Statut,
                DateCreation = paiement.DateCreation,
                DateEmissionBillet = paiement.DateEmissionBillet,
                IdBilletEmis = paiement.IdBilletEmis,
                IdReservation = paiement.IdReservation,
                IdSociete = paiement.IdSociete,
                IdSite = paiement.IdSite,
                EstComplet = paiement.EstComplet,
                EstPartiel = paiement.EstPartiel,
                Origine = paiement.Origine,
                OrigineGroupe = OrigineOperationGroupeHelper.ToGroupe(paiement.Origine)
            };
    }
}
