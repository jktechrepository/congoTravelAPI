using CongoTravel.Models.Restaurant.Enums;

namespace CongoTravel.Services.Restaurant.Strategies
{
    public class RestaurantInventoryHoldStrategyFactory : IRestaurantInventoryHoldStrategyFactory
    {
        private readonly RestaurantGlobalQuotaHoldStrategy _globalQuotaStrategy;
        private readonly RestaurantClassQuotaHoldStrategy _classQuotaStrategy;

        public RestaurantInventoryHoldStrategyFactory(
            RestaurantGlobalQuotaHoldStrategy globalQuotaStrategy,
            RestaurantClassQuotaHoldStrategy classQuotaStrategy)
        {
            _globalQuotaStrategy = globalQuotaStrategy;
            _classQuotaStrategy = classQuotaStrategy;
        }

        public IRestaurantInventoryHoldStrategy GetStrategy(RestaurantInventoryMode inventoryMode) =>
            inventoryMode switch
            {
                RestaurantInventoryMode.GlobalQuota => _globalQuotaStrategy,
                RestaurantInventoryMode.ClassQuota => _classQuotaStrategy,
                _ => throw new ArgumentOutOfRangeException(nameof(inventoryMode), inventoryMode, null)
            };
    }

    public class RestaurantInventoryConfirmStrategyFactory : IRestaurantInventoryConfirmStrategyFactory
    {
        private readonly RestaurantGlobalQuotaConfirmStrategy _globalQuotaStrategy;
        private readonly RestaurantClassQuotaConfirmStrategy _classQuotaStrategy;

        public RestaurantInventoryConfirmStrategyFactory(
            RestaurantGlobalQuotaConfirmStrategy globalQuotaStrategy,
            RestaurantClassQuotaConfirmStrategy classQuotaStrategy)
        {
            _globalQuotaStrategy = globalQuotaStrategy;
            _classQuotaStrategy = classQuotaStrategy;
        }

        public IRestaurantInventoryConfirmStrategy GetStrategy(RestaurantInventoryMode inventoryMode) =>
            inventoryMode switch
            {
                RestaurantInventoryMode.GlobalQuota => _globalQuotaStrategy,
                RestaurantInventoryMode.ClassQuota => _classQuotaStrategy,
                _ => throw new ArgumentOutOfRangeException(nameof(inventoryMode), inventoryMode, null)
            };
    }

    public class RestaurantInventoryCancelStrategyFactory : IRestaurantInventoryCancelStrategyFactory
    {
        private readonly RestaurantGlobalQuotaCancelStrategy _globalQuotaStrategy;
        private readonly RestaurantClassQuotaCancelStrategy _classQuotaStrategy;

        public RestaurantInventoryCancelStrategyFactory(
            RestaurantGlobalQuotaCancelStrategy globalQuotaStrategy,
            RestaurantClassQuotaCancelStrategy classQuotaStrategy)
        {
            _globalQuotaStrategy = globalQuotaStrategy;
            _classQuotaStrategy = classQuotaStrategy;
        }

        public IRestaurantInventoryCancelStrategy GetStrategy(RestaurantInventoryMode inventoryMode) =>
            inventoryMode switch
            {
                RestaurantInventoryMode.GlobalQuota => _globalQuotaStrategy,
                RestaurantInventoryMode.ClassQuota => _classQuotaStrategy,
                _ => throw new ArgumentOutOfRangeException(nameof(inventoryMode), inventoryMode, null)
            };
    }
}
