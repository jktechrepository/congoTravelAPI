using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Services.SiteTouristique.Strategies
{
    public interface ISiteTouristiqueInventoryHoldStrategyFactory
    {
        ISiteTouristiqueInventoryHoldStrategy GetStrategy(SiteTouristiqueInventoryMode inventoryMode);
    }
}
