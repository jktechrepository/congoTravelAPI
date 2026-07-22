using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CongoTravel.Models
{
    public class PasswordResetToken
    {
        [Key]
        public int IdPasswordResetToken { get; set; }

        [Required]
        [ForeignKey(nameof(Utilisateur))]
        public int IdUtilisateur { get; set; }

        [Required]
        [MaxLength(200)]
        public string Token { get; set; } = string.Empty;

        [Required]
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime DateExpiration { get; set; }

        public DateTime? DateUtilisation { get; set; }

        public bool Utilise => DateUtilisation.HasValue;

        public bool EstExpire => DateTime.UtcNow > DateExpiration;

        public Utilisateur? Utilisateur { get; set; }
    }
}


