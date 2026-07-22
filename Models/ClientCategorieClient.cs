using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace CongoTravel.Models
{
    /// <summary>
    /// Table de jointure pour la relation many-to-many entre Client et CategorieClient
    /// Permet à un client d'appartenir à plusieurs catégories
    /// </summary>
    public class ClientCategorieClient
    {
        /// <summary>
        /// Identifiant du client
        /// </summary>
        [Key]
        [Column(Order = 0)]
        public int IdClient { get; set; }

        /// <summary>
        /// Identifiant de la catégorie
        /// </summary>
        [Key]
        [Column(Order = 1)]
        public int IdCategorie { get; set; }

        /// <summary>
        /// Date d'attribution de la catégorie au client
        /// </summary>
        public DateTime DateAttribution { get; set; } = DateTime.Now;

        // Navigation properties
        [JsonIgnore]
        [ForeignKey("IdClient")]
        public Client? Client { get; set; }

        // Les fonctionnalités de catégorie client ne sont plus disponibles après la refactorisation
    }
}
