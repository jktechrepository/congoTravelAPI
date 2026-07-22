namespace CongoTravel.Models.DTOs.Evenement
{
    /// <summary>En-tête réservation événement pour les listes (sans lignes / tickets / paiements).</summary>
    public class EvenementReservationListItemDto
    {
        public int IdEvenementReservation { get; set; }

        public int IdSociete { get; set; }

        public int IdEvenementSession { get; set; }

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
