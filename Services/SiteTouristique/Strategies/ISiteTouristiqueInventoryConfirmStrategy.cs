using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Services.SiteTouristique.Strategies
{
    public interface ISiteTouristiqueInventoryConfirmStrategy
    {
        SiteTouristiqueInventoryMode SupportedMode { get; }

        Task ConfirmHoldAsync(
            SiteTouristiqueInventoryConfirmRequest request,
            CancellationToken cancellationToken = default);
    }
}
