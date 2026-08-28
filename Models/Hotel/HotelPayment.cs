using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using CongoTravel.Models.Hotel.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models.Hotel
{
    public class HotelPayment
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdHotelPayment { get; set; }
        public int? IdHotelReservation { get; set; }
        public Guid? IdHotelCommandeEnAttente { get; set; }
        public int? IdSite { get; set; }
        [Required, MaxLength(100)]
        public string ReferencePaiement { get; set; } = string.Empty;
        [Required, MaxLength(40)]
        public string Provider { get; set; } = "CASH";
        [MaxLength(120)]
        public string? ProviderTxRef { get; set; }
        public HotelPaymentStatus Status { get; set; } = HotelPaymentStatus.PENDING;
        [Column(TypeName = "decimal(18,2)")]
        public decimal Montant { get; set; }
        [Required, MaxLength(3)]
        public string CodeDevise { get; set; } = "CDF";
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontantTarif { get; set; }
        [Required, MaxLength(3)]
        public string CodeDeviseTarif { get; set; } = "CDF";
        [Column(TypeName = "decimal(18,8)")]
        public decimal TauxVersDevisePaiement { get; set; } = 1m;
        [MaxLength(120)]
        public string? IdempotencyKey { get; set; }
        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;
        [JsonIgnore]
        public DateTime? DateModification { get; set; }
        [JsonIgnore, ValidateNever]
        public HotelReservation? Reservation { get; set; }
        [JsonIgnore, ValidateNever]
        public HotelCommandeEnAttente? CommandeEnAttente { get; set; }
        [JsonIgnore, ValidateNever]
        public Site? Site { get; set; }
    }
}
