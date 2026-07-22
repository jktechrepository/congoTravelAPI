using CongoTravel.Models.Evenement.Enums;

namespace CongoTravel.Services.Evenement.Strategies
{
    public class EvenementInventoryCancelStrategyFactory : IEvenementInventoryCancelStrategyFactory
    {
        private readonly EvenementGlobalQuotaCancelStrategy _globalQuotaStrategy;
        private readonly EvenementClassQuotaCancelStrategy _classQuotaStrategy;
        private readonly EvenementSeatNumberedCancelStrategy _seatNumberedStrategy;

        public EvenementInventoryCancelStrategyFactory(
            EvenementGlobalQuotaCancelStrategy globalQuotaStrategy,
            EvenementClassQuotaCancelStrategy classQuotaStrategy,
            EvenementSeatNumberedCancelStrategy seatNumberedStrategy)
        {
            _globalQuotaStrategy = globalQuotaStrategy;
            _classQuotaStrategy = classQuotaStrategy;
            _seatNumberedStrategy = seatNumberedStrategy;
        }

        public IEvenementInventoryCancelStrategy GetStrategy(EvenementInventoryMode inventoryMode) =>
            inventoryMode switch
            {
                EvenementInventoryMode.GlobalQuota => _globalQuotaStrategy,
                EvenementInventoryMode.ClassQuota => _classQuotaStrategy,
                EvenementInventoryMode.SeatNumbered => _seatNumberedStrategy,
                _ => throw new ArgumentOutOfRangeException(nameof(inventoryMode), inventoryMode, null)
            };
    }
}
