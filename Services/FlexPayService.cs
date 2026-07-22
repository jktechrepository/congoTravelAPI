using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CongoTravel.Configuration;
using CongoTravel.Models.DTOs.FlexPay;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CongoTravel.Services
{
    public class FlexPayService : IFlexPayService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly FlexPayOptions _options;
        private readonly ILogger<FlexPayService> _logger;

        public FlexPayService(
            IHttpClientFactory httpClientFactory,
            IOptions<FlexPayOptions> options,
            ILogger<FlexPayService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
            _logger = logger;
        }

        public Task<FlexPayPaymentResponseDto> InitierPaiementMobileMoneyAsync(
            string merchant,
            string apiToken,
            string reference,
            string phone,
            decimal amount,
            string currency,
            string callbackUrl,
            CancellationToken cancellationToken = default)
        {
            var amountStr = currency.Equals("CDF", StringComparison.OrdinalIgnoreCase)
                ? Math.Round(amount, 0, MidpointRounding.AwayFromZero).ToString("0")
                : amount.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

            var body = new Dictionary<string, object?>
            {
                ["merchant"] = merchant,
                ["type"] = "1",
                ["reference"] = reference,
                ["phone"] = phone,
                ["amount"] = amountStr,
                ["currency"] = currency.ToUpperInvariant(),
                ["callbackUrl"] = callbackUrl,
                ["return_url"] = callbackUrl
            };

            return PostPaymentAsync(apiToken, _options.MobileMoneyUrl!, body, cancellationToken);
        }

        public Task<FlexPayPaymentResponseDto> InitierPayOutAsync(
            string merchant,
            string apiToken,
            string reference,
            string phone,
            decimal amount,
            string currency,
            string callbackUrl,
            CancellationToken cancellationToken = default)
        {
            var amountStr = currency.Equals("CDF", StringComparison.OrdinalIgnoreCase)
                ? Math.Round(amount, 0, MidpointRounding.AwayFromZero).ToString("0")
                : amount.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

            var body = new Dictionary<string, object?>
            {
                ["merchant"] = merchant,
                ["type"] = "1",
                ["reference"] = reference,
                ["phone"] = phone,
                ["amount"] = amountStr,
                ["currency"] = currency.ToUpperInvariant(),
                ["callbackUrl"] = callbackUrl
            };

            return PostPaymentAsync(apiToken, _options.PayOutUrl!, body, cancellationToken);
        }

        public Task<FlexPayPaymentResponseDto> InitierPaiementCarteV1Async(
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
            CancellationToken cancellationToken = default)
        {
            var token = NormalizeBearer(apiToken);
            var body = new Dictionary<string, object?>
            {
                ["authorization"] = token,
                ["merchant"] = merchant,
                ["reference"] = reference,
                ["amount"] = amount,
                ["currency"] = currency.ToUpperInvariant(),
                ["description"] = description,
                ["callback_url"] = callbackUrl,
                ["approve_url"] = approveUrl,
                ["cancel_url"] = cancelUrl,
                ["decline_url"] = declineUrl
            };

            return PostPaymentAsync(apiToken, _options.CardPaymentUrl!, body, cancellationToken);
        }

        public async Task<FlexPayCheckResponseDto> VerifierStatutTransactionAsync(
            string apiToken,
            string orderNumber,
            CancellationToken cancellationToken = default)
        {
            var baseUrl = (_options.CheckTransactionUrl ?? string.Empty).TrimEnd('/');
            var url = $"{baseUrl}/{Uri.EscapeDataString(orderNumber)}";
            var client = CreateClient(apiToken);
            var response = await client.GetAsync(url, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("FlexPay check {OrderNumber} — HTTP {Status}: {Body}", orderNumber, (int)response.StatusCode, raw);

            return JsonSerializer.Deserialize<FlexPayCheckResponseDto>(raw, JsonOptions)
                   ?? new FlexPayCheckResponseDto { Code = "1", Message = "Réponse check invalide" };
        }

        private async Task<FlexPayPaymentResponseDto> PostPaymentAsync(
            string apiToken,
            string url,
            Dictionary<string, object?> body,
            CancellationToken cancellationToken)
        {
            var client = CreateClient(apiToken);
            var json = JsonSerializer.Serialize(body);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content, cancellationToken);
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("FlexPay POST {Url} — HTTP {Status}: {Body}", url, (int)response.StatusCode, raw);

            return JsonSerializer.Deserialize<FlexPayPaymentResponseDto>(raw, JsonOptions)
                   ?? new FlexPayPaymentResponseDto { Code = "1", Message = "Réponse FlexPay invalide" };
        }

        private HttpClient CreateClient(string apiToken)
        {
            var client = _httpClientFactory.CreateClient("FlexPay");
            var bare = NormalizeBearer(apiToken).Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bare);
            return client;
        }

        private static string NormalizeBearer(string apiToken)
        {
            var t = apiToken.Trim();
            return t.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? t : $"Bearer {t}";
        }
    }
}
