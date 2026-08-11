using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Services.SiteTouristique.Strategies
{
    public class SiteTouristiqueInventoryCancelStrategyFactory : ISiteTouristiqueInventoryCancelStrategyFactory
    {
        private readonly SiteTouristiqueGlobalQuotaCancelStrategy _globalQuotaStrategy;
        private readonly SiteTouristiqueClassQuotaCancelStrategy _classQuotaStrategy;

        public SiteTouristiqueInventoryCancelStrategyFactory(
            SiteTouristiqueGlobalQuotaCancelStrategy globalQuotaStrategy,
            SiteTouristiqueClassQuotaCancelStrategy classQuotaStrategy)
        {
            _globalQuotaStrategy = globalQuotaStrategy;
            _classQuotaStrategy = classQuotaStrategy;
        }

        public ISiteTouristiqueInventoryCancelStrategy GetStrategy(SiteTouristiqueInventoryMode inventoryMode) =>
            inventoryMode switch
            {
                SiteTouristiqueInventoryMode.GlobalQuota => _globalQuotaStrategy,
                SiteTouristiqueInventoryMode.ClassQuota => _classQuotaStrategy,
                _ => throw new ArgumentOutOfRangeException(nameof(inventoryMode), inventoryMode, null)
            };
    }
}
