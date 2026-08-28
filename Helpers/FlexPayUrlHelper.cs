using Microsoft.AspNetCore.Http;
using CongoTravel.Helpers.Evenement;
using CongoTravel.Helpers.Restaurant;
using CongoTravel.Helpers.SiteTouristique;
using CongoTravel.Helpers.Hotel;

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
            bool forceProductionCallbackInDev) =>
            ResolveModuleCallbackUrl(
                httpContext,
                callbackBaseUrl,
                eventCallbackRelativePath,
                EvenementFlexPayConstants.CallbackRoute,
                forceProductionCallbackInDev,
                "événement");

        /// <summary>URL callback FlexPay pour le module site touristique (pipeline autonome).</summary>
        public static string ResolveSiteTouristiqueCallbackUrl(
            HttpContext? httpContext,
            string? callbackBaseUrl,
            string siteTouristiqueCallbackRelativePath,
            bool forceProductionCallbackInDev) =>
            ResolveModuleCallbackUrl(
                httpContext,
                callbackBaseUrl,
                siteTouristiqueCallbackRelativePath,
                SiteTouristiqueFlexPayConstants.CallbackRoute,
                forceProductionCallbackInDev,
                "site touristique");

        /// <summary>URL callback FlexPay pour le module restaurant (pipeline autonome).</summary>
        public static string ResolveRestaurantCallbackUrl(
            HttpContext? httpContext,
            string? callbackBaseUrl,
            string restaurantCallbackRelativePath,
            bool forceProductionCallbackInDev) =>
            ResolveModuleCallbackUrl(
                httpContext,
                callbackBaseUrl,
                restaurantCallbackRelativePath,
                RestaurantFlexPayConstants.CallbackRoute,
                forceProductionCallbackInDev,
                "restaurant");

        public static string ResolveHotelCallbackUrl(
            HttpContext? httpContext,
            string? callbackBaseUrl,
            string hotelCallbackRelativePath,
            bool forceProductionCallbackInDev) =>
            ResolveModuleCallbackUrl(
                httpContext,
                callbackBaseUrl,
                hotelCallbackRelativePath,
                HotelFlexPayConstants.CallbackRoute,
                forceProductionCallbackInDev,
                "hôtel");

        public static string DeriveRedirectUrl(string callbackBaseUrl, string action)
        {
            var baseUrl = callbackBaseUrl.Trim();
            if (baseUrl.EndsWith("/callback", StringComparison.OrdinalIgnoreCase))
                return baseUrl[..^"/callback".Length] + "/" + action;
            return baseUrl.TrimEnd('/') + "/" + action;
        }

        private static string ResolveModuleCallbackUrl(
            HttpContext? httpContext,
            string? callbackBaseUrl,
            string? relativePathConfig,
            string defaultRelativePath,
            bool forceProductionCallbackInDev,
            string moduleLabel)
        {
            var relativePath = NormalizeRelativePath(relativePathConfig, defaultRelativePath);

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
                $"FlexPay:CallbackBaseUrl doit être configuré pour les callbacks {moduleLabel} en local.");
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

        private static string NormalizeRelativePath(string? relativePathConfig, string defaultRelativePath)
        {
            var path = string.IsNullOrWhiteSpace(relativePathConfig)
                ? defaultRelativePath
                : relativePathConfig.Trim();

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
