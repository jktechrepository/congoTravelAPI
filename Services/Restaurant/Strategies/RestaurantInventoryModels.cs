using CongoTravel.Models.DTOs.Restaurant;
using CongoTravel.Models.Restaurant;
using CongoTravel.Models.Restaurant.Enums;

namespace CongoTravel.Services.Restaurant.Strategies
{
    public class RestaurantHoldLineResult
    {
        public RestaurantReservationLineType LineType { get; set; }

        public int Quantite { get; set; }

        public decimal PrixUnitaire { get; set; }

        public decimal MontantLigne { get; set; }

        public string CodeDevise { get; set; } = "CDF";

        public int? IdRestaurantCreneauGlobalQuota { get; set; }

        public int? IdRestaurantCreneauZoneQuota { get; set; }
    }

    public class RestaurantHoldStrategyResult
    {
        public IReadOnlyList<RestaurantHoldLineResult> Lines { get; init; } = Array.Empty<RestaurantHoldLineResult>();

        public decimal MontantSousTotal { get; init; }

        public int NombreCouverts { get; init; }
    }

    public class RestaurantInventoryHoldRequest
    {
        public RestaurantCreneau Creneau { get; set; } = null!;

        public IReadOnlyList<RestaurantHoldItemRequestDto> Items { get; set; } = Array.Empty<RestaurantHoldItemRequestDto>();

        /// <summary>Acompte unitaire (calculé par le service appelant).</summary>
        public decimal PrixUnitaire { get; set; }

        public string CodeDevise { get; set; } = "CDF";

        public DateTime HoldExpiresAtUtc { get; set; }
    }

    public class RestaurantInventoryConfirmRequest
    {
        public RestaurantReservation Reservation { get; set; } = null!;

        public RestaurantCreneau Creneau { get; set; } = null!;
    }

    public class RestaurantInventoryCancelRequest
    {
        public RestaurantReservation Reservation { get; set; } = null!;

        public RestaurantCreneau Creneau { get; set; } = null!;

        public bool FromConfirmedSale { get; set; }
    }
}
