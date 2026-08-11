using CongoTravel.Models.SiteTouristique.Enums;

namespace CongoTravel.Services.SiteTouristique.Strategies
{
    public interface ISiteTouristiqueInventoryCancelStrategyFactory
    {
        ISiteTouristiqueInventoryCancelStrategy GetStrategy(SiteTouristiqueInventoryMode inventoryMode);
    }
}
