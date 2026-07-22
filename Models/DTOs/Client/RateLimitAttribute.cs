using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using CongoTravel.Services;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace CongoTravel.Models.DTOs.Client
{
    /// <summary>
    /// Attribute pour limiter le nombre de requêtes par IP
    /// Utilisé pour protéger les endpoints publics contre les abus
    /// </summary>
    public class RateLimitAttribute : ActionFilterAttribute
    {
        private readonly int _requests;
        private readonly TimeSpan _timeWindow;
        private readonly string _cacheKeyPrefix;
        private IMemoryCache _cache;

        public RateLimitAttribute(int requests = 5, int timeWindowMinutes = 15, string? cacheKeyPrefix = null)
        {
            _requests = requests;
            _timeWindow = TimeSpan.FromMinutes(timeWindowMinutes);
            _cacheKeyPrefix = cacheKeyPrefix ?? "RateLimit";
            
            // Résoudre le cache via le service provider (sera injecté plus tard)
            _cache = null!;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Résoudre le cache si nécessaire
            if (_cache == null)
            {
                _cache = context.HttpContext.RequestServices.GetService<IMemoryCache>()
                         ?? throw new InvalidOperationException("IMemoryCache non disponible");
            }

            var clientIp = GetClientIpAddress(context.HttpContext);
            var cacheKey = $"{_cacheKeyPrefix}_{clientIp}_{context.ActionDescriptor.DisplayName}";

            if (_cache.TryGetValue(cacheKey, out int currentRequests))
            {
                if (currentRequests >= _requests)
                {
                    context.Result = new JsonResult(new
                    {
                        success = false,
                        message = "Trop de tentatives. Veuillez réessayer plus tard.",
                        retryAfter = _timeWindow.TotalSeconds
                    })
                    {
                        StatusCode = (int)HttpStatusCode.TooManyRequests
                    };
                    return;
                }

                _cache.Set(cacheKey, currentRequests + 1, _timeWindow);
            }
            else
            {
                _cache.Set(cacheKey, 1, _timeWindow);
            }

            base.OnActionExecuting(context);
        }

        protected static string GetClientIpAddress(HttpContext context)
        {
            // Vérifier les headers proxy d'abord
            var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(ip))
            {
                // Prendre la première IP si plusieurs sont séparées par des virgules
                return ip.Split(',')[0].Trim();
            }

            ip = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(ip))
            {
                return ip;
            }

            // Fallback sur l'IP distante
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }

    /// <summary>
    /// Rate limiting spécifique pour l'inscription des clients
    /// </summary>
    public class ClientRegistrationRateLimitAttribute : RateLimitAttribute
    {
        public ClientRegistrationRateLimitAttribute() : base(requests: 3, timeWindowMinutes: 10, cacheKeyPrefix: "ClientRegistration")
        {
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var cache = context.HttpContext.RequestServices.GetService<IMemoryCache>()
                        ?? throw new InvalidOperationException("IMemoryCache non disponible");
            var logger = context.HttpContext.RequestServices.GetService<ILogger<ClientRegistrationRateLimitAttribute>>();
            var optionsAccessor = context.HttpContext.RequestServices.GetService<IOptions<ClientRegistrationRateLimitOptions>>();
            var options = optionsAccessor?.Value ?? new ClientRegistrationRateLimitOptions();

            var request = context.HttpContext.Request;
            var endpoint = context.ActionDescriptor.DisplayName ?? "unknown";
            var ip = GetClientIpAddress(context.HttpContext);
            var deviceId = request.Headers[options.DeviceIdHeaderName].FirstOrDefault()?.Trim();
            var registrationDto = context.ActionArguments.Values.OfType<RegisterClientDto>().FirstOrDefault();
            var normalizedEmail = NormalizeEmail(registrationDto?.EmailClient);

            // 1) Limiteur principal par email (évite blocage global derrière une même IP)
            if (!string.IsNullOrWhiteSpace(normalizedEmail))
            {
                var keyEmail = $"ClientRegistration:email:{normalizedEmail}";
                if (IsLimited(cache, keyEmail, options.EmailLimit, TimeSpan.FromMinutes(options.EmailWindowMinutes), out var emailCount))
                {
                    LogBlock(logger, "email", keyEmail, ip, endpoint, emailCount, options.EmailLimit);
                    MetricsService.RecordRateLimitBlock("email");
                    context.Result = BuildTooManyRequestsResult("Trop de tentatives pour cet email. Veuillez réessayer plus tard.", options.EmailWindowMinutes);
                    return;
                }
            }

            // 2) Limiteur par device (si activé et header disponible)
            if (options.EnableDeviceScope && !string.IsNullOrWhiteSpace(deviceId))
            {
                var keyDevice = $"ClientRegistration:device:{deviceId}";
                if (IsLimited(cache, keyDevice, options.DeviceLimit, TimeSpan.FromMinutes(options.DeviceWindowMinutes), out var deviceCount))
                {
                    LogBlock(logger, "device", keyDevice, ip, endpoint, deviceCount, options.DeviceLimit);
                    MetricsService.RecordRateLimitBlock("device");
                    context.Result = BuildTooManyRequestsResult("Trop de tentatives depuis cet appareil. Veuillez réessayer plus tard.", options.DeviceWindowMinutes);
                    return;
                }
            }

            // 3) Filet anti-flood par IP (seuil volontairement plus haut)
            var keyIp = $"ClientRegistration:ip:{ip}";
            if (IsLimited(cache, keyIp, options.IpLimit, TimeSpan.FromMinutes(options.IpWindowMinutes), out var ipCount))
            {
                LogBlock(logger, "ip", keyIp, ip, endpoint, ipCount, options.IpLimit);
                MetricsService.RecordRateLimitBlock("ip");
                context.Result = BuildTooManyRequestsResult("Trop de tentatives depuis cette IP. Veuillez réessayer plus tard.", options.IpWindowMinutes);
                return;
            }

            base.OnActionExecuting(context);
        }

        private static bool IsLimited(
            IMemoryCache cache,
            string cacheKey,
            int maxRequests,
            TimeSpan window,
            out int resultingCount)
        {
            if (cache.TryGetValue(cacheKey, out int current))
            {
                if (current >= maxRequests)
                {
                    resultingCount = current;
                    return true;
                }

                resultingCount = current + 1;
                cache.Set(cacheKey, resultingCount, window);
                return false;
            }

            resultingCount = 1;
            cache.Set(cacheKey, resultingCount, window);
            return false;
        }

        private static JsonResult BuildTooManyRequestsResult(string message, int retryAfterMinutes) =>
            new(new
            {
                success = false,
                message,
                retryAfter = TimeSpan.FromMinutes(retryAfterMinutes).TotalSeconds
            })
            {
                StatusCode = (int)HttpStatusCode.TooManyRequests
            };

        private static string? NormalizeEmail(string? email) =>
            string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

        private static string HashKeyForLogs(string raw)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(bytes)[..12];
        }

        private static void LogBlock(
            ILogger<ClientRegistrationRateLimitAttribute>? logger,
            string scope,
            string key,
            string ip,
            string endpoint,
            int currentCount,
            int limit)
        {
            logger?.LogWarning(
                "RateLimit registration blocked. Scope={Scope} KeyHash={KeyHash} Ip={Ip} Endpoint={Endpoint} Count={Count} Limit={Limit}",
                scope,
                HashKeyForLogs(key),
                ip,
                endpoint,
                currentCount,
                limit);
        }
    }

    /// <summary>
    /// Rate limiting spécifique pour la vérification d'email
    /// </summary>
    public class EmailCheckRateLimitAttribute : RateLimitAttribute
    {
        public EmailCheckRateLimitAttribute() : base(requests: 10, timeWindowMinutes: 5, cacheKeyPrefix: "EmailCheck")
        {
        }
    }

    public class ClientRegistrationRateLimitOptions
    {
        public const string SectionName = "ClientRegistrationRateLimit";

        public int EmailLimit { get; set; } = 3;

        public int EmailWindowMinutes { get; set; } = 10;

        public bool EnableDeviceScope { get; set; } = true;

        public int DeviceLimit { get; set; } = 5;

        public int DeviceWindowMinutes { get; set; } = 10;

        public int IpLimit { get; set; } = 30;

        public int IpWindowMinutes { get; set; } = 10;

        public string DeviceIdHeaderName { get; set; } = "X-Device-Id";
    }
}
