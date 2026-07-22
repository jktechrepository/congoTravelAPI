using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models
{
    /// <summary>
    /// Historique d’embarquement : billet scanné, passager et horodatage.
    /// </summary>
    public class BilletEmbarquement
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdEmbarquement { get; set; }

        [Required]
        public int IdSociete { get; set; }

        /// <summary>Référence <see cref="Billet.IdBillet"/> (colonne <c>Billets.Id</c>).</summary>
        [Required]
        public int IdBillet { get; set; }

        [Required]
        public int IdReservationPassenger { get; set; }

        public DateTime DateEmbarquementUtc { get; set; }

        /// <summary>Agent / utilisateur ayant enregistré l’embarquement (JWT).</summary>
        public int? IdUtilisateurEnregistrement { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Billet? Billet { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ReservationPassenger? ReservationPassenger { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Societe? Societe { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public Utilisateur? UtilisateurEnregistrement { get; set; }
    }
}
