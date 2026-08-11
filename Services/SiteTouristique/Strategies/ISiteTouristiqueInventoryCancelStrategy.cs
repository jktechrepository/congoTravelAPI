using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Services.SiteTouristique.Strategies
{
    public interface ISiteTouristiqueInventoryCancelStrategy
    {
        SiteTouristiqueInventoryMode SupportedMode { get; }

        Task ReleaseReservationAsync(
            SiteTouristiqueInventoryCancelRequest request,
            CancellationToken cancellationToken = default);
    }
}
