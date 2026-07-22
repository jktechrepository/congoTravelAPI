using CongoTravel.Models.DTOs.FlexPay;
using CongoTravel.Models.DTOs.ReversementSite;

namespace CongoTravel.Services
{
    public interface IFlexPayPayOutCallbackService
    {
        Task<FlexPayPayOutCallbackProcessResultDto> ProcessCallbackAsync(
            FlexPayCallbackDto callback,
            string? payloadComplet,
            string? headers,
            string? ipSource,
            CancellationToken cancellationToken = default);
    }
}
