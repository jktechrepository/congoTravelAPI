namespace CongoTravel.Models.DTOs.Evenement
{
    public class EvenementReservationLineResponseDto
    {
        public int IdEvenementReservationLine { get; set; }

        public string LineType { get; set; } = string.Empty;

        public int Quantite { get; set; }

        public decimal PrixUnitaire { get; set; }

        public string CodeDevise { get; set; } = "CDF";

        public int? IdEvenementSessionSeat { get; set; }

        public int? IdEvenementSessionClassQuota { get; set; }
    }
}
