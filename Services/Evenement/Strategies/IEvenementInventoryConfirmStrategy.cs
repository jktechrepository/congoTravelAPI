using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Services.Evenement.Strategies
{
    public interface IEvenementInventoryConfirmStrategy
    {
        EvenementInventoryMode SupportedMode { get; }

        Task ConfirmHoldAsync(
            EvenementInventoryConfirmRequest request,
            CancellationToken cancellationToken = default);
    }
}
