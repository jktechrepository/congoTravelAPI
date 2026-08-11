using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Services.SiteTouristique.Strategies
{
    public interface ISiteTouristiqueInventoryConfirmStrategyFactory
    {
        ISiteTouristiqueInventoryConfirmStrategy GetStrategy(SiteTouristiqueInventoryMode inventoryMode);
    }
}
