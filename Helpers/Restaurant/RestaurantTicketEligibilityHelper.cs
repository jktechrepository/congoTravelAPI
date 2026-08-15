using Microsoft.AspNetCore.Http;
using CongoTravel.Models.Restaurant;
using CongoTravel.Models.Restaurant.Enums;

namespace CongoTravel.Helpers.Restaurant
{
    /// <summary>Règles d'éligibilité entrée restaurant (check ticket) — fenêtre créneau.</summary>
    public static class RestaurantTicketEligibilityHelper
    {
        public sealed class Result
        {
            public bool EntreeAutorisee { get; init; }

            public string Statut { get; init; } = string.Empty;

            public string Message { get; init; } = string.Empty;

            public int SuggestedHttpStatus { get; init; } = StatusCodes.Status200OK;
        }

        public static Result Evaluate(
            RestaurantTicket? ticket,
            RestaurantReservation? reservation,
            RestaurantCreneau? creneau,
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

            if (ticket.Status == RestaurantTicketStatus.USED)
            {
                return new Result
                {
                    EntreeAutorisee = false,
                    Statut = "DejaUtilise",
                    Message = "Ce ticket a déjà été utilisé.",
                    SuggestedHttpStatus = StatusCodes.Status409Conflict
                };
            }

            if (ticket.Status == RestaurantTicketStatus.VOID)
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

            if (reservation.Status != RestaurantReservationStatus.CONFIRMED)
            {
                return new Result
                {
                    EntreeAutorisee = false,
                    Statut = "Invalide",
                    Message = $"Réservation non confirmée (statut {reservation.Status}).",
                    SuggestedHttpStatus = StatusCodes.Status409Conflict
                };
            }

            if (creneau == null)
            {
                return new Result
                {
                    EntreeAutorisee = false,
                    Statut = "Invalide",
                    Message = "Créneau associé au ticket introuvable.",
                    SuggestedHttpStatus = StatusCodes.Status409Conflict
                };
            }

            if (creneau.Status != RestaurantStatus.Published)
            {
                return new Result
                {
                    EntreeAutorisee = false,
                    Statut = "CreneauInactif",
                    Message = $"Créneau non ouvert à l'entrée (statut {creneau.Status}).",
                    SuggestedHttpStatus = StatusCodes.Status400BadRequest
                };
            }

            if (!IsWithinEntryWindow(creneau, utcNow, heuresOuvertureAvantDebut))
            {
                return new Result
                {
                    EntreeAutorisee = false,
                    Statut = "HorsFenetre",
                    Message = BuildHorsFenetreMessage(creneau, utcNow, heuresOuvertureAvantDebut),
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
        /// Fenêtre : [StartAtUtc − heuresAvantDebut, EndAtUtc].
        /// </summary>
        public static bool IsWithinEntryWindow(
            RestaurantCreneau creneau,
            DateTime utcNow,
            int heuresOuvertureAvantDebut = 0)
        {
            var start = RestaurantDateTimeUtcHelper.NormalizeToUtc(creneau.StartAtUtc);
            var end = RestaurantDateTimeUtcHelper.NormalizeToUtc(creneau.EndAtUtc);
            var now = RestaurantDateTimeUtcHelper.NormalizeToUtc(utcNow);
            var heures = Math.Clamp(heuresOuvertureAvantDebut, 0, 72);
            var ouverture = start.AddHours(-heures);

            if (now < ouverture)
                return false;

            return now <= end;
        }

        private static string BuildHorsFenetreMessage(
            RestaurantCreneau creneau,
            DateTime utcNow,
            int heuresOuvertureAvantDebut)
        {
            var start = RestaurantDateTimeUtcHelper.NormalizeToUtc(creneau.StartAtUtc);
            var end = RestaurantDateTimeUtcHelper.NormalizeToUtc(creneau.EndAtUtc);
            var now = RestaurantDateTimeUtcHelper.NormalizeToUtc(utcNow);
            var heures = Math.Clamp(heuresOuvertureAvantDebut, 0, 72);
            var ouverture = start.AddHours(-heures);

            if (now < ouverture)
            {
                return $"Entrée pas encore ouverte (ouverture à partir de {ouverture:O} UTC ; début créneau {start:O} UTC).";
            }

            if (now > end)
                return $"Entrée fermée (fin créneau : {end:O} UTC).";

            return "Entrée hors fenêtre autorisée pour ce créneau.";
        }
    }
}
