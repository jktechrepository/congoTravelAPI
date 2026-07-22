using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CongoTravel.Models
{
    /// <summary>
    /// Modèle représentant les préférences de notification d'un utilisateur
    /// </summary>
    public class NotificationPreference
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdNotificationPreference { get; set; }

        /// <summary>
        /// Identifiant de l'utilisateur
        /// </summary>
        [Required]
        public int IdUtilisateur { get; set; }

        /// <summary>
        /// Autoriser les notifications push
        /// </summary>
        public bool AllowPush { get; set; } = true;

        /// <summary>
        /// Autoriser les notifications in-app
        /// </summary>
        public bool AllowInApp { get; set; } = true;

        /// <summary>
        /// Autoriser les notifications SMS
        /// </summary>
        public bool AllowSms { get; set; } = true;

        /// <summary>
        /// Autoriser les notifications email
        /// </summary>
        public bool AllowEmail { get; set; } = true;

        /// <summary>
        /// Opt-out global : désactiver toutes les notifications
        /// </summary>
        public bool OptOutGlobal { get; set; } = false;

        /// <summary>
        /// Opt-out spécifique pour les factures
        /// </summary>
        public bool OptOutFactures { get; set; } = false;

        /// <summary>
        /// Date de création des préférences
        /// </summary>
        public DateTime DateCreation { get; set; } = DateTime.Now;

        /// <summary>
        /// Date de dernière modification
        /// </summary>
        public DateTime DateModification { get; set; } = DateTime.Now;

        // Navigation property
        [JsonIgnore]
        [ValidateNever]
        [ForeignKey("IdUtilisateur")]
        public Utilisateur? Utilisateur { get; set; }
    }
}

