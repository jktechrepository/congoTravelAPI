using Microsoft.AspNetCore.Http;
using CongoTravel.Models.SiteTouristique;
using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Helpers.SiteTouristique
{
    /// <summary>Règles d'éligibilité entrée site touristique (check ticket) — jour calendaire DateVisite.</summary>
    public static class SiteTouristiqueTicketEligibilityHelper
    {
        public sealed class Result
        {
            public bool EntreeAutorisee { get; init; }

            public string Statut { get; init; } = string.Empty;

            public string Message { get; init; } = string.Empty;

            public int SuggestedHttpStatus { get; init; } = StatusCodes.Status200OK;
        }

        public static Result Evaluate(
            SiteTouristiqueTicket? ticket,
            SiteTouristiqueReservation? reservation,
            SiteTouristiqueJournee? journee,
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

            if (ticket.Status == SiteTouristiqueTicketStatus.USED)
            {
                return new Result
                {
                    EntreeAutorisee = false,
                    Statut = "DejaUtilise",
                    Message = "Ce ticket a déjà été utilisé.",
                    SuggestedHttpStatus = StatusCodes.Status409Conflict
                };
            }

            if (ticket.Status == SiteTouristiqueTicketStatus.VOID)
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

            if (reservation.Status != SiteTouristiqueReservationStatus.CONFIRMED)
            {
                return new Result
                {
                    EntreeAutorisee = false,
                    Statut = "Invalide",
                    Message = $"Réservation non confirmée (statut {reservation.Status}).",
                    SuggestedHttpStatus = StatusCodes.Status409Conflict
                };
            }

            if (journee == null)
            {
                return new Result
                {
                    EntreeAutorisee = false,
                    Statut = "Invalide",
                    Message = "Journée associée au ticket introuvable.",
                    SuggestedHttpStatus = StatusCodes.Status409Conflict
                };
            }

            if (journee.Status != SiteTouristiqueStatus.Published)
            {
                return new Result
                {
                    EntreeAutorisee = false,
                    Statut = "JourneeInactive",
                    Message = $"Journée non ouverte à l'entrée (statut {journee.Status}).",
                    SuggestedHttpStatus = StatusCodes.Status400BadRequest
                };
            }

            if (!IsWithinEntryWindow(journee, utcNow))
            {
                return new Result
                {
                    EntreeAutorisee = false,
                    Statut = "HorsFenetre",
                    Message = BuildHorsFenetreMessage(journee, utcNow),
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

        /// <summary>Entrée autorisée si la date UTC courante égale <see cref="SiteTouristiqueJournee.DateVisite"/>.</summary>
        public static bool IsWithinEntryWindow(SiteTouristiqueJournee journee, DateTime utcNow)
        {
            var now = SiteTouristiqueDateTimeUtcHelper.NormalizeToUtc(utcNow);
            var today = DateOnly.FromDateTime(now);
            return today == journee.DateVisite;
        }

        private static string BuildHorsFenetreMessage(SiteTouristiqueJournee journee, DateTime utcNow)
        {
            var now = SiteTouristiqueDateTimeUtcHelper.NormalizeToUtc(utcNow);
            var today = DateOnly.FromDateTime(now);
            if (today < journee.DateVisite)
            {
                return $"Entrée pas encore ouverte (visite le {journee.DateVisite:yyyy-MM-dd} ; aujourd'hui UTC {today:yyyy-MM-dd}).";
            }

            return $"Entrée fermée (visite le {journee.DateVisite:yyyy-MM-dd} ; aujourd'hui UTC {today:yyyy-MM-dd}).";
        }
    }
}
