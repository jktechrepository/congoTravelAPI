using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using CongoTravel.Models.Restaurant.Enums;

namespace CongoTravel.Models.Restaurant
{
    public class RestaurantReservationLine
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdRestaurantReservationLine { get; set; }

        [Required]
        public int IdRestaurantReservation { get; set; }

        [Required]
        public RestaurantReservationLineType LineType { get; set; }

        [Required]
        public int Quantite { get; set; }

        /// <summary>Acompte unitaire facturé.</summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrixUnitaire { get; set; }

        /// <summary>Montant ligne = acompte unitaire × quantité (arrondi).</summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontantLigne { get; set; }

        [Required]
        [MaxLength(3)]
        public string CodeDevise { get; set; } = "CDF";

        public int? IdRestaurantCreneauGlobalQuota { get; set; }

        public int? IdRestaurantCreneauZoneQuota { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public RestaurantReservation? Reservation { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public RestaurantCreneauGlobalQuota? GlobalQuota { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public RestaurantCreneauZoneQuota? ZoneQuota { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<RestaurantTicket> Tickets { get; set; } = new List<RestaurantTicket>();
    }
}
