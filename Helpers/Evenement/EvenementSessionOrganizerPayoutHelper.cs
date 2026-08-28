using CongoTravel.Helpers;
using CongoTravel.Models.Evenement;

namespace CongoTravel.Helpers.Evenement
{
    /// <summary>Résolution PayOut organisateur vs site pour le module événement.</summary>
    public static class EvenementSessionOrganizerPayoutHelper
    {
        /// <summary>Numéro MM normalisé pour override PayOut, ou null (fallback site).</summary>
        public static string? TryResolveNormalizedMobileMoney(EvenementSession? session)
        {
            if (session == null || string.IsNullOrWhiteSpace(session.NumeroMobileMoneyOrganisateur))
                return null;

            if (MobileMoneyPhoneHelper.TryNormalize(
                    session.NumeroMobileMoneyOrganisateur,
                    out var normalized,
                    out _))
            {
                return normalized;
            }

            return null;
        }

        /// <summary>Rejette si un MM organisateur est renseigné mais invalide (publish).</summary>
        public static void ValidateMobileMoneyForPublish(EvenementSession session)
        {
            if (string.IsNullOrWhiteSpace(session.NumeroMobileMoneyOrganisateur))
                return;

            if (!MobileMoneyPhoneHelper.TryNormalize(
                    session.NumeroMobileMoneyOrganisateur,
                    out _,
                    out var error))
            {
                throw new InvalidOperationException(
                    error ?? "NumeroMobileMoneyOrganisateur invalide pour cette session.");
            }
        }

        public static string? NormalizeOptionalMobileMoney(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            if (!MobileMoneyPhoneHelper.TryNormalize(raw.Trim(), out var normalized, out var error))
                throw new InvalidOperationException(error ?? "NumeroMobileMoneyOrganisateur invalide.");

            return normalized;
        }
    }
}
