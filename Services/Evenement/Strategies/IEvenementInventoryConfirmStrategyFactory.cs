using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Services.Evenement.Strategies
{
    public interface IEvenementInventoryConfirmStrategyFactory
    {
        IEvenementInventoryConfirmStrategy GetStrategy(EvenementInventoryMode inventoryMode);
    }
}
