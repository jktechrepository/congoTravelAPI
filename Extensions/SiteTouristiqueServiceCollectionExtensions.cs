using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using CongoTravel.Services;
using CongoTravel.Services.SiteTouristique;
using CongoTravel.Services.SiteTouristique.Strategies;

namespace CongoTravel.Extensions
{
    /// <summary>Enregistrement DI du module billetterie site touristique (GlobalQuota + ClassQuota V1).</summary>
    public static class SiteTouristiqueServiceCollectionExtensions
    {
        public static IServiceCollection AddSiteTouristiqueTicketing(this IServiceCollection services)
        {
            services.TryAddScoped<IReversementAutomatiqueService, NullReversementAutomatiqueService>();
            services.AddScoped<ISiteTouristiqueHoldExpirationRunner, SiteTouristiqueHoldExpirationRunner>();
            services.AddHostedService<SiteTouristiqueHoldExpirationHostedService>();

            services.AddScoped<SiteTouristiqueGlobalQuotaHoldStrategy>();
            services.AddScoped<SiteTouristiqueClassQuotaHoldStrategy>();
            services.AddScoped<ISiteTouristiqueInventoryHoldStrategyFactory, SiteTouristiqueInventoryHoldStrategyFactory>();

            services.AddScoped<SiteTouristiqueGlobalQuotaConfirmStrategy>();
            services.AddScoped<SiteTouristiqueClassQuotaConfirmStrategy>();
            services.AddScoped<ISiteTouristiqueInventoryConfirmStrategyFactory, SiteTouristiqueInventoryConfirmStrategyFactory>();

            services.AddScoped<SiteTouristiqueGlobalQuotaCancelStrategy>();
            services.AddScoped<SiteTouristiqueClassQuotaCancelStrategy>();
            services.AddScoped<ISiteTouristiqueInventoryCancelStrategyFactory, SiteTouristiqueInventoryCancelStrategyFactory>();

            services.AddScoped<ISiteTouristiqueReservationConfirmationService, SiteTouristiqueReservationConfirmationService>();
            services.AddScoped<ISiteTouristiqueFlexPayInitiationService, SiteTouristiqueFlexPayInitiationService>();
            services.AddScoped<ISiteTouristiqueCommandeFlexPayService, SiteTouristiqueCommandeFlexPayService>();
            services.AddScoped<ISiteTouristiqueFlexPayCallbackService, SiteTouristiqueFlexPayCallbackService>();
            services.AddScoped<ISiteTouristiqueDashboardService, SiteTouristiqueDashboardService>();
            services.AddScoped<ISiteTouristiqueLieuService, SiteTouristiqueLieuService>();
            services.AddScoped<ISiteTouristiqueLieuPhotoService, SiteTouristiqueLieuPhotoService>();
            services.AddScoped<ISiteTouristiqueJourneeService, SiteTouristiqueJourneeService>();
            services.AddScoped<ISiteTouristiquePlanificationService, SiteTouristiquePlanificationService>();
            services.AddScoped<ISiteTouristiqueJourneeGenerationService, SiteTouristiqueJourneeGenerationService>();
            services.AddScoped<ISiteTouristiqueClasseService, SiteTouristiqueClasseService>();
            services.AddScoped<ISiteTouristiqueHoldService, SiteTouristiqueHoldService>();
            services.AddScoped<ISiteTouristiqueAvailabilityService, SiteTouristiqueAvailabilityService>();
            services.AddScoped<ISiteTouristiquePaymentService, SiteTouristiquePaymentService>();
            services.AddScoped<ISiteTouristiqueReservationService, SiteTouristiqueReservationService>();
            services.AddScoped<ISiteTouristiqueReservationWithPaiementService, SiteTouristiqueReservationWithPaiementService>();
            services.AddScoped<ISiteTouristiqueTicketService, SiteTouristiqueTicketService>();

            return services;
        }
    }
}
