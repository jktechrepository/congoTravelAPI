using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Services.Evenement.Strategies
{
    public class EvenementInventoryConfirmStrategyFactory : IEvenementInventoryConfirmStrategyFactory
    {
        private readonly EvenementGlobalQuotaConfirmStrategy _globalQuotaStrategy;
        private readonly EvenementClassQuotaConfirmStrategy _classQuotaStrategy;
        private readonly EvenementSeatNumberedConfirmStrategy _seatNumberedStrategy;

        public EvenementInventoryConfirmStrategyFactory(
            EvenementGlobalQuotaConfirmStrategy globalQuotaStrategy,
            EvenementClassQuotaConfirmStrategy classQuotaStrategy,
            EvenementSeatNumberedConfirmStrategy seatNumberedStrategy)
        {
            _globalQuotaStrategy = globalQuotaStrategy;
            _classQuotaStrategy = classQuotaStrategy;
            _seatNumberedStrategy = seatNumberedStrategy;
        }

        public IEvenementInventoryConfirmStrategy GetStrategy(EvenementInventoryMode inventoryMode) =>
            inventoryMode switch
            {
                EvenementInventoryMode.GlobalQuota => _globalQuotaStrategy,
                EvenementInventoryMode.ClassQuota => _classQuotaStrategy,
                EvenementInventoryMode.SeatNumbered => _seatNumberedStrategy,
                _ => throw new ArgumentOutOfRangeException(nameof(inventoryMode), inventoryMode, null)
            };
    }
}
