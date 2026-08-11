using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Services.SiteTouristique.Strategies
{
    public class SiteTouristiqueInventoryConfirmStrategyFactory : ISiteTouristiqueInventoryConfirmStrategyFactory
    {
        private readonly SiteTouristiqueGlobalQuotaConfirmStrategy _globalQuotaStrategy;
        private readonly SiteTouristiqueClassQuotaConfirmStrategy _classQuotaStrategy;

        public SiteTouristiqueInventoryConfirmStrategyFactory(
            SiteTouristiqueGlobalQuotaConfirmStrategy globalQuotaStrategy,
            SiteTouristiqueClassQuotaConfirmStrategy classQuotaStrategy)
        {
            _globalQuotaStrategy = globalQuotaStrategy;
            _classQuotaStrategy = classQuotaStrategy;
        }

        public ISiteTouristiqueInventoryConfirmStrategy GetStrategy(SiteTouristiqueInventoryMode inventoryMode) =>
            inventoryMode switch
            {
                SiteTouristiqueInventoryMode.GlobalQuota => _globalQuotaStrategy,
                SiteTouristiqueInventoryMode.ClassQuota => _classQuotaStrategy,
                _ => throw new ArgumentOutOfRangeException(nameof(inventoryMode), inventoryMode, null)
            };
    }
}
