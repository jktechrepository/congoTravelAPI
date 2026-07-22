using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Destination
{
    /// <summary>
    /// DTO pour la mise à jour d'une destination existante
    /// </summary>
    public class UpdateDestinationDto
    {
        /// <summary>
        /// Ville de départ
        /// </summary>
        [MaxLength(200, ErrorMessage = "La ville de départ ne peut pas dépasser 200 caractères")]
        public string? VilleDepart { get; set; }

        /// <summary>
        /// Ville d'arrivée
        /// </summary>
        [MaxLength(200, ErrorMessage = "La ville d'arrivée ne peut pas dépasser 200 caractères")]
        public string? VilleArrivee { get; set; }

        /// <summary>
        /// Montant du trajet
        /// </summary>
        [Range(0.01, double.MaxValue, ErrorMessage = "Le montant doit être supérieur à 0")]
        public decimal? Montant { get; set; }

        /// <summary>
        /// Statut de la destination (actif/inactif)
        /// </summary>
        public bool? Statut { get; set; }

        /// <summary>
        /// Jour de départ du trajet (optionnel)
        /// </summary>
        [MaxLength(50, ErrorMessage = "Le jour de départ ne peut pas dépasser 50 caractères")]
        public string? JourDepart { get; set; }
    }
}
