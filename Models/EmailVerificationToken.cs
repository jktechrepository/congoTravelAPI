using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CongoTravel.Models
{
    /// <summary>
    /// Token de vérification d'email (lien). Le secret brut n'est jamais stocké — uniquement <see cref="CodeHash"/>.
    /// </summary>
    public class EmailVerificationToken
    {
        [Key]
        public int IdEmailVerificationToken { get; set; }

        [Required]
        [ForeignKey(nameof(Utilisateur))]
        public int IdUtilisateur { get; set; }

        /// <summary>SHA-256 hex du token contenu dans le lien.</summary>
        [Required]
        [MaxLength(128)]
        public string CodeHash { get; set; } = string.Empty;

        [Required]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime DateExpiration { get; set; }

        public DateTime? DateUtilisation { get; set; }

        public int AttemptCount { get; set; }

        public bool Utilise => DateUtilisation.HasValue;

        public bool EstExpire => DateTime.UtcNow > DateExpiration;

        public Utilisateur? Utilisateur { get; set; }
    }
}
