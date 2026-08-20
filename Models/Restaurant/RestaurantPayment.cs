using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using CongoTravel.Models.Restaurant.Enums;

namespace CongoTravel.Models.Restaurant
{
    public class RestaurantPayment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdRestaurantPayment { get; set; }

        public int? IdRestaurantReservation { get; set; }

        public Guid? IdRestaurantCommandeEnAttente { get; set; }

        /// <summary>Site marchand / bénéficiaire.</summary>
        public int? IdSite { get; set; }

        [Required]
        [MaxLength(100)]
        public string ReferencePaiement { get; set; } = string.Empty;

        [Required]
        [MaxLength(40)]
        public string Provider { get; set; } = string.Empty;

        [MaxLength(120)]
        public string? ProviderTxRef { get; set; }

        [Required]
        public RestaurantPaymentStatus Status { get; set; } = RestaurantPaymentStatus.PENDING;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Montant { get; set; }

        [Required]
        [MaxLength(3)]
        public string CodeDevise { get; set; } = "CDF";

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontantTarif { get; set; }

        [Required]
        [MaxLength(3)]
        public string CodeDeviseTarif { get; set; } = "CDF";

        [Required]
        [Column(TypeName = "decimal(18,8)")]
        public decimal TauxVersDevisePaiement { get; set; } = 1m;

        [MaxLength(120)]
        public string? IdempotencyKey { get; set; }

        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public DateTime? DateModification { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public RestaurantReservation? Reservation { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public RestaurantCommandeEnAttente? CommandeEnAttente { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Site? Site { get; set; }
    }
}
