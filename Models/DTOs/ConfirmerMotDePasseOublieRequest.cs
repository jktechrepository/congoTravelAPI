using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs
{
    public class ConfirmerMotDePasseOublieRequest
    {
        [Required(ErrorMessage = "Le token est obligatoire")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nouveau mot de passe est obligatoire")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Le mot de passe doit contenir entre 6 et 100 caractères")]
        public string NouveauMotDePasse { get; set; } = string.Empty;
    }
}

