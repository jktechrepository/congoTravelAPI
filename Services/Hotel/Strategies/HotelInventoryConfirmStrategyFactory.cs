using CongoTravel.Models.Hotel.Enums;

namespace CongoTravel.Services.Hotel.Strategies
{
    public class HotelInventoryConfirmStrategyFactory : IHotelInventoryConfirmStrategyFactory
    {
        private readonly HotelGlobalQuotaConfirmStrategy _globalQuotaStrategy;
        private readonly HotelClassQuotaConfirmStrategy _classQuotaStrategy;

        public HotelInventoryConfirmStrategyFactory(
            HotelGlobalQuotaConfirmStrategy globalQuotaStrategy,
            HotelClassQuotaConfirmStrategy classQuotaStrategy)
        {
            _globalQuotaStrategy = globalQuotaStrategy;
            _classQuotaStrategy = classQuotaStrategy;
        }

        public IHotelInventoryConfirmStrategy GetStrategy(HotelInventoryMode inventoryMode) =>
            inventoryMode switch
            {
                HotelInventoryMode.GlobalQuota => _globalQuotaStrategy,
                HotelInventoryMode.ClassQuota => _classQuotaStrategy,
                _ => throw new ArgumentOutOfRangeException(nameof(inventoryMode), inventoryMode, null)
            };
    }
}
