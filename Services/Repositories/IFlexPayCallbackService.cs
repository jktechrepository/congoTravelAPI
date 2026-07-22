using CongoTravel.Models.DTOs.FlexPay;

namespace CongoTravel.Services.Repositories
{
    public interface IFlexPayCallbackService
    {
        Task<FlexPayCallbackProcessResultDto> ProcessCallbackAsync(
            FlexPayCallbackDto callback,
            string? payloadComplet,
            string? headers,
            string? ipSource,
            CancellationToken cancellationToken = default);

        Task<FlexPayVerifierResultDto> VerifyAndFinalizeAsync(
            string orderNumber,
            CancellationToken cancellationToken = default);
    }
}
