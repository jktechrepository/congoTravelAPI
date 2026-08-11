using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Helpers.Evenement
{
    /// <summary>
    /// Fenêtre de vente session événement : Published jusqu’à la fin de session
    /// (<see cref="EvenementSession.EndAtUtc"/> ou <see cref="EvenementSession.StartAtUtc"/> + 24 h).
    /// </summary>
    public static class EvenementSessionSalesEligibilityHelper
    {
        /// <summary>Fin de vente UTC : EndAtUtc si présent, sinon StartAtUtc + 24 h.</summary>
        public static DateTime ResolveSalesEndUtc(EvenementSession session)
        {
            var start = EvenementDateTimeUtcHelper.NormalizeToUtc(session.StartAtUtc);
            if (session.EndAtUtc.HasValue)
                return EvenementDateTimeUtcHelper.NormalizeToUtc(session.EndAtUtc.Value);
            return start.AddHours(24);
        }

        public static bool CanSell(EvenementSession session, DateTime utcNow)
        {
            if (session.Status != EvenementSessionStatus.Published)
                return false;

            var now = EvenementDateTimeUtcHelper.NormalizeToUtc(utcNow);
            return now < ResolveSalesEndUtc(session);
        }

        /// <summary>Rejette si la session n’est plus en vente (statut ou fin dépassée).</summary>
        public static void EnsureCanSell(EvenementSession session, DateTime utcNow)
        {
            if (session.Status != EvenementSessionStatus.Published)
            {
                throw new InvalidOperationException(
                    $"Impossible de vendre pour une session au statut {session.Status} (Published requis).");
            }

            var now = EvenementDateTimeUtcHelper.NormalizeToUtc(utcNow);
            var salesEnd = ResolveSalesEndUtc(session);
            if (now >= salesEnd)
            {
                throw new InvalidOperationException(
                    $"Vente fermée : la session est terminée (fin : {salesEnd:O}).");
            }
        }
    }
}
