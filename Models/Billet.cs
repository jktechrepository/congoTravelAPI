using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models
{
    public class Billet
    {
        /// <summary>Identifiant billet (colonne SQL <c>Id</c> pour compatibilité schémas existants).</summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("Id")]
        public int IdBillet { get; set; }

        /// <summary>Billet déjà présenté à l’embarquement (scan QR).</summary>
        public bool IsUsed { get; set; }

        /// <summary>
        /// Identifiant de la réservation (optionnel)
        /// </summary>
        [ValidateNever]
        [Column("IdReservation")]
        public int? IdReservation { get; set; }

        [Required]
        [StringLength(255)]
        public string QrCode { get; set; } = string.Empty;

        [Required]
        [Column("dateGeneration")]
        [DataType(DataType.Date)]
        public DateTime DateGeneration { get; set; }

        /// <summary>Date de début de validité du billet (override optionnel du voyage).</summary>
        public DateTime? DateValiditeDebut { get; set; }

        /// <summary>Date de fin de validité du billet (override optionnel du voyage).</summary>
        public DateTime? DateValiditeFin { get; set; }

        /// <summary>Override optionnel de la pénalité de réaffectation.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? PenaliteOverride { get; set; }

        [Required]
        public int IdSociete { get; set; }

        /// <summary>Site associée au billet (optionnel, même société).</summary>
        public int? IdSite { get; set; }

        /// <summary>
        /// Identifiant du client (optionnel)
        /// </summary>
        [ValidateNever]
        [ForeignKey("Client")]
        [Column("IdClient")]
        public int? IdClient { get; set; }

        /// <summary>
        /// Passager transporté (workflow V2). Nullable pour billets historiques ou hors réservation.
        /// </summary>
        [ValidateNever]
        [Column("IdReservationPassenger")]
        public int? IdReservationPassenger { get; set; }

        /// <summary>
        /// Siège attribué (référentiel bus). Nullable tant que migration ou cas sans réservation.
        /// </summary>
        [ValidateNever]
        [Column("IdSiege")]
        public int? IdSiege { get; set; }

        /// <summary>
        /// Copie affichage / historique (ex. AliasVehicule/12).
        /// </summary>
        [MaxLength(120)]
        [Column("CodeSiege")]
        public string? CodeSiege { get; set; }

        // Attributs Techniques
        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.Now;

        [JsonIgnore]
        public DateTime? DateModification { get; set; }

        // Navigation properties
        [JsonIgnore]
        [ValidateNever]
        public Reservation? Reservation { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Societe? Societe { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Site? Site { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Client? Client { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ReservationPassenger? ReservationPassenger { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Siege? Siege { get; set; }
    }
}
