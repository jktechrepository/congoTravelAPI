using CongoTravel.Models.Hotel.Enums;

namespace CongoTravel.Services.Hotel.Strategies
{
    public class HotelInventoryCancelStrategyFactory : IHotelInventoryCancelStrategyFactory
    {
        private readonly HotelGlobalQuotaCancelStrategy _globalQuotaStrategy;
        private readonly HotelClassQuotaCancelStrategy _classQuotaStrategy;

        public HotelInventoryCancelStrategyFactory(
            HotelGlobalQuotaCancelStrategy globalQuotaStrategy,
            HotelClassQuotaCancelStrategy classQuotaStrategy)
        {
            _globalQuotaStrategy = globalQuotaStrategy;
            _classQuotaStrategy = classQuotaStrategy;
        }

        public IHotelInventoryCancelStrategy GetStrategy(HotelInventoryMode inventoryMode) =>
            inventoryMode switch
            {
                HotelInventoryMode.GlobalQuota => _globalQuotaStrategy,
                HotelInventoryMode.ClassQuota => _classQuotaStrategy,
                _ => throw new ArgumentOutOfRangeException(nameof(inventoryMode), inventoryMode, null)
            };
    }
}
