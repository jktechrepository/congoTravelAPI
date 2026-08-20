using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.Restaurant
{
    /// <summary>Commande restaurant en attente de confirmation FlexPay (pas de réservation métier).</summary>
    public class RestaurantCommandeEnAttente
    {
        [Key]
        public Guid IdRestaurantCommandeEnAttente { get; set; } = Guid.NewGuid();
        [Required] public int IdSociete { get; set; }
        [Required] public int IdRestaurant { get; set; }
        [Required] public int IdRestaurantCreneau { get; set; }
        public int? IdSite { get; set; }
        public int? IdUtilisateur { get; set; }
        public int? IdClient { get; set; }
        [Required, MaxLength(50)] public string MethodePaiement { get; set; } = string.Empty;
        [Required, Column(TypeName = "decimal(18,2)")] public decimal MontantTarif { get; set; }
        [Required, MaxLength(3)] public string CodeDeviseTarif { get; set; } = "CDF";
        [Required, Column(TypeName = "decimal(18,2)")] public decimal MontantFlexPay { get; set; }
        [Required, MaxLength(3)] public string CodeDevisePaiement { get; set; } = "CDF";
        [Column(TypeName = "decimal(18,8)")] public decimal TauxVersDevisePaiement { get; set; } = 1m;
        [MaxLength(120)] public string? OrderNumberFlexPay { get; set; }
        [MaxLength(120)] public string? ReferenceFlexPay { get; set; }
        [MaxLength(120)] public string? IdempotencyKey { get; set; }
        [Required] public string PayloadMetierJson { get; set; } = "{}";
        public int? IdPaiementEnAttente { get; set; }
        [Required] public DateTime DateCreation { get; set; } = DateTime.UtcNow;
        public DateTime? DateExpiration { get; set; }
        [JsonIgnore, ValidateNever] public RestaurantCreneau? Creneau { get; set; }
        [JsonIgnore, ValidateNever] public Site? Site { get; set; }
        [JsonIgnore, ValidateNever] public RestaurantPayment? PaiementEnAttente { get; set; }
    }
}
