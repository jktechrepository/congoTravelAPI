using CongoTravel.Models.Hotel.Enums;

namespace CongoTravel.Services.Hotel.Strategies
{
    public interface IHotelInventoryHoldStrategyFactory
    {
        IHotelInventoryHoldStrategy GetStrategy(HotelInventoryMode inventoryMode);
    }
}
