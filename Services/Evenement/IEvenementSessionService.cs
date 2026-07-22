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

        Task<EvenementSessionResponseDto?> GetByIdAsync(
            int idEvenementSession,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<EvenementSessionResponseDto?> GetByCodeAsync(
            string codeSession,
            int idSociete,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<EvenementSessionListItemDto>> ListAsync(
            int idSociete,
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
