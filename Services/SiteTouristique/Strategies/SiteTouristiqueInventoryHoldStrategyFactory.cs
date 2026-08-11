using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Services.SiteTouristique.Strategies
{
    public class SiteTouristiqueInventoryHoldStrategyFactory : ISiteTouristiqueInventoryHoldStrategyFactory
    {
        private readonly SiteTouristiqueGlobalQuotaHoldStrategy _globalQuotaStrategy;
        private readonly SiteTouristiqueClassQuotaHoldStrategy _classQuotaStrategy;

        public SiteTouristiqueInventoryHoldStrategyFactory(
            SiteTouristiqueGlobalQuotaHoldStrategy globalQuotaStrategy,
            SiteTouristiqueClassQuotaHoldStrategy classQuotaStrategy)
        {
            _globalQuotaStrategy = globalQuotaStrategy;
            _classQuotaStrategy = classQuotaStrategy;
        }

        public ISiteTouristiqueInventoryHoldStrategy GetStrategy(SiteTouristiqueInventoryMode inventoryMode) =>
            inventoryMode switch
            {
                SiteTouristiqueInventoryMode.GlobalQuota => _globalQuotaStrategy,
                SiteTouristiqueInventoryMode.ClassQuota => _classQuotaStrategy,
                _ => throw new ArgumentOutOfRangeException(nameof(inventoryMode), inventoryMode, null)
            };
    }
}
