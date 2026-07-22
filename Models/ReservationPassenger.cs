using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models
{
    /// <summary>
    /// Passager rattaché à une réservation (soi ou tiers).
    /// </summary>
    public class ReservationPassenger
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdReservationPassenger { get; set; }

        [Required]
        public int IdReservation { get; set; }

        public int? IdClient { get; set; }

        [Required]
        [MaxLength(200)]
        public string NomComplet { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Telephone { get; set; }

        [MaxLength(256)]
        [EmailAddress]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string? DocumentType { get; set; }

        [MaxLength(100)]
        public string? DocumentNumero { get; set; }

        [Column(TypeName = "date")]
        public DateTime? DateNaissance { get; set; }

        [MaxLength(10)]
        public string? Genre { get; set; }

        [Required]
        public int IdSociete { get; set; }

        public bool Statut { get; set; } = true;

        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public DateTime? DateModification { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Reservation? Reservation { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Client? Client { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Societe? Societe { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<VoyageSeatAllocation>? VoyageSeatAllocations { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<Billet>? Billets { get; set; }
    }
}
