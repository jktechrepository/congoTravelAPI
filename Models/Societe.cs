using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CongoTravel.Models
{
    public class Societe
    {
        [Key]
        public int IdSociete { get; set; }
        [Required]
        [MaxLength(150)]
        public string? Nom { get; set; }
        public string? Devise { get; set; }
        
        [MaxLength(3)]
        public string CodeDevisePrincipale { get; set; } = "CDF";

        [MaxLength(50)]
        public string? Type { get; set; } // Privee, Publique, Conventionnee
        public string? Logo { get; set; }
        public string? Telephone { get; set; }
        public string? EmailContact { get; set; }
        public string? SiteWeb { get; set; }
        public string? NomCompletResponsable { get; set; }
        
        [MaxLength(10)]
        public string? GenreResponsable { get; set; } // Genre du responsable: Masculin, Feminin
        
        public string? Description { get; set; } // Description de la société
        //[ValidateNever]
        //public IFormFile? Image { get; set; }
        public bool? Statut { get; set; } = true;
        
        /// <summary>
        /// Adresse de la société
        /// </summary>
        [MaxLength(500)]
        public string? AdresseResidence { get; set; }

        // Attributs Techniques
        [JsonIgnore]
        public DateTime DateCreation { get; set; } = DateTime.Now;

        // Navigation properties
        [JsonIgnore]
        [ValidateNever]
        public ICollection<Utilisateur>? Utilisateurs { get; set; }
        [JsonIgnore]
        [ValidateNever]
        public ICollection<Agent>? Agents { get; set; }
        [JsonIgnore]
        [ValidateNever]
        public ICollection<Notification>? Notifications { get; set; }
        // Les fonctionnalités de catégorie client ne sont plus disponibles après la refactorisation
        [JsonIgnore]
        [ValidateNever]
        public ICollection<Destination>? Destinations { get; set; }
        [JsonIgnore]
        [ValidateNever]
        public ICollection<Vehicule>? Vehicules { get; set; }
        [JsonIgnore]
        [ValidateNever]
        public ICollection<TypeVehicule>? TypeVehicules { get; set; }
        [JsonIgnore]
        [ValidateNever]
        public ICollection<Voyage>? Voyages { get; set; }
        [JsonIgnore]
        [ValidateNever]
        public ICollection<Reservation>? Reservations { get; set; }
        [JsonIgnore]
        [ValidateNever]
        public ICollection<Billet>? Billets { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<CategorieSiege>? CategorieSieges { get; set; }

        [JsonIgnore]
        [ValidateNever]
        public ICollection<Site>? Sites { get; set; }
    }
}

