using CongoTravel.Helpers;
using CongoTravel.Models.DTOs.Pagination;
using CongoTravel.Models.DTOs.ReversementSite;

namespace CongoTravel.Services
{
    public interface IReversementSiteService
    {
        Task<ReversementSiteResponseDto> InitierAsync(
            InitierReversementSiteDto dto,
            int idUtilisateur,
            CancellationToken cancellationToken = default);

        Task<ReversementSiteResponseDto?> InitierPourPaiementAsync(
            int idPaiement,
            int idReservation,
            int idSite,
            int idSociete,
            int idUtilisateur,
            decimal montant,
            string codeDevise,
            string? motif,
            CancellationToken cancellationToken = default);

        Task<ReversementSiteResponseDto?> GetByIdAsync(
            int id,
            int idSociete,
            bool isSuperAdmin,
            CancellationToken cancellationToken = default);

        Task<PagedResponse<ReversementSiteResponseDto>> GetBySitePagedAsync(
            int idSite,
            int idSociete,
            PagedRequest request,
            bool isSuperAdmin,
            CancellationToken cancellationToken = default);

        Task<ReversementSiteResponseDto> VerifierEtFinaliserAsync(
            string orderNumber,
            int idSociete,
            bool isSuperAdmin,
            CancellationToken cancellationToken = default);
    }
}
