using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs
{
    public class ReinitialiserMotDePasseIndividuelDto
    {
        [Required(ErrorMessage = "L'ID de l'utilisateur est obligatoire")]
        public int IdUtilisateur { get; set; }

        [Required(ErrorMessage = "Le nouveau mot de passe est obligatoire")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Le mot de passe doit contenir entre 6 et 100 caractères")]
        public string NouveauMotDePasse { get; set; } = string.Empty;
    }
}

