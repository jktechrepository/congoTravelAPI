using CongoTravel.Models.Restaurant.Enums;

namespace CongoTravel.Models.DTOs.Restaurant
{
    /// <summary>Ligne d'inventaire figée dans le payload d'une commande restaurant.</summary>
    public class RestaurantCommandeSnapshotLineDto
    {
        public RestaurantReservationLineType LineType { get; set; }
        public int Quantite { get; set; }
        public decimal PrixUnitaire { get; set; }
        public decimal MontantLigne { get; set; }
        public string CodeDevise { get; set; } = "CDF";
        public int? IdRestaurantCreneauGlobalQuota { get; set; }
        public int? IdRestaurantCreneauZoneQuota { get; set; }
    }

    /// <summary>Payload métier sérialisé pour finaliser la réservation au callback.</summary>
    public class RestaurantCommandeSnapshotDto
    {
        public RestaurantReservationWithPaiementRequestDto Request { get; set; } = new();
        public List<RestaurantCommandeSnapshotLineDto> Lines { get; set; } = new();
        public decimal MontantSousTotal { get; set; }
        public string CodeDevise { get; set; } = "CDF";
        public int NombreCouverts { get; set; }
        public string ReferenceReservation { get; set; } = string.Empty;
        public string? CustomerRef { get; set; }
    }
}
