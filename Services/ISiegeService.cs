using CongoTravel.Models.DTOs;

namespace CongoTravel.Services
{
    /// <summary>
    /// Maintient les sièges (1 à NombreSiege) pour un véhicule et synchronise les codes au format AliasVehicule/n.
    /// </summary>
    public interface ISiegeService
    {
        /// <summary>
        /// Crée les sièges manquants pour les ordres 1 à NombreSiege et met à jour les codes AliasVehicule/n.
        /// </summary>
        Task EnsureSeatsForVehiculeAsync(int idVehicule, CancellationToken cancellationToken = default);

        /// <summary>
        /// Crée/synchronise les sièges pour un véhicule en appliquant une répartition par catégorie de siège.
        /// </summary>
        Task EnsureSeatsForVehiculeWithCategorieDistributionAsync(
            int idVehicule,
            IReadOnlyList<(int IdCategorieSiege, int NombreSiegeParCategorie)>? distribution,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Répartition des sièges actifs par catégorie pour chaque véhicule demandé.
        /// </summary>
        Task<IReadOnlyDictionary<int, List<VehiculeCategorieSiegeRepartitionDto>>> GetActiveRepartitionByVehiculeIdsAsync(
            IReadOnlyCollection<int> idVehicules,
            CancellationToken cancellationToken = default);
    }
}
