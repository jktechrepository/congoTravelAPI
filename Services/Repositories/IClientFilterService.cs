using CongoTravel.Models;
using CongoTravel.Models.DTOs.Communication;

namespace CongoTravel.Services.Repositories
{
    /// <summary>
    /// Interface pour le service de filtrage des clients selon des critères
    /// </summary>
    public interface IClientFilterService
    {
        /// <summary>
        /// Récupère les clients correspondant aux critères de ciblage
        /// Note: Toujours filtre par Statut = true (clients actifs uniquement)
        /// </summary>
        Task<List<Client>> GetClientsByCriteriaAsync(CriteresCiblageDto? criteres);
    }
}

