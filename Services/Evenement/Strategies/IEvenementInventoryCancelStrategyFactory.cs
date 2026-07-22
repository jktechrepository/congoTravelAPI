using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Services.Evenement.Strategies
{
    public interface IEvenementInventoryCancelStrategyFactory
    {
        IEvenementInventoryCancelStrategy GetStrategy(EvenementInventoryMode inventoryMode);
    }
}
