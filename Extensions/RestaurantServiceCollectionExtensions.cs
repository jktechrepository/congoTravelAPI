using Microsoft.Extensions.DependencyInjection;
using CongoTravel.Services.Restaurant;
using CongoTravel.Services.Restaurant.Strategies;

namespace CongoTravel.Extensions
{
    /// <summary>Enregistrement DI du module réservation restaurant (Phase 1–6 planif).</summary>
    public static class RestaurantServiceCollectionExtensions
    {
        public static IServiceCollection AddRestaurantReservations(this IServiceCollection services)
        {
            services.AddScoped<IRestaurantHoldExpirationRunner, RestaurantHoldExpirationRunner>();
            services.AddHostedService<RestaurantHoldExpirationHostedService>();

            services.AddScoped<RestaurantGlobalQuotaHoldStrategy>();
            services.AddScoped<RestaurantClassQuotaHoldStrategy>();
            services.AddScoped<IRestaurantInventoryHoldStrategyFactory, RestaurantInventoryHoldStrategyFactory>();

            services.AddScoped<RestaurantGlobalQuotaConfirmStrategy>();
            services.AddScoped<RestaurantClassQuotaConfirmStrategy>();
            services.AddScoped<IRestaurantInventoryConfirmStrategyFactory, RestaurantInventoryConfirmStrategyFactory>();

            services.AddScoped<RestaurantGlobalQuotaCancelStrategy>();
            services.AddScoped<RestaurantClassQuotaCancelStrategy>();
            services.AddScoped<IRestaurantInventoryCancelStrategyFactory, RestaurantInventoryCancelStrategyFactory>();

            services.AddScoped<IRestaurantEtablissementService, RestaurantEtablissementService>();
            services.AddScoped<IRestaurantZoneService, RestaurantZoneService>();
            services.AddScoped<IRestaurantCreneauService, RestaurantCreneauService>();
            services.AddScoped<IRestaurantPlanificationService, RestaurantPlanificationService>();
            services.AddScoped<IRestaurantCreneauGenerationService, RestaurantCreneauGenerationService>();
            services.AddScoped<IRestaurantAvailabilityService, RestaurantAvailabilityService>();
            services.AddScoped<IRestaurantHoldService, RestaurantHoldService>();
            services.AddScoped<IRestaurantReservationConfirmationService, RestaurantReservationConfirmationService>();
            services.AddScoped<IRestaurantFlexPayInitiationService, RestaurantFlexPayInitiationService>();
            services.AddScoped<IRestaurantFlexPayCallbackService, RestaurantFlexPayCallbackService>();
            services.AddScoped<IRestaurantPaymentService, RestaurantPaymentService>();
            services.AddScoped<IRestaurantReservationService, RestaurantReservationService>();
            services.AddScoped<IRestaurantReservationWithPaiementService, RestaurantReservationWithPaiementService>();
            services.AddScoped<IRestaurantDashboardService, RestaurantDashboardService>();

            return services;
        }
    }
}
