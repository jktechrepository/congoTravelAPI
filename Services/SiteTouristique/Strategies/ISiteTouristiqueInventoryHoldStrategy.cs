using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Services.SiteTouristique.Strategies
{
    public interface ISiteTouristiqueInventoryHoldStrategy
    {
        SiteTouristiqueInventoryMode SupportedMode { get; }

        Task<SiteTouristiqueHoldStrategyResult> ReserveHoldAsync(
            SiteTouristiqueInventoryHoldRequest request,
            CancellationToken cancellationToken = default);
    }
}
