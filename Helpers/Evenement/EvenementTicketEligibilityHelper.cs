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
            DateTime utcNow)
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

            if (!IsWithinEntryWindow(session, utcNow))
            {
                return new Result
                {
                    EntreeAutorisee = false,
                    Statut = "HorsFenetre",
                    Message = BuildHorsFenetreMessage(session, utcNow),
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

        public static bool IsWithinEntryWindow(EvenementSession session, DateTime utcNow)
        {
            if (utcNow < session.StartAtUtc)
                return false;

            if (session.EndAtUtc.HasValue)
                return utcNow <= session.EndAtUtc.Value;

            return utcNow <= session.StartAtUtc.AddHours(24);
        }

        private static string BuildHorsFenetreMessage(EvenementSession session, DateTime utcNow)
        {
            if (utcNow < session.StartAtUtc)
            {
                return $"Entrée pas encore ouverte (début session : {session.StartAtUtc:O}).";
            }

            if (session.EndAtUtc.HasValue && utcNow > session.EndAtUtc.Value)
            {
                return $"Entrée fermée (fin session : {session.EndAtUtc.Value:O}).";
            }

            return "Entrée hors fenêtre autorisée pour cette session.";
        }
    }
}
