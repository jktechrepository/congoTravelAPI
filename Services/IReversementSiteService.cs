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

        /// <summary>Rétrocompatibilité Transport : idempotence sur <c>IdPaiement</c>.</summary>
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

        /// <summary>Reversement auto multi-module (Transport, Événement, Restaurant, Site touristique).</summary>
        Task<ReversementSiteResponseDto?> InitierPourPaiementAsync(
            string modulePaiement,
            int idPaiementSource,
            int? idReservationSource,
            int idSite,
            int idSociete,
            int idUtilisateur,
            decimal montant,
            string codeDevise,
            string? motif,
            int? idPaiementTransport = null,
            int? idReservationTransport = null,
            string? numeroMobileMoneyBeneficiaireOverride = null,
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
