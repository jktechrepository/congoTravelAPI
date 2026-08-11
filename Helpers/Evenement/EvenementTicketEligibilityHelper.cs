using Microsoft.AspNetCore.Http;
using CongoTravel.Models.Evenement;
using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Helpers.Evenement
{
    /// <summary>Règles d'éligibilité entrée événement (check ticket).</summary>
    public static class EvenementTicketEligibilityHelper
    {
        public sealed class Result
        {
            public bool EntreeAutorisee { get; init; }

            public string Statut { get; init; } = string.Empty;

            public string Message { get; init; } = string.Empty;

            public int SuggestedHttpStatus { get; init; } = StatusCodes.Status200OK;
        }

        public static Result Evaluate(
            EvenementTicket? ticket,
            EvenementReservation? reservation,
            EvenementSession? session,
            DateTime utcNow,
            int heuresOuvertureAvantDebut = 0)
        {
            if (ticket == null)
            {
                return new Result
                {
                    EntreeAutorisee = false,
                    Statut = "NonReconnu",
                    Message = "Ticket inconnu ou code invalide.",
                    SuggestedHttpStatus = StatusCodes.Status404NotFound
                };
            }

            if (ticket.Status == EvenementTicketStatus.USED)
            {
                return new Result
                {
                    EntreeAutorisee = false,
                    Statut = "DejaUtilise",
                    Message = "Ce ticket a déjà été utilisé.",
                    SuggestedHttpStatus = StatusCodes.Status409Conflict
                };
            }

            if (ticket.Status == EvenementTicketStatus.VOID)
            {
                return new Result
                {
                    EntreeAutorisee = false,
                    Statut = "Invalide",
                    Message = "Ce ticket est annulé (VOID).",
                    SuggestedHttpStatus = StatusCodes.Status409Conflict
                };
            }

            if (reservation == null)
            {
                return new Result
                {
                    EntreeAutorisee = false,
                    Statut = "Invalide",
                    Message = "Réservation associée au ticket introuvable.",
                    SuggestedHttpStatus = StatusCodes.Status409Conflict
                };
            }

            if (reservation.Status != EvenementReservationStatus.CONFIRMED)
            {
                return new Result
                {
                    EntreeAutorisee = false,
                    Statut = "Invalide",
                    Message = $"Réservation non confirmée (statut {reservation.Status}).",
                    SuggestedHttpStatus = StatusCodes.Status409Conflict
                };
            }

            if (session == null)
            {
                return new Result
                {
                    EntreeAutorisee = false,
                    Statut = "Invalide",
                    Message = "Session associée au ticket introuvable.",
                    SuggestedHttpStatus = StatusCodes.Status409Conflict
                };
            }

            if (session.Status != EvenementSessionStatus.Published)
            {
                return new Result
                {
                    EntreeAutorisee = false,
                    Statut = "SessionInactive",
                    Message = $"Session non ouverte à l'entrée (statut {session.Status}).",
                    SuggestedHttpStatus = StatusCodes.Status400BadRequest
                };
            }

            if (!IsWithinEntryWindow(session, utcNow, heuresOuvertureAvantDebut))
            {
                return new Result
                {
                    EntreeAutorisee = false,
                    Statut = "HorsFenetre",
                    Message = BuildHorsFenetreMessage(session, utcNow, heuresOuvertureAvantDebut),
                    SuggestedHttpStatus = StatusCodes.Status400BadRequest
                };
            }

            return new Result
            {
                EntreeAutorisee = true,
                Statut = "Valide",
                Message = "Ticket valide. Entrée autorisée.",
                SuggestedHttpStatus = StatusCodes.Status200OK
            };
        }

        /// <summary>
        /// Fenêtre : [StartAtUtc − heuresAvantDebut, EndAtUtc] ou […, StartAtUtc + 24h] si pas de fin.
        /// </summary>
        public static bool IsWithinEntryWindow(
            EvenementSession session,
            DateTime utcNow,
            int heuresOuvertureAvantDebut = 0)
        {
            var start = EvenementDateTimeUtcHelper.NormalizeToUtc(session.StartAtUtc);
            var now = EvenementDateTimeUtcHelper.NormalizeToUtc(utcNow);
            var heures = Math.Clamp(heuresOuvertureAvantDebut, 0, 72);
            var ouverture = start.AddHours(-heures);

            if (now < ouverture)
                return false;

            if (session.EndAtUtc.HasValue)
            {
                var end = EvenementDateTimeUtcHelper.NormalizeToUtc(session.EndAtUtc.Value);
                return now <= end;
            }

            return now <= start.AddHours(24);
        }

        private static string BuildHorsFenetreMessage(
            EvenementSession session,
            DateTime utcNow,
            int heuresOuvertureAvantDebut)
        {
            var start = EvenementDateTimeUtcHelper.NormalizeToUtc(session.StartAtUtc);
            var now = EvenementDateTimeUtcHelper.NormalizeToUtc(utcNow);
            var heures = Math.Clamp(heuresOuvertureAvantDebut, 0, 72);
            var ouverture = start.AddHours(-heures);

            if (now < ouverture)
            {
                return $"Entrée pas encore ouverte (ouverture à partir de {ouverture:O} UTC ; début session {start:O} UTC).";
            }

            if (session.EndAtUtc.HasValue)
            {
                var end = EvenementDateTimeUtcHelper.NormalizeToUtc(session.EndAtUtc.Value);
                if (now > end)
                    return $"Entrée fermée (fin session : {end:O} UTC).";
            }

            return "Entrée hors fenêtre autorisée pour cette session.";
        }
    }
}
