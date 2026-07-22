using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Destination
{
    /// <summary>
    /// DTO pour la création d'une nouvelle destination
    /// </summary>
    public class CreateDestinationDto
    {
        /// <summary>
        /// Ville de départ
        /// </summary>
        [Required(ErrorMessage = "La ville de départ est obligatoire")]
        [MaxLength(200, ErrorMessage = "La ville de départ ne peut pas dépasser 200 caractères")]
        public string VilleDepart { get; set; } = string.Empty;

        /// <summary>
        /// Ville d'arrivée
        /// </summary>
        [Required(ErrorMessage = "La ville d'arrivée est obligatoire")]
        [MaxLength(200, ErrorMessage = "La ville d'arrivée ne peut pas dépasser 200 caractères")]
        public string VilleArrivee { get; set; } = string.Empty;

        /// <summary>
        /// Montant du trajet
        /// </summary>
        [Required(ErrorMessage = "Le montant est obligatoire")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Le montant doit être supérieur à 0")]
        public decimal Montant { get; set; }

        /// <summary>
        /// Identifiant de la société propriétaire
        /// </summary>
        [Required(ErrorMessage = "L'identifiant de la société est obligatoire")]
        public int IdSociete { get; set; }

        /// <summary>
        /// Jour de départ du trajet (optionnel)
        /// </summary>
        [MaxLength(50, ErrorMessage = "Le jour de départ ne peut pas dépasser 50 caractères")]
        public string? JourDepart { get; set; }
    }
}
