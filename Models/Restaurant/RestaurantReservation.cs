using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using CongoTravel.Models.Restaurant.Enums;

namespace CongoTravel.Models.Restaurant
{
    public class RestaurantReservation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdRestaurantReservation { get; set; }

        [Required]
        public int IdSociete { get; set; }

        [Required]
        public int IdRestaurant { get; set; }

        [Required]
        public int IdRestaurantCreneau { get; set; }

        /// <summary>Site opérationnel (défaut établissement, override possible à l'achat).</summary>
        public int? IdSite { get; set; }

        /// <summary>Acheteur authentifié (JWT) ; null si guichet / legacy.</summary>
        public int? IdUtilisateur { get; set; }

        [Required]
        [MaxLength(64)]
        public string ReferenceReservation { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? CustomerRef { get; set; }

        [Required]
        public RestaurantReservationStatus Status { get; set; } = RestaurantReservationStatus.HOLD;

        public DateTime? ExpiresAtUtc { get; set; }

        /// <summary>Montant total d'acompte (sous-total).</summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontantSousTotal { get; set; }

        [Required]
        [MaxLength(3)]
        public string CodeDevise { get; set; } = "CDF";

        /// <summary>Nombre de couverts réservés (somme des quantités de lignes).</summary>
        [Required]
        public int NombreCouverts { get; set; }

        [MaxLength(120)]
        public string? IdempotencyKey { get; set; }

        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public DateTime? DateModification { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Societe? Societe { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Site? Site { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Restaurant? Restaurant { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public RestaurantCreneau? Creneau { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<RestaurantReservationLine> Lines { get; set; } = new List<RestaurantReservationLine>();

        [JsonIgnore]
        [ValidateNever]
        public ICollection<RestaurantPayment> Payments { get; set; } = new List<RestaurantPayment>();
    }
}
