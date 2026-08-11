namespace CongoTravel.Models.DTOs.Restaurant
{
    public class RestaurantHoldResponseDto
    {
        public int IdRestaurantReservation { get; set; }

        public string ReferenceReservation { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public DateTime ExpiresAtUtc { get; set; }

        public decimal AmountPreview { get; set; }

        public string CodeDevise { get; set; } = "CDF";

        public int NombreCouverts { get; set; }
    }
}
