using CongoTravel.Models.Hotel.Enums;

namespace CongoTravel.Services.Hotel.Strategies
{
    public interface IHotelInventoryCancelStrategyFactory
    {
        IHotelInventoryCancelStrategy GetStrategy(HotelInventoryMode inventoryMode);
    }
}
