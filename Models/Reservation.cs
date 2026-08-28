using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models
{
    public class Reservation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdReservation { get; set; }

        [Required]
        [Column("IdUtilisateur")]
        public int IdUtilisateur { get; set; }

        [Required]
        [Column("IdClient")]
        public int IdClient { get; set; }

        [Required]
        [Column("IdVoyage")]
        public int IdVoyage { get; set; }

        [Required]
        [Column("StatutReservation")]
        [StringLength(20)]
        public string StatutReservation { get; set; } = "EN_ATTENTE";

        public bool Statut { get; set; } = true;

        [Required]
        [Column("dateReservation")]
        [DataType(DataType.Date)]
        public DateTime DateReservation { get; set; }

        [Required]
        public int IdSociete { get; set; }

        /// <summary>Site associée à la réservation (optionnel, même société).</summary>
        public int? IdSite { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Le nombre de places doit être au moins 1")]
        [Column("nombreDePlace")]
        public int NombreDePlace { get; set; } = 1;

        /// <summary>
        /// Canal d'origine de la réservation (session client vs rôle staff). Snapshot serveur.
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Origine { get; set; } = Enums.OrigineOperation.Default;

        /// <summary>FK agrégat aller-retour (null = single-leg).</summary>
        public int? IdReservationAllerRetour { get; set; }

        /// <summary>Leg dans l'agrégat AR : Aller / Retour (null = single-leg).</summary>
        public Enums.ReservationAllerRetourLeg? AllerRetourLeg { get; set; }

        // Attributs Techniques
        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.Now;

        [JsonIgnore]
        public DateTime? DateModification { get; set; }

        // Navigation properties
        [JsonIgnore]
        [ValidateNever]
        public Utilisateur? Utilisateur { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Client? Client { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Voyage? Voyage { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Societe? Societe { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Site? Site { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<ReservationPassenger>? Passagers { get; set; }

        [JsonIgnore]
        [ValidateNever]
        [ForeignKey(nameof(IdReservationAllerRetour))]
        public ReservationAllerRetour? ReservationAllerRetour { get; set; }
    }
}
