using CongoTravel.Models;

namespace CongoTravel.Services.Repositories
{
    /// <summary>
    /// Tarifs voyage × catégorie de siège et résolution du prix par siège.
    /// </summary>
    public interface IVoyageTarifService
    {
        Task<int> ResolvePrixAsync(int idVoyage, int idCategorieSiege, int prixFallbackVoyage, CancellationToken cancellationToken = default);

        Task<decimal> ComputeTotalForSiegesAsync(
            int idVoyage,
            IReadOnlyList<int> idSiegeList,
            int prixFallbackVoyage,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<VoyageTarifCategorieSiege>> GetTarifsByVoyageAsync(int idVoyage, CancellationToken cancellationToken = default);

        Task ReplaceTarifsForVoyageAsync(
            int idVoyage,
            int idSociete,
            IReadOnlyList<(int IdCategorieSiege, int Prix)> lignes,
            CancellationToken cancellationToken = default);

        /// <summary>Crée ou met à jour le tarif d'une catégorie pour un voyage.</summary>
        Task<VoyageTarifCategorieSiege> UpsertTarifForVoyageAsync(
            int idVoyage,
            int idSociete,
            int idCategorieSiege,
            int prix,
            CancellationToken cancellationToken = default);

        /// <summary>Indique si le voyage possède au moins une ligne tarif catégorie.</summary>
        Task<bool> HasTarifsForVoyageAsync(int idVoyage, CancellationToken cancellationToken = default);

        /// <summary>Après création de voyage : une ligne tarif ECO = <paramref name="prixVoyage"/> si aucune ligne.</summary>
        Task EnsureDefaultEcoTarifAsync(int idVoyage, int idSociete, int prixVoyage, CancellationToken cancellationToken = default);

        /// <summary>Met à jour le tarif ECO existant quand <see cref="Voyage.Prix"/> change.</summary>
        Task SyncEcoTarifPrixAsync(int idVoyage, int idSociete, int nouveauPrixVoyage, CancellationToken cancellationToken = default);

        /// <summary>
        /// Ajuste tous les tarifs catégorie quand <see cref="Voyage.Prix"/> change sans fournir <c>tarifs</c> explicites.
        /// Conservé pour migration de données ; ne plus appeler depuis les flux API standard.
        /// </summary>
        [Obsolete("Utiliser UpsertTarifForVoyageAsync ou ReplaceTarifsForVoyageAsync par catégorie.")]
        Task SyncTarifsWhenVoyagePrixChangesAsync(
            int idVoyage,
            int idSociete,
            int ancienPrixVoyage,
            int nouveauPrixVoyage,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Prix de référence affichable : tarif ECO s'il existe, sinon minimum des tarifs, sinon fallback voyage.
        /// </summary>
        Task<int> ResolveReferencePrixFromTarifsAsync(
            int idVoyage,
            int idSociete,
            int prixFallbackVoyage,
            CancellationToken cancellationToken = default);
    }
}
