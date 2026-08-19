using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Pagination;

namespace CongoTravel.Services.Repositories
{
    public interface IVoyageRepository
    {
        Task<IEnumerable<Voyage>> GetAllAsync(DateTime? dateDepartDebut = null, DateTime? dateDepartFin = null);
        Task<IEnumerable<Voyage>> GetAllPublicAsync(DateTime? dateDepartDebut = null, DateTime? dateDepartFin = null);
        Task<Voyage?> GetByIdAsync(int id);
        Task<Voyage?> GetByIdPublicAsync(int id);
        Task<Voyage> CreateAsync(Voyage voyage, IReadOnlyList<CreateVoyageEtapeDto>? etapesDestinations = null);
        Task<VoyageCreateResult> TryCreateAsync(
            Voyage voyage,
            IReadOnlyList<CreateVoyageEtapeDto>? etapesDestinations = null,
            VoyageCreateOptions? options = null);
        Task<Voyage?> UpdateAsync(Voyage voyage, IReadOnlyList<CreateVoyageEtapeDto>? etapesDestinations = null);

        /// <summary>Aligne <see cref="Voyage.Prix"/> sur le tarif ECO (ou le minimum des tarifs catégorie).</summary>
        Task SyncVoyagePrixReferenceFromTarifsAsync(int idVoyage, CancellationToken cancellationToken = default);

        /// <summary>
        /// Vérifie qu'une modification de <see cref="Voyage.Prix"/> sans <c>tarifs[]</c> explicites est autorisée.
        /// </summary>
        Task EnsurePrixUpdateAllowedAsync(
            int idVoyage,
            int nouveauPrix,
            bool tarifsFournis,
            CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id);

        Task<IEnumerable<Voyage>> GetByVehiculeAsync(int idVehicule, DateTime? dateDepartDebut = null, DateTime? dateDepartFin = null);
        Task<IEnumerable<Voyage>> GetBySocieteAsync(int idSociete, DateTime? dateDepartDebut = null, DateTime? dateDepartFin = null);
        Task<IEnumerable<Voyage>> GetBySocietePublicAsync(int idSociete, DateTime? dateDepartDebut = null, DateTime? dateDepartFin = null);
        Task<IEnumerable<Voyage>> GetBySiteAsync(int idSite, DateTime? dateDepartDebut = null, DateTime? dateDepartFin = null);
        Task<IEnumerable<Voyage>> GetBySitePublicAsync(int idSite, DateTime? dateDepartDebut = null, DateTime? dateDepartFin = null);
        Task<IEnumerable<Voyage>> GetByDestinationAsync(int idDestination, DateTime? dateDepartDebut = null, DateTime? dateDepartFin = null);
        Task<IEnumerable<Voyage>> GetByDestinationPublicAsync(int idDestination, DateTime? dateDepartDebut = null, DateTime? dateDepartFin = null);
        Task<IEnumerable<Voyage>> GetByDateAsync(DateTime date);
        Task<IEnumerable<Voyage>> GetByDatePublicAsync(DateTime date);
        Task<IEnumerable<Voyage>> GetByVehiculeAndDestinationAsync(int idVehicule, int idDestination);
        Task<IEnumerable<Voyage>> GetByVehiculeAndDestinationPublicAsync(int idVehicule, int idDestination);
        Task<IEnumerable<Voyage>> GetByDateRangeAsync(DateTime dateDebut, DateTime dateFin);
        Task<IEnumerable<Voyage>> GetByDateRangePublicAsync(DateTime dateDebut, DateTime dateFin);

        Task<IEnumerable<Voyage>> GetByStatutAsync(bool statut);
        Task<IEnumerable<Voyage>> GetByPriceRangeAsync(int prixMin, int prixMax);
        Task<IEnumerable<Voyage>> GetByPriceRangePublicAsync(int prixMin, int prixMax);

        Task<bool> ExistsAsync(int id);
        Task<bool> ExistsPublicAsync(int id);
        Task<bool> ExistsByVehiculeAndDateAsync(int idVehicule, DateTime date, TimeSpan heure);

        Task<PagedResult<Voyage>> GetPagedAsync(PagedRequest request, DateTime? dateDepartDebut = null, DateTime? dateDepartFin = null);
        Task<PagedResult<Voyage>> GetPagedPublicAsync(PagedRequest request, DateTime? dateDepartDebut = null, DateTime? dateDepartFin = null);
        Task<PagedResult<Voyage>> SearchPagedAsync(
            PagedRequest request,
            string? villeDepart = null,
            string? villeArrivee = null,
            int? idSociete = null,
            DateTime? dateDepartDebut = null,
            DateTime? dateDepartFin = null);
        Task<PagedResult<Voyage>> SearchPagedPublicAsync(
            PagedRequest request,
            string? villeDepart = null,
            string? villeArrivee = null,
            int? idSociete = null,
            DateTime? dateDepartDebut = null,
            DateTime? dateDepartFin = null);
        Task<PagedResult<Voyage>> GetByVehiculePagedAsync(int idVehicule, PagedRequest request, DateTime? dateDepartDebut = null, DateTime? dateDepartFin = null);
        Task<PagedResult<Voyage>> GetByVehiculePagedPublicAsync(int idVehicule, PagedRequest request, DateTime? dateDepartDebut = null, DateTime? dateDepartFin = null);
        Task<PagedResult<Voyage>> GetBySocietePagedAsync(int idSociete, PagedRequest request, DateTime? dateDepartDebut = null, DateTime? dateDepartFin = null);
        Task<PagedResult<Voyage>> GetBySocietePagedPublicAsync(int idSociete, PagedRequest request, DateTime? dateDepartDebut = null, DateTime? dateDepartFin = null);
        Task<PagedResult<Voyage>> GetBySitePagedAsync(int idSite, PagedRequest request, DateTime? dateDepartDebut = null, DateTime? dateDepartFin = null);
        Task<PagedResult<Voyage>> GetBySitePagedPublicAsync(int idSite, PagedRequest request, DateTime? dateDepartDebut = null, DateTime? dateDepartFin = null);
        Task<PagedResult<Voyage>> GetByDestinationPagedAsync(int idDestination, PagedRequest request, DateTime? dateDepartDebut = null, DateTime? dateDepartFin = null);
        Task<PagedResult<Voyage>> GetByDestinationPagedPublicAsync(int idDestination, PagedRequest request, DateTime? dateDepartDebut = null, DateTime? dateDepartFin = null);

        Task<int> CountAsync();
        Task<int> CountByVehiculeAsync(int idVehicule);
        Task<int> CountByDestinationAsync(int idDestination);
        Task<int> CountByDateAsync(DateTime date);
        Task<int> CountByStatutAsync(bool statut);

        Task<IReadOnlyList<VoyageDestination>> GetOrderedDestinationsAsync(int idVoyage);
        Task<IReadOnlyList<VoyageDestination>> GetOrderedDestinationsPublicAsync(int idVoyage);

        Task<IReadOnlyList<Siege>> GetSiegesDisponiblesPourVoyageAsync(int idVoyage);

        Task<VoyageSiegesDisponiblesResponseDto> GetSiegesDisponiblesResponsePourVoyageAsync(int idVoyage);

        /// <summary>
        /// Résumé des sièges libres par catégorie pour plusieurs voyages (une requête groupée par page).
        /// </summary>
        Task<IReadOnlyDictionary<int, List<VoyageCategorieSiegeDisponiblesSummaryDto>>> GetRepartitionSiegesDisponiblesParVoyagesAsync(
            IReadOnlyList<int> idVoyages);

        Task<IReadOnlyList<VoyageSeatAllocation>> GetAllocationsConfirmePourVoyageAsync(int idVoyage);

        /// <summary>
        /// Passagers ayant un enregistrement d’embarquement pour le voyage identifié par destination, véhicule et jour de départ.
        /// Si <paramref name="heureDepart"/> est renseigné, le filtre inclut l’heure de départ du voyage (<see cref="Voyage.HeureDepart"/>).
        /// Échec 404 si aucun voyage ; 400 si plusieurs voyages correspondent encore (sans heure : plusieurs départs le même jour ; avec heure : doublons anormaux).
        /// </summary>
        Task<PassagersEmbarquesQueryResult> GetPassagersEmbarquesPourCriteresVoyageAsync(
            int idDestination,
            int idVehicule,
            DateTime dateDepart,
            TimeSpan? heureDepart = null);
    }
}
