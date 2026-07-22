using CongoTravel.Models.DTOs.FlexPay;

namespace CongoTravel.Services
{
    public interface IFlexPayService
    {
        Task<FlexPayPaymentResponseDto> InitierPaiementMobileMoneyAsync(
            string merchant,
            string apiToken,
            string reference,
            string phone,
            decimal amount,
            string currency,
            string callbackUrl,
            CancellationToken cancellationToken = default);

        Task<FlexPayPaymentResponseDto> InitierPaiementCarteV1Async(
            string merchant,
            string apiToken,
            string reference,
            decimal amount,
            string currency,
            string description,
            string callbackUrl,
            string approveUrl,
            string cancelUrl,
            string declineUrl,
            CancellationToken cancellationToken = default);

        Task<FlexPayCheckResponseDto> VerifierStatutTransactionAsync(
            string apiToken,
            string orderNumber,
            CancellationToken cancellationToken = default);

        Task<FlexPayPaymentResponseDto> InitierPayOutAsync(
            string merchant,
            string apiToken,
            string reference,
            string phone,
            decimal amount,
            string currency,
            string callbackUrl,
            CancellationToken cancellationToken = default);
    }
}
