namespace CongoTravel.Models.DTOs.SiteTouristique
{
    /// <summary>En-tête réservation site touristique pour les listes (sans lignes / tickets / paiements).</summary>
    public class SiteTouristiqueReservationListItemDto
    {
        public int IdSiteTouristiqueReservation { get; set; }

        public int IdSociete { get; set; }

        public int IdSiteTouristiqueJournee { get; set; }

        public int? IdSite { get; set; }

        public string ReferenceReservation { get; set; } = string.Empty;

        public string? CustomerRef { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime? ExpiresAtUtc { get; set; }

        public decimal MontantSousTotal { get; set; }

        public string CodeDevise { get; set; } = "CDF";

        public DateTime DateCreation { get; set; }

        public DateTime? DateModification { get; set; }
    }
}
