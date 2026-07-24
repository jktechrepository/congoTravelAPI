using Microsoft.Extensions.DependencyInjection;
using CongoTravel.Services.Evenement;
using CongoTravel.Services.Evenement.Strategies;

namespace CongoTravel.Extensions
{
    /// <summary>Enregistrement DI du module billetterie événementielle (Mode C V1).</summary>
    public static class EvenementServiceCollectionExtensions
    {
        public static IServiceCollection AddEvenementTicketing(this IServiceCollection services)
        {
            services.AddScoped<IEvenementHoldExpirationRunner, EvenementHoldExpirationRunner>();
            services.AddHostedService<EvenementHoldExpirationHostedService>();

            services.AddScoped<EvenementGlobalQuotaHoldStrategy>();
            services.AddScoped<EvenementClassQuotaHoldStrategy>();
            services.AddScoped<EvenementSeatNumberedHoldStrategy>();
            services.AddScoped<IEvenementInventoryHoldStrategyFactory, EvenementInventoryHoldStrategyFactory>();

            services.AddScoped<EvenementGlobalQuotaConfirmStrategy>();
            services.AddScoped<EvenementClassQuotaConfirmStrategy>();
            services.AddScoped<EvenementSeatNumberedConfirmStrategy>();
            services.AddScoped<IEvenementInventoryConfirmStrategyFactory, EvenementInventoryConfirmStrategyFactory>();

            services.AddScoped<EvenementGlobalQuotaCancelStrategy>();
            services.AddScoped<EvenementClassQuotaCancelStrategy>();
            services.AddScoped<EvenementSeatNumberedCancelStrategy>();
            services.AddScoped<IEvenementInventoryCancelStrategyFactory, EvenementInventoryCancelStrategyFactory>();

            services.AddScoped<IEvenementReservationConfirmationService, EvenementReservationConfirmationService>();
            services.AddScoped<IEvenementFlexPayInitiationService, EvenementFlexPayInitiationService>();
            services.AddScoped<IEvenementFlexPayCallbackService, EvenementFlexPayCallbackService>();
            services.AddScoped<IEvenementDashboardService, EvenementDashboardService>();
            services.AddScoped<IEvenementSessionService, EvenementSessionService>();
            services.AddScoped<IEvenementSessionPhotoService, EvenementSessionPhotoService>();
            services.AddScoped<IEvenementClasseService, EvenementClasseService>();
            services.AddScoped<IEvenementHoldService, EvenementHoldService>();
            services.AddScoped<IEvenementAvailabilityService, EvenementAvailabilityService>();
            services.AddScoped<IEvenementPaymentService, EvenementPaymentService>();
            services.AddScoped<IEvenementReservationService, EvenementReservationService>();
            services.AddScoped<IEvenementReservationWithPaiementService, EvenementReservationWithPaiementService>();
            services.AddScoped<IEvenementTicketService, EvenementTicketService>();

            return services;
        }
    }
}
