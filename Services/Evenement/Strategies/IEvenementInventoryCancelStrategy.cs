using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Services.Evenement.Strategies
{
    public interface IEvenementInventoryCancelStrategy
    {
        EvenementInventoryMode SupportedMode { get; }

        Task ReleaseReservationAsync(
            EvenementInventoryCancelRequest request,
            CancellationToken cancellationToken = default);
    }
}
