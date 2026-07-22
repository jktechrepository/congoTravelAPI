using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Services.Evenement.Strategies
{
    public class EvenementInventoryHoldStrategyFactory : IEvenementInventoryHoldStrategyFactory
    {
        private readonly EvenementGlobalQuotaHoldStrategy _globalQuotaStrategy;
        private readonly EvenementClassQuotaHoldStrategy _classQuotaStrategy;
        private readonly EvenementSeatNumberedHoldStrategy _seatNumberedStrategy;

        public EvenementInventoryHoldStrategyFactory(
            EvenementGlobalQuotaHoldStrategy globalQuotaStrategy,
            EvenementClassQuotaHoldStrategy classQuotaStrategy,
            EvenementSeatNumberedHoldStrategy seatNumberedStrategy)
        {
            _globalQuotaStrategy = globalQuotaStrategy;
            _classQuotaStrategy = classQuotaStrategy;
            _seatNumberedStrategy = seatNumberedStrategy;
        }

        public IEvenementInventoryHoldStrategy GetStrategy(EvenementInventoryMode inventoryMode) =>
            inventoryMode switch
            {
                EvenementInventoryMode.GlobalQuota => _globalQuotaStrategy,
                EvenementInventoryMode.ClassQuota => _classQuotaStrategy,
                EvenementInventoryMode.SeatNumbered => _seatNumberedStrategy,
                _ => throw new ArgumentOutOfRangeException(nameof(inventoryMode), inventoryMode, null)
            };
    }
}
