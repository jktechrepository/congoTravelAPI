using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Services.Evenement.Strategies
{
    public interface IEvenementInventoryHoldStrategy
    {
        EvenementInventoryMode SupportedMode { get; }

        Task<EvenementHoldStrategyResult> ReserveHoldAsync(
            EvenementInventoryHoldRequest request,
            CancellationToken cancellationToken = default);
    }
}
