using System.ComponentModel.DataAnnotations;

namespace CongoTravel.Models.DTOs.Sync
{
    /// <summary>
    /// DTO pour la synchronisation des clients (projection optimisée)
    /// Contient uniquement les champs nécessaires pour le mode offline
    /// </summary>
    public class ClientSyncDto
    {
        /// <summary>
        /// Identifiant unique du client
        /// </summary>
        public int IdClient { get; set; }

        /// <summary>
        /// Nom du client
        /// </summary>
        public string NomClient { get; set; } = string.Empty;

        /// <summary>
        /// Adresse complète du client
        /// </summary>
        public string? AdresseClient { get; set; }

        /// <summary>
        /// Numéro de téléphone du client
        /// </summary>
        public string? Telephone { get; set; }

        /// <summary>
        /// Email du client
        /// </summary>
        public string? EmailClient { get; set; }

        // Les fonctionnalités de CodeCons ne sont plus disponibles après la refactorisation

        /// <summary>
        /// Genre du client (M, F, Autre)
        /// </summary>
        public string? GenreClient { get; set; }

        // Les fonctionnalités d'axe ne sont plus disponibles après la refactorisation

        // Les fonctionnalités de cabine ne sont plus disponibles après la refactorisation

        /// <summary>
        /// Identifiant de la société (via relation indirecte Axe->Cabine->Societe)
        /// </summary>
        public int IdSociete { get; set; }

        /// <summary>
        /// Catégorie principale du client (via premier usage)
        /// </summary>
        public int? IdCategorieClient { get; set; }

        /// <summary>
        /// Indique si le client est actif (champ métier)
        /// </summary>
        public bool IsActif { get; set; }

        /// <summary>
        /// Statut du client (true = actif, false = supprimé)
        /// </summary>
        public bool Statut { get; set; }

        /// <summary>
        /// Indique si le client est supprimé (soft delete pour sync)
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        /// Date de dernière modification (pour delta sync)
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}
