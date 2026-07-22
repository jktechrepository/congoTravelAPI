using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs
{
    public class ChangerMotDePasseRequest
    {
        [Required(ErrorMessage = "L'ancien mot de passe est obligatoire")]
        public string AncienMotDePasse { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nouveau mot de passe est obligatoire")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Le mot de passe doit contenir entre 6 et 100 caractères")]
        public string NouveauMotDePasse { get; set; } = string.Empty;
    }
}

