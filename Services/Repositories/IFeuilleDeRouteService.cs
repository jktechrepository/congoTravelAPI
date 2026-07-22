using CongoTravel.Models.DTOs.FeuilleDeRoute;
using CongoTravel.Models.DTOs.Pagination;

namespace CongoTravel.Services.Repositories
{
    public interface IFeuilleDeRouteService
    {
        Task<int?> GetVoyageSocieteIdAsync(int idVoyage, CancellationToken cancellationToken = default);

        Task<FeuilleDeRouteDetailDto> GenererAsync(
            int idVoyage,
            int? idUtilisateurGeneration,
            CancellationToken cancellationToken = default);

        Task<FeuilleDeRouteDetailDto?> GetByIdAsync(
            int idFeuilleDeRoute,
            CancellationToken cancellationToken = default);

        Task<PagedResult<FeuilleDeRouteListItemDto>> GetBySocieteAsync(
            int idSociete,
            int? idVoyage,
            DateTime? dateEmbarquement,
            PagedRequest request,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<FeuilleDeRouteListItemDto>> GetByVoyageAsync(
            int idVoyage,
            CancellationToken cancellationToken = default);
    }
}
