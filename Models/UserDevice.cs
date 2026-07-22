using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CongoTravel.Models
{
    /// <summary>
    /// Modèle pour stocker les informations des appareils utilisateurs (FCM tokens)
    /// </summary>
    public class UserDevice
    {
        [Key]
        public int IdUserDevice { get; set; }

        [Required]
        [ForeignKey("Utilisateur")]
        public int IdUtilisateur { get; set; }

        [Required]
        [MaxLength(500)]
        public string FcmToken { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? DeviceType { get; set; } // "Android", "iOS", "Web"

        [MaxLength(100)]
        public string? DeviceModel { get; set; }

        [MaxLength(50)]
        public string? OsVersion { get; set; }

        [MaxLength(100)]
        public string? DefaultDevice { get; set; }

        public bool? Statut { get; set; } = true;

        public DateTime DateEnregistrement { get; set; } = DateTime.Now;

        public DateTime? DateDerniereUtilisation { get; set; }

        // Navigation
        public virtual Utilisateur Utilisateur { get; set; } = null!;
    }
}
