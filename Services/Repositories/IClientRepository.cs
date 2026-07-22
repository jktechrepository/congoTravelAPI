using System.Collections;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Models.DTOs;

namespace CongoTravel.Services.Repositories
{
    /// <summary>
    /// Interface du repository pour la gestion des clients
    /// </summary>
    public interface IClientRepository
    {
        /// <summary>
        /// Récupérer tous les clients
        /// </summary>
        Task<IEnumerable<Client>> GetAllAsync();
        
        Task<IEnumerable<Client>> GetBySocieteAndSearchAsync(int idSociete, string SearchTerm, bool IncludeInactive);

        
        Task<PagedResult<Client>> GetBySocietePagedAsync(int idSociete, ClientPagedSearchRequestDto request);
        /// <summary>
        /// Récupérer un client par son ID
        /// </summary>
        Task<Client?> GetByIdAsync(int id);
        
        Task<IEnumerable<Client>> GetBySocieteAsync(int idSociete);

        /// <summary>
        /// Créer un nouveau client
        /// </summary>
        Task<Client> CreateAsync(Client client);

        /// <summary>
        /// Mettre à jour un client existant
        /// </summary>
        Task<Client> UpdateAsync(Client client);

        /// <summary>
        /// Supprimer un client (soft delete)
        /// </summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Récupérer les clients avec pagination, recherche et filtres (liste globale)
        /// </summary>
        Task<PagedResult<Client>> GetPagedAsync(ClientPagedSearchRequestDto request);

        /// <summary>
        /// Rechercher des clients par terme
        /// </summary>
        Task<IEnumerable<Client>> SearchAsync(string searchTerm);
        
        Task<IEnumerable<Client>> GetByNomAsync(string nom);

        /// <summary>
        /// Vérifier si un email existe déjà
        /// </summary>
        Task<bool> EmailExistsAsync(string email, int? excludeId = null);

        /// <summary>
        /// Récupérer un client par son email
        /// </summary>
        Task<Client?> GetByEmailAsync(string email);

        /// <summary>
        /// Récupérer le nombre total de clients
        /// </summary>
        Task<int> GetTotalCountAsync();
        
        Task<bool> ToggleStatutAsync(int id);
         Task<bool> SetStatutAsync(int id, bool statut);
         
         Task<bool> ExistsAsync(int id);
    }
}
