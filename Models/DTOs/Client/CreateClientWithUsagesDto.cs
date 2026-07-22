using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Client
{
    /// <summary>
    /// DTO pour créer un client (version nettoyée et simplifiée)
    /// </summary>
    public class CreateClientWithUsagesDto
    {
        /// <summary>
        /// Nom du client
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string NomClient { get; set; } = string.Empty;

        /// <summary>
        /// Adresse complète du client (optionnelle)
        /// </summary>
        [MaxLength(500)]
        public string? AdresseClient { get; set; }

        /// <summary>
        /// Numéro de téléphone du client
        /// </summary>
        [MaxLength(20)]
        public string? Telephone { get; set; }

        /// <summary>
        /// Email du client
        /// </summary>
        [EmailAddress(ErrorMessage = "L'email doit être valide")]
        [MaxLength(256)]
        public string? EmailClient { get; set; }

        /// <summary>
        /// Genre du client (M, F, Autre)
        /// </summary>
        [MaxLength(10)]
        public string? GenreClient { get; set; }

        /// <summary>
        /// Statut du client (actif/inactif)
        /// </summary>
        public bool Statut { get; set; } = true;

        /// <summary>
        /// Indique si le client est actif (champ métier)
        /// </summary>
        public bool IsActif { get; set; } = true;

        /// <summary>
        /// Province du client
        /// </summary>
        [MaxLength(100)]
        public string? Province { get; set; }

        /// <summary>
        /// Ville du client
        /// </summary>
        [MaxLength(100)]
        public string? Ville { get; set; }

        /// <summary>
        /// Commune du client
        /// </summary>
        [MaxLength(100)]
        public string? Commune { get; set; }

        /// <summary>
        /// Avenue du client
        /// </summary>
        [MaxLength(200)]
        public string? Avenue { get; set; }

        /// <summary>
        /// Numéro de l'adresse du client
        /// </summary>
        [MaxLength(50)]
        public string? Numero { get; set; }
    }
}
