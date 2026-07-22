using CongoTravel.Models.DTOs.Evenement;

namespace CongoTravel.Services.Evenement
{
    public interface IEvenementHoldService
    {
        Task<EvenementHoldResponseDto> CreateHoldAsync(
            int idEvenementSession,
            int idSociete,
            EvenementHoldRequestDto request,
            CancellationToken cancellationToken = default);
    }
}
