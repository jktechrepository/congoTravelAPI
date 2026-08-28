using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models
{
    /// <summary>
    /// Agrégat réservation aller-retour Transport (2 voyages, 2 réservations, 1 paiement).
    /// </summary>
    public class ReservationAllerRetour
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdReservationAllerRetour { get; set; }

        [Required]
        public int IdVoyageAller { get; set; }

        [Required]
        public int IdVoyageRetour { get; set; }

        public int? IdReservationAller { get; set; }

        public int? IdReservationRetour { get; set; }

        public int? IdPaiement { get; set; }

        public Guid? IdCommandeReservationEnAttente { get; set; }

        [Required]
        [MaxLength(30)]
        public string Statut { get; set; } = Enums.ReservationAllerRetourStatut.EnAttentePaiement;

        [Required]
        public int IdSociete { get; set; }

        [Required]
        public int IdClient { get; set; }

        [Required]
        public int IdUtilisateur { get; set; }

        public int? IdSite { get; set; }

        [Required]
        [MaxLength(20)]
        public string Origine { get; set; } = Enums.OrigineOperation.Default;

        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        public DateTime? DateModification { get; set; }

        [JsonIgnore]
        [ValidateNever]
        [ForeignKey(nameof(IdVoyageAller))]
        public Voyage? VoyageAller { get; set; }

        [JsonIgnore]
        [ValidateNever]
        [ForeignKey(nameof(IdVoyageRetour))]
        public Voyage? VoyageRetour { get; set; }

        [JsonIgnore]
        [ValidateNever]
        [ForeignKey(nameof(IdReservationAller))]
        public Reservation? ReservationAller { get; set; }

        [JsonIgnore]
        [ValidateNever]
        [ForeignKey(nameof(IdReservationRetour))]
        public Reservation? ReservationRetour { get; set; }

        [JsonIgnore]
        [ValidateNever]
        [ForeignKey(nameof(IdPaiement))]
        public Paiement? Paiement { get; set; }

        [JsonIgnore]
        [ValidateNever]
        [ForeignKey(nameof(IdSociete))]
        public Societe? Societe { get; set; }

        [JsonIgnore]
        [ValidateNever]
        [ForeignKey(nameof(IdClient))]
        public Client? Client { get; set; }

        [JsonIgnore]
        [ValidateNever]
        [ForeignKey(nameof(IdUtilisateur))]
        public Utilisateur? Utilisateur { get; set; }

        [JsonIgnore]
        [ValidateNever]
        [ForeignKey(nameof(IdSite))]
        public Site? Site { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<Reservation>? Reservations { get; set; }
    }
}
