using CongoTravel.Helpers;
using CongoTravel.Helpers.Evenement;
using CongoTravel.Helpers.Restaurant;
using CongoTravel.Helpers.SiteTouristique;
using CongoTravel.Models;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;
using CongoTravel.Models.Restaurant;
using CongoTravel.Models.Restaurant.Enums;
using CongoTravel.Models.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Services
{
    /// <summary>Contexte module-agnostique pour déclencher un reversement auto post-paiement.</summary>
    public sealed class ReversementAutomatiqueContext
    {
        public string ModulePaiement { get; init; } = string.Empty;
        public int IdPaiementSource { get; init; }
        public int? IdReservationSource { get; init; }
        public int? IdSite { get; init; }
        public int IdSociete { get; init; }
        public int IdUtilisateur { get; init; }
        public decimal MontantBrut { get; init; }
        public string CodeDevisePaiement { get; init; } = "CDF";
        public DateTime DateReference { get; init; } = DateTime.UtcNow;
        public bool EstPaiementElectronique { get; init; }

        /// <summary>Override bénéficiaire PayOut (module Événement : MM organisateur).</summary>
        public string? NumeroMobileMoneyBeneficiaireOverride { get; init; }

        /// <summary>Gate reversement session ; null = pas de gate session (Transport, etc.).</summary>
        public bool? AutoReversementSessionAutorise { get; init; }

        /// <summary>Rétrocompatibilité Transport : peupler aussi <c>IdPaiement</c> / <c>IdReservation</c>.</summary>
        public int? IdPaiementTransport { get; init; }
        public int? IdReservationTransport { get; init; }

        public static ReversementAutomatiqueContext FromTransport(
            Paiement paiement,
            Reservation reservation) =>
            new()
            {
                ModulePaiement = ReversementModulePaiement.Transport,
                IdPaiementSource = paiement.IdPaiement,
                IdReservationSource = reservation.IdReservation,
                IdPaiementTransport = paiement.IdPaiement,
                IdReservationTransport = reservation.IdReservation,
                IdSite = reservation.IdSite ?? paiement.IdSite,
                IdSociete = paiement.IdSociete,
                IdUtilisateur = paiement.IdUtilisateur > 0
                    ? paiement.IdUtilisateur
                    : reservation.IdUtilisateur,
                MontantBrut = paiement.MontantPaye ?? 0m,
                CodeDevisePaiement = paiement.CodeDevisePaiement ?? "CDF",
                DateReference = paiement.DatePaiement == default
                    ? DateTime.UtcNow
                    : paiement.DatePaiement,
                EstPaiementElectronique = MethodePaiementHelper.IsElectronic(paiement.MethodePaiement)
            };

        public static ReversementAutomatiqueContext FromEvenement(
            EvenementPayment payment,
            EvenementReservation reservation,
            EvenementSession? session = null) =>
            new()
            {
                ModulePaiement = ReversementModulePaiement.Evenement,
                IdPaiementSource = payment.IdEvenementPayment,
                IdReservationSource = reservation.IdEvenementReservation,
                IdSite = reservation.IdSite ?? payment.IdSite,
                IdSociete = reservation.IdSociete,
                IdUtilisateur = reservation.IdUtilisateur ?? 0,
                MontantBrut = payment.Montant,
                CodeDevisePaiement = payment.CodeDevise,
                DateReference = payment.DateModification ?? payment.DateCreation,
                EstPaiementElectronique =
                    string.Equals(payment.Provider, EvenementFlexPayConstants.Provider, StringComparison.OrdinalIgnoreCase)
                    && payment.Status == EvenementPaymentStatus.SUCCEEDED,
                NumeroMobileMoneyBeneficiaireOverride =
                    EvenementSessionOrganizerPayoutHelper.TryResolveNormalizedMobileMoney(session),
                AutoReversementSessionAutorise = session?.AutoReversementOrganisateur
            };

        public static ReversementAutomatiqueContext FromRestaurant(
            RestaurantPayment payment,
            RestaurantReservation reservation) =>
            new()
            {
                ModulePaiement = ReversementModulePaiement.Restaurant,
                IdPaiementSource = payment.IdRestaurantPayment,
                IdReservationSource = reservation.IdRestaurantReservation,
                IdSite = reservation.IdSite ?? payment.IdSite,
                IdSociete = reservation.IdSociete,
                IdUtilisateur = reservation.IdUtilisateur ?? 0,
                MontantBrut = payment.Montant,
                CodeDevisePaiement = payment.CodeDevise,
                DateReference = payment.DateModification ?? payment.DateCreation,
                EstPaiementElectronique =
                    string.Equals(payment.Provider, RestaurantFlexPayConstants.Provider, StringComparison.OrdinalIgnoreCase)
                    && payment.Status == RestaurantPaymentStatus.SUCCEEDED
            };

        public static ReversementAutomatiqueContext FromSiteTouristique(
            SiteTouristiquePayment payment,
            SiteTouristiqueReservation reservation) =>
            new()
            {
                ModulePaiement = ReversementModulePaiement.SiteTouristique,
                IdPaiementSource = payment.IdSiteTouristiquePayment,
                IdReservationSource = reservation.IdSiteTouristiqueReservation,
                IdSite = reservation.IdSite ?? payment.IdSite,
                IdSociete = reservation.IdSociete,
                IdUtilisateur = reservation.IdUtilisateur ?? 0,
                MontantBrut = payment.Montant,
                CodeDevisePaiement = payment.CodeDevise,
                DateReference = payment.DateModification ?? payment.DateCreation,
                EstPaiementElectronique =
                    string.Equals(payment.Provider, SiteTouristiqueFlexPayConstants.Provider, StringComparison.OrdinalIgnoreCase)
                    && payment.Status == SiteTouristiquePaymentStatus.SUCCEEDED
            };
    }
}
