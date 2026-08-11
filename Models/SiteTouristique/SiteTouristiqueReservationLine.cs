using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Models.SiteTouristique
{
    public class SiteTouristiqueReservationLine
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdSiteTouristiqueReservationLine { get; set; }

        [Required]
        public int IdSiteTouristiqueReservation { get; set; }

        [Required]
        public SiteTouristiqueReservationLineType LineType { get; set; }

        [Required]
        public int Quantite { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PrixUnitaire { get; set; }

        [Required]
        [MaxLength(3)]
        public string CodeDevise { get; set; } = "CDF";

        public int? IdSiteTouristiqueClassQuota { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public SiteTouristiqueReservation? Reservation { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public SiteTouristiqueClassQuota? ClassQuota { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<SiteTouristiqueTicket> Tickets { get; set; } = new List<SiteTouristiqueTicket>();
    }
}
