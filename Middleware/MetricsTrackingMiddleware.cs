using CongoTravel.Services;

namespace CongoTravel.Middleware
{
    /// <summary>
    /// Middleware pour tracker les métriques d'application
    /// </summary>
    public class MetricsTrackingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<MetricsTrackingMiddleware> _logger;

        public MetricsTrackingMiddleware(RequestDelegate next, ILogger<MetricsTrackingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var isError = false;

            try
            {
                await _next(context);
            }
            catch
            {
                isError = true;
                MetricsService.RecordError();
                throw;
            }
            finally
            {
                stopwatch.Stop();

                // Enregistrer la requête
                MetricsService.RecordRequest();

                // Logger les requêtes lentes
                if (stopwatch.ElapsedMilliseconds > 1000)
                {
                    _logger.LogWarning("Requête lente détectée: {Method} {Path} - {ElapsedMs}ms", 
                        context.Request.Method, 
                        context.Request.Path, 
                        stopwatch.ElapsedMilliseconds);
                }

                // Logger les erreurs
                if (isError || context.Response.StatusCode >= 400)
                {
                    MetricsService.RecordError();
                    _logger.LogWarning("Erreur HTTP: {Method} {Path} - {StatusCode}", 
                        context.Request.Method, 
                        context.Request.Path, 
                        context.Response.StatusCode);
                }

                // Détecter les exports
                if (context.Request.Path.StartsWithSegments("/api/Client/societe/") && 
                    context.Request.Path.Value.EndsWith("/export"))
                {
                    MetricsService.RecordExport();
                }
            }
        }
    }

    /// <summary>
    /// Extension method pour ajouter le middleware au pipeline
    /// </summary>
    public static class MetricsTrackingMiddlewareExtensions
    {
        public static IApplicationBuilder UseMetricsTracking(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<MetricsTrackingMiddleware>();
        }
    }
}
