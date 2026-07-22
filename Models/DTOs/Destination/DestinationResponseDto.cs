using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Destination
{
    /// <summary>
    /// DTO de réponse pour une destination avec ses informations complètes
    /// </summary>
    public class DestinationResponseDto
    {
        /// <summary>
        /// Identifiant unique de la destination
        /// </summary>
        public int IdDestination { get; set; }

        /// <summary>
        /// Ville de départ
        /// </summary>
        [Required]
        public string VilleDepart { get; set; } = string.Empty;

        /// <summary>
        /// Ville d'arrivée
        /// </summary>
        [Required]
        public string VilleArrivee { get; set; } = string.Empty;

        /// <summary>
        /// Montant du trajet
        /// </summary>
        [Required]
        public decimal Montant { get; set; }

        /// <summary>
        /// Jour de départ du trajet (optionnel)
        /// </summary>
        public string? JourDepart { get; set; }

        /// <summary>
        /// Statut de la destination (actif/inactif)
        /// </summary>
        public bool Statut { get; set; }

        /// <summary>
        /// Date de création de la destination
        /// </summary>
        public DateTime DateCreation { get; set; }

        /// <summary>
        /// Date de dernière modification
        /// </summary>
        public DateTime? DateModification { get; set; }

        /// <summary>
        /// Identifiant de la société propriétaire
        /// </summary>
        public int IdSociete { get; set; }

        /// <summary>
        /// Nom de la société propriétaire
        /// </summary>
        public string? NomSociete { get; set; }

        /// <summary>
        /// Devise de la société
        /// </summary>
        public string? DeviseSociete { get; set; }
    }
}
