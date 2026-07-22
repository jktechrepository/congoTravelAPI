using System;
using System.Collections.Generic;

namespace CongoTravel.Helpers
{
    /// <summary>
    /// Codes canoniques et normalisation des méthodes de paiement (non-régression CASH / FlexPay).
    /// </summary>
    public static class MethodePaiementHelper
    {
        public const string Cash = "CASH";
        public const string MobileMoney = "MOBILE_MONEY";
        public const string CarteBancaire = "CARTE_BANCAIRE";

        private static readonly HashSet<string> ElectronicCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            MobileMoney,
            CarteBancaire
        };

        private static readonly HashSet<string> CashAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            Cash,
            "ESPECES",
            "ESPECE",
            "ESPÈCES",
            "ESPECES",
            "CASH",
            "LIQUIDE"
        };

        /// <summary>True si la méthode nécessite FlexPay (MM ou carte).</summary>
        public static bool IsElectronic(string? methodePaiement)
        {
            var code = ToCanonicalCode(methodePaiement);
            return code != null && ElectronicCodes.Contains(code);
        }

        /// <summary>True si paiement guichet / espèces (y compris libellés legacy).</summary>
        public static bool IsCash(string? methodePaiement)
        {
            if (string.IsNullOrWhiteSpace(methodePaiement))
                return false;

            var trimmed = methodePaiement.Trim();
            if (CashAliases.Contains(trimmed))
                return true;

            var lower = trimmed.ToLowerInvariant();
            return lower.Contains("espèce") || lower.Contains("espece") || lower == "cash";
        }

        /// <summary>
        /// Code canonique pour stockage : CASH, MOBILE_MONEY, CARTE_BANCAIRE ou texte trimé si inconnu.
        /// </summary>
        public static string NormalizeForStorage(string? methodePaiement)
        {
            var canonical = ToCanonicalCode(methodePaiement);
            if (canonical != null)
                return canonical;

            if (IsCash(methodePaiement))
                return Cash;

            return methodePaiement?.Trim() ?? string.Empty;
        }

        /// <summary>Lève si la méthode n'est pas autorisée sur l'endpoint guichet CASH.</summary>
        public static void EnsureCashOnlyForGuichetEndpoint(string? methodePaiement)
        {
            if (IsElectronic(methodePaiement))
            {
                throw new InvalidOperationException(
                    "Les paiements Mobile Money et carte bancaire doivent utiliser l'endpoint électronique " +
                    "POST /api/Reservation/reservation_with_paiement_electronique. " +
                    "Cet endpoint accepte uniquement CASH (ou espèces).");
            }
        }

        /// <summary>Lève si la méthode n'est pas électronique (endpoint FlexPay).</summary>
        public static void EnsureElectronicOnly(string? methodePaiement)
        {
            if (!IsElectronic(methodePaiement))
            {
                throw new InvalidOperationException(
                    "L'endpoint de paiement électronique accepte uniquement MOBILE_MONEY ou CARTE_BANCAIRE. " +
                    "Utilisez POST /api/Reservation/reservation_with_paiement pour CASH.");
            }
        }

        /// <summary>Interdit l'upload sync batch pour les méthodes électroniques.</summary>
        public static void EnsureAllowedForSyncBatch(string? methodePaiement)
        {
            if (IsElectronic(methodePaiement))
            {
                throw new InvalidOperationException(
                    "Les paiements MOBILE_MONEY et CARTE_BANCAIRE ne peuvent pas être synchronisés via le batch offline. " +
                    "Utilisez le flux FlexPay en ligne.");
            }
        }

        public static string? ToCanonicalCode(string? methodePaiement)
        {
            if (string.IsNullOrWhiteSpace(methodePaiement))
                return null;

            var upper = methodePaiement.Trim().ToUpperInvariant().Replace(" ", "_");
            return upper switch
            {
                "CASH" => Cash,
                "MOBILE_MONEY" => MobileMoney,
                "MOBILEMONEY" => MobileMoney,
                "CARTE_BANCAIRE" => CarteBancaire,
                "CARTE" => CarteBancaire,
                "CARD" => CarteBancaire,
                _ => ElectronicCodes.Contains(upper) ? upper : null
            };
        }

        public enum RecetteBucket
        {
            Espece,
            MobileMoney,
            Virement,
            Carte,
            Autre
        }

        /// <summary>Classement pour les recettes caissier (rétrocompat libellés legacy).</summary>
        public static RecetteBucket GetRecetteBucket(string? methodePaiement)
        {
            var code = ToCanonicalCode(methodePaiement);
            if (code == Cash || IsCash(methodePaiement))
                return RecetteBucket.Espece;
            if (code == MobileMoney)
                return RecetteBucket.MobileMoney;
            if (code == CarteBancaire)
                return RecetteBucket.Carte;

            var lower = methodePaiement?.ToLowerInvariant() ?? string.Empty;
            if (lower.Contains("mobile") || lower.Contains("orange") || lower.Contains("m-pesa") || lower.Contains("airtel"))
                return RecetteBucket.MobileMoney;
            if (lower.Contains("carte") || lower.Contains("card") || lower.Contains("visa") || lower.Contains("master"))
                return RecetteBucket.Carte;
            if (lower.Contains("virement") || lower.Contains("bank"))
                return RecetteBucket.Virement;
            if (lower.Contains("espèce") || lower.Contains("espece") || lower.Contains("cash"))
                return RecetteBucket.Espece;

            return RecetteBucket.Autre;
        }
    }
}
