using Microsoft.AspNetCore.Http;
using CongoTravel.Helpers.Evenement;

namespace CongoTravel.Helpers
{
    public static class FlexPayUrlHelper
    {
        public static string ResolveCallbackUrl(
            HttpContext? httpContext,
            string? callbackBaseUrl,
            bool forceProductionCallbackInDev)
        {
            if (!string.IsNullOrWhiteSpace(callbackBaseUrl)
                && (forceProductionCallbackInDev || httpContext == null || !IsPrivateHost(httpContext.Request.Host.Host)))
            {
                return callbackBaseUrl.Trim();
            }

            if (httpContext != null && !IsPrivateHost(httpContext.Request.Host.Host))
            {
                return $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/api/FlexPay/callback";
            }

            if (!string.IsNullOrWhiteSpace(callbackBaseUrl))
                return callbackBaseUrl.Trim();

            throw new InvalidOperationException(
                "FlexPay:CallbackBaseUrl doit être configuré pour les environnements locaux.");
        }

        public static string ResolvePayOutCallbackUrl(
            HttpContext? httpContext,
            string? callbackBaseUrl,
            bool forceProductionCallbackInDev)
        {
            var baseCallback = ResolveCallbackUrl(httpContext, callbackBaseUrl, forceProductionCallbackInDev)
                .TrimEnd('/');
            if (baseCallback.EndsWith("/callback", StringComparison.OrdinalIgnoreCase))
                return baseCallback[..^"/callback".Length] + "/payout/callback";
            return baseCallback + "/payout/callback";
        }

        /// <summary>URL callback FlexPay pour le module événementiel (pipeline autonome).</summary>
        public static string ResolveEvenementCallbackUrl(
            HttpContext? httpContext,
            string? callbackBaseUrl,
            string eventCallbackRelativePath,
            bool forceProductionCallbackInDev)
        {
            var relativePath = NormalizeRelativePath(eventCallbackRelativePath);

            if (!string.IsNullOrWhiteSpace(callbackBaseUrl)
                && (forceProductionCallbackInDev || httpContext == null || !IsPrivateHost(httpContext.Request.Host.Host)))
            {
                return CombineBaseAndPath(callbackBaseUrl.Trim(), relativePath);
            }

            if (httpContext != null && !IsPrivateHost(httpContext.Request.Host.Host))
            {
                return $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{relativePath}";
            }

            if (!string.IsNullOrWhiteSpace(callbackBaseUrl))
                return CombineBaseAndPath(callbackBaseUrl.Trim(), relativePath);

            throw new InvalidOperationException(
                "FlexPay:CallbackBaseUrl doit être configuré pour les callbacks événement en local.");
        }

        public static string DeriveRedirectUrl(string callbackBaseUrl, string action)
        {
            var baseUrl = callbackBaseUrl.Trim();
            if (baseUrl.EndsWith("/callback", StringComparison.OrdinalIgnoreCase))
                return baseUrl[..^"/callback".Length] + "/" + action;
            return baseUrl.TrimEnd('/') + "/" + action;
        }

        private static bool IsPrivateHost(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
                return true;
            if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                || host.StartsWith("127.", StringComparison.Ordinal)
                || host.StartsWith("10.", StringComparison.Ordinal)
                || host.StartsWith("192.168.", StringComparison.Ordinal))
                return true;
            return false;
        }

        private static string NormalizeRelativePath(string? eventCallbackRelativePath)
        {
            var path = string.IsNullOrWhiteSpace(eventCallbackRelativePath)
                ? EvenementFlexPayConstants.CallbackRoute
                : eventCallbackRelativePath.Trim();

            return path.StartsWith('/') ? path : "/" + path;
        }

        private static string CombineBaseAndPath(string baseUrl, string relativePath)
        {
            var trimmed = baseUrl.TrimEnd('/');
            if (trimmed.EndsWith("/api/FlexPay/callback", StringComparison.OrdinalIgnoreCase))
                return trimmed[..^"/api/FlexPay/callback".Length] + relativePath;

            return trimmed + relativePath;
        }
    }
}
