using CongoTravel.Configuration;

namespace CongoTravel.Helpers
{
    public static class FlexPayCurrencyPolicy
    {
        private static readonly string[] DefaultSupportedCurrencies = { "CDF", "USD" };

        public static string NormalizePaymentCurrencyOrThrow(string? currency, string contextMessage)
        {
            var normalized = string.IsNullOrWhiteSpace(currency)
                ? string.Empty
                : currency.Trim().ToUpperInvariant();

            if (normalized is "CDF" or "USD")
                return normalized;

            throw new InvalidOperationException($"{contextMessage} n'accepte que CDF ou USD comme devise de paiement.");
        }

        public static void EnsureChannelCurrencySupported(
            FlexPayOptions options,
            string methodePaiement,
            string paymentCurrency,
            string contextMessage)
        {
            var supported = IsCard(methodePaiement)
                ? NormalizeCurrencyList(options.CardSupportedCurrencies)
                : NormalizeCurrencyList(options.MobileMoneySupportedCurrencies);

            if (!supported.Contains(paymentCurrency, StringComparer.Ordinal))
            {
                var list = string.Join(", ", supported);
                throw new InvalidOperationException(
                    $"{contextMessage} n'autorise pas la devise {paymentCurrency} pour {methodePaiement}. Devises autorisées: {list}.");
            }
        }

        public static void EnsureCallbackCurrencyMatchesExpected(
            string? callbackCurrency,
            string expectedCurrency,
            string contextMessage)
        {
            if (string.IsNullOrWhiteSpace(callbackCurrency))
                return;

            var normalized = callbackCurrency.Trim().ToUpperInvariant();
            if (!string.Equals(normalized, expectedCurrency, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"{contextMessage}: devise callback {normalized} incohérente avec la devise attendue {expectedCurrency}.");
            }
        }

        private static bool IsCard(string methodePaiement) =>
            string.Equals(methodePaiement, MethodePaiementHelper.CarteBancaire, StringComparison.Ordinal);

        private static IReadOnlyList<string> NormalizeCurrencyList(IEnumerable<string>? source)
        {
            if (source == null)
                return DefaultSupportedCurrencies;

            var normalized = source
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim().ToUpperInvariant())
                .Distinct(StringComparer.Ordinal)
                .ToList();

            return normalized.Count == 0 ? DefaultSupportedCurrencies : normalized;
        }
    }
}
