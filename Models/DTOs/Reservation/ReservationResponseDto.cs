using CongoTravel.Models.DTOs.Reservation;
using CongoTravel.Models.Enums;

namespace CongoTravel.Models.DTOs
{
    public class ReservationResponseDto
    {
        public int IdReservation { get; set; }
        public int IdUtilisateur { get; set; }
        public int IdClient { get; set; }
        public int IdVoyage { get; set; }
        public string StatutReservation { get; set; }
        public bool Statut { get; set; }
        public DateTime DateReservation { get; set; }
        public int IdSociete { get; set; }

        /// <summary>Canal d'origine (CLIENT, CAISSIER, etc.). Snapshot serveur.</summary>
        public string Origine { get; set; } = OrigineOperation.Default;

        /// <summary>Site associée (optionnel).</summary>
        public int? IdSite { get; set; }

        public DateTime DateCreation { get; set; }
        public DateTime? DateModification { get; set; }

        // Navigation properties optionnelles
        public string? NomUtilisateur { get; set; }
        public string? EmailUtilisateur { get; set; }
        public string? NomClient { get; set; }
        public string? PrenomClient { get; set; }
        public string? TelephoneClient { get; set; }
        public DateTime? DateVoyage { get; set; }
        public TimeSpan? HeureVoyage { get; set; }
        public int? PrixVoyage { get; set; }
        public string? AliasVehicule { get; set; }
        public string? VilleDepart { get; set; }
        public string? VilleArrivee { get; set; }

        /// <summary>Renseigné lorsque les passagers sont chargés (ex. GET par société, client, voyage).</summary>
        public List<ReservationPassengerReadDto>? Passagers { get; set; }

        /// <summary>Agrégat aller-retour (null = single-leg).</summary>
        public int? IdReservationAllerRetour { get; set; }

        /// <summary>Leg AR : Aller / Retour (null = single-leg).</summary>
        public CongoTravel.Models.Enums.ReservationAllerRetourLeg? AllerRetourLeg { get; set; }
    }
}
