namespace CongoTravel.Models.DTOs.Restaurant
{
    public class RestaurantReservationLineResponseDto
    {
        public int IdRestaurantReservationLine { get; set; }

        public string LineType { get; set; } = string.Empty;

        public int Quantite { get; set; }

        public decimal PrixUnitaire { get; set; }

        public decimal MontantLigne { get; set; }

        public string CodeDevise { get; set; } = "CDF";

        public int? IdRestaurantCreneauGlobalQuota { get; set; }

        public int? IdRestaurantCreneauZoneQuota { get; set; }
    }
}
