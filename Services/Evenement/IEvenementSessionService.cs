using CongoTravel.Models.DTOs.Evenement;
using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Services.Evenement
{
    public interface IEvenementSessionService
    {
        Task<EvenementSessionResponseDto> CreateDraftAsync(
            EvenementCreateSessionRequestDto request,
            int idSociete,
            CancellationToken cancellationToken = default);

        /// <param name="idSociete">
        /// Si fourni, filtre multi-tenant. Si null, lookup par PK seul (Super-Admin / lecture croisée).
        /// </param>
        Task<EvenementSessionResponseDto?> GetByIdAsync(
            int idEvenementSession,
            int? idSociete = null,
            CancellationToken cancellationToken = default);

        /// <summary>Détail public : session <c>Published</c> par id, sans filtre société.</summary>
        Task<EvenementSessionResponseDto?> GetPublishedByIdAsync(
            int idEvenementSession,
            CancellationToken cancellationToken = default);

        Task<EvenementSessionResponseDto?> GetByCodeAsync(
            string codeSession,
            int idSociete,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Détail public par code (<c>Published</c>).
        /// Si plusieurs sociétés partagent le code, <paramref name="idSociete"/> est obligatoire.
        /// </summary>
        /// <exception cref="ArgumentException">Code ambigu sans idSociete.</exception>
        Task<EvenementSessionResponseDto?> GetPublishedByCodeAsync(
            string codeSession,
            int? idSociete = null,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<EvenementSessionListItemDto>> ListAsync(
            int idSociete,
            EvenementSessionListFilter? filter = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Catalogue public : sessions <c>Published</c> (toutes sociétés, ou filtrées par <c>IdSociete</c>).
        /// Le filtre statut est ignoré (toujours Published) ; <c>InventoryMode</c> / <c>IdSociete</c> restent applicables.
        /// </summary>
        Task<IReadOnlyList<EvenementSessionListItemDto>> ListPublishedGlobalAsync(
            EvenementSessionListFilter? filter = null,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<EvenementSessionListItemDto>> ListByStatusAsync(
            EvenementSessionStatus status,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<EvenementSessionListItemDto>> ListByInventoryModeAsync(
            EvenementInventoryMode inventoryMode,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<EvenementSessionListItemDto>> ListByDateAsync(
            DateTime date,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<EvenementSessionListItemDto>> ListByDateRangeAsync(
            DateTime dateDebut,
            DateTime dateFin,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<EvenementSessionResponseDto> PublishAsync(
            int idEvenementSession,
            int idSociete,
            CancellationToken cancellationToken = default);
    }
}
