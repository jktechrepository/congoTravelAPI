namespace CongoTravel.Services.Repositories
{
    /// <summary>Notifications temps réel SignalR après callback FlexPay.</summary>
    public interface IFlexPayRealtimeNotifier
    {
        Task NotifyPaymentConfirmedAsync(
            int userId,
            string orderNumber,
            int idReservation,
            int idPaiement,
            CancellationToken cancellationToken = default);

        Task NotifyPaymentFailedAsync(
            int userId,
            string orderNumber,
            string message,
            CancellationToken cancellationToken = default);
    }
}
