using CongoTravel.Models.Restaurant.Enums;

namespace CongoTravel.Services.Restaurant.Strategies
{
    public interface IRestaurantInventoryHoldStrategy
    {
        RestaurantInventoryMode SupportedMode { get; }

        Task<RestaurantHoldStrategyResult> ReserveHoldAsync(
            RestaurantInventoryHoldRequest request,
            CancellationToken cancellationToken = default);
    }

    public interface IRestaurantInventoryConfirmStrategy
    {
        RestaurantInventoryMode SupportedMode { get; }

        Task ConfirmHoldAsync(
            RestaurantInventoryConfirmRequest request,
            CancellationToken cancellationToken = default);
    }

    public interface IRestaurantInventoryCancelStrategy
    {
        RestaurantInventoryMode SupportedMode { get; }

        Task ReleaseReservationAsync(
            RestaurantInventoryCancelRequest request,
            CancellationToken cancellationToken = default);
    }

    public interface IRestaurantInventoryHoldStrategyFactory
    {
        IRestaurantInventoryHoldStrategy GetStrategy(RestaurantInventoryMode inventoryMode);
    }

    public interface IRestaurantInventoryConfirmStrategyFactory
    {
        IRestaurantInventoryConfirmStrategy GetStrategy(RestaurantInventoryMode inventoryMode);
    }

    public interface IRestaurantInventoryCancelStrategyFactory
    {
        IRestaurantInventoryCancelStrategy GetStrategy(RestaurantInventoryMode inventoryMode);
    }
}
