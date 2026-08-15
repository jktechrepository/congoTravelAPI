namespace CongoTravel.Models.DTOs.Restaurant
{
    public class RestaurantReservationListItemDto
    {
        public int IdRestaurantReservation { get; set; }

        public int IdSociete { get; set; }

        public int IdRestaurant { get; set; }

        public int IdRestaurantCreneau { get; set; }

        public int? IdSite { get; set; }

        public string ReferenceReservation { get; set; } = string.Empty;

        public string? CustomerRef { get; set; }

        public int? IdUtilisateur { get; set; }

        public int? IdClient { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime? ExpiresAtUtc { get; set; }

        public decimal MontantSousTotal { get; set; }

        public string CodeDevise { get; set; } = "CDF";

        public int NombreCouverts { get; set; }

        public DateTime DateCreation { get; set; }

        public DateTime? DateModification { get; set; }
    }
}
