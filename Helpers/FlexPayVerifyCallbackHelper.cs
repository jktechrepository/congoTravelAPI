using System.Globalization;
using CongoTravel.Models.DTOs.FlexPay;

namespace CongoTravel.Helpers
{
    public static class FlexPayVerifyCallbackHelper
    {
        public static FlexPayCallbackDto BuildSyntheticCallback(
            FlexPayCheckResponseDto check,
            string orderNumber,
            decimal expectedAmount,
            string expectedCurrency,
            string callbackCode)
        {
            var transaction = check.Transaction;
            var amount = !string.IsNullOrWhiteSpace(transaction?.Amount)
                ? transaction!.Amount!
                : expectedAmount.ToString(CultureInfo.InvariantCulture);
            var currency = !string.IsNullOrWhiteSpace(transaction?.Currency)
                ? transaction!.Currency
                : expectedCurrency;

            return new FlexPayCallbackDto
            {
                Code = callbackCode,
                OrderNumber = orderNumber.Trim(),
                Amount = amount,
                Currency = currency
            };
        }
    }
}
