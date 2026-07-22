using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs
{
    public class ReinitialiserMotDePasseMasseDto
    {
        [Required(ErrorMessage = "L'ID de la société est obligatoire")]
        public int IdSociete { get; set; }

        [Required(ErrorMessage = "L'ID du rôle est obligatoire")]
        public int IdRole { get; set; }

        [Required(ErrorMessage = "Le nouveau mot de passe est obligatoire")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Le mot de passe doit contenir entre 6 et 100 caractères")]
        public string NouveauMotDePasse { get; set; } = string.Empty;
    }
}

