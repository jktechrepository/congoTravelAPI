using CongoTravel.Models.Hotel.Enums;

namespace CongoTravel.Services.Hotel.Strategies
{
    public class HotelInventoryHoldStrategyFactory : IHotelInventoryHoldStrategyFactory
    {
        private readonly HotelGlobalQuotaHoldStrategy _globalQuotaStrategy;
        private readonly HotelClassQuotaHoldStrategy _classQuotaStrategy;

        public HotelInventoryHoldStrategyFactory(
            HotelGlobalQuotaHoldStrategy globalQuotaStrategy,
            HotelClassQuotaHoldStrategy classQuotaStrategy)
        {
            _globalQuotaStrategy = globalQuotaStrategy;
            _classQuotaStrategy = classQuotaStrategy;
        }

        public IHotelInventoryHoldStrategy GetStrategy(HotelInventoryMode inventoryMode) =>
            inventoryMode switch
            {
                HotelInventoryMode.GlobalQuota => _globalQuotaStrategy,
                HotelInventoryMode.ClassQuota => _classQuotaStrategy,
                _ => throw new ArgumentOutOfRangeException(nameof(inventoryMode), inventoryMode, null)
            };
    }
}
