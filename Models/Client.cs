using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CongoTravel.Models
{
    /// <summary>
    /// Modèle représentant un client
    /// </summary>
    public class Client
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdClient { get; set; }

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

        // Les fonctionnalités de CodeCons ne sont plus disponibles après la refactorisation

        /// <summary>
        /// Statut du client (actif/inactif)
        /// </summary>
        public bool Statut { get; set; } = true;

        /// <summary>
        /// Indique si le client est actif (champ métier, par défaut vrai)
        /// </summary>
        public bool IsActif { get; set; } = true;

        /// <summary>
        /// Date de création du client
        /// </summary>
        public DateTime DateCreation { get; set; } = DateTime.Now;

        /// <summary>
        /// Date de dernière modification (pour delta sync)
        /// </summary>
        [JsonIgnore]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Indique si le client est supprimé (soft delete pour sync)
        /// </summary>
        [JsonIgnore]
        public bool? IsDeleted { get; set; } = false;

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

        // Les fonctionnalités d'axe ne sont plus disponibles après la refactorisation

        // Les fonctionnalités de type de courant ne sont plus disponibles après la refactorisation

        // Navigation properties
        // Les fonctionnalités d'usage ne sont plus disponibles après la refactorisation

        // Les fonctionnalités d'axe ne sont plus disponibles après la refactorisation

        // Les fonctionnalités de type de courant ne sont plus disponibles après la refactorisation

        [JsonIgnore]
        [ValidateNever]
        public ICollection<Utilisateur>? Utilisateurs { get; set; } = new List<Utilisateur>();
    }
}

