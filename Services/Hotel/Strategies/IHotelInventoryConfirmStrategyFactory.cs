using CongoTravel.Models.Hotel.Enums;

namespace CongoTravel.Services.Hotel.Strategies
{
    public interface IHotelInventoryConfirmStrategyFactory
    {
        IHotelInventoryConfirmStrategy GetStrategy(HotelInventoryMode inventoryMode);
    }
}
