namespace CongoTravel.Models.DTOs.Evenement
{
    /// <summary>Disponibilité d'un siège (mode <c>SeatNumbered</c>).</summary>
    public class EvenementSeatAvailabilityDto
    {
        public int IdEvenementSessionSeat { get; set; }

        public string SeatCode { get; set; } = string.Empty;

        public string SeatStatus { get; set; } = string.Empty;

        public string? CodeSection { get; set; }

        public string? LibelleSection { get; set; }

        public int? IdEvenementClasse { get; set; }

        public string? CodeClasse { get; set; }

        public string? LibelleClasse { get; set; }

        public decimal PrixUnitaire { get; set; }

        public string CodeDevise { get; set; } = "CDF";
    }
}
