using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Services.Evenement.Strategies
{
    public interface IEvenementInventoryHoldStrategyFactory
    {
        IEvenementInventoryHoldStrategy GetStrategy(EvenementInventoryMode inventoryMode);
    }
}
