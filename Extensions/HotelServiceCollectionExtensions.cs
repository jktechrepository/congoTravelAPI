using CongoTravel.Services.Hotel;
using CongoTravel.Services.Hotel.Strategies;

namespace CongoTravel.Extensions
{
    public static class HotelServiceCollectionExtensions
    {
        public static IServiceCollection AddHotelReservations(this IServiceCollection services)
        {
            services.AddScoped<IHotelEtablissementService, HotelEtablissementService>();
            services.AddScoped<IHotelRoomTypeService, HotelRoomTypeService>();
            services.AddScoped<IHotelRoomService, HotelRoomService>();
            services.AddScoped<IHotelExtraService, HotelExtraService>();
            services.AddScoped<IHotelPhotoService, HotelPhotoService>();
            services.AddScoped<IHotelAllotmentService, HotelAllotmentService>();
            services.AddScoped<IHotelNightService, HotelNightService>();
            services.AddScoped<IHotelPlanificationService, HotelPlanificationService>();
            services.AddScoped<IHotelAllotmentGenerationService, HotelAllotmentGenerationService>();
            services.AddScoped<IHotelAvailabilityService, HotelAvailabilityService>();

            services.AddScoped<HotelGlobalQuotaHoldStrategy>();
            services.AddScoped<HotelClassQuotaHoldStrategy>();
            services.AddScoped<IHotelInventoryHoldStrategyFactory, HotelInventoryHoldStrategyFactory>();

            services.AddScoped<HotelGlobalQuotaConfirmStrategy>();
            services.AddScoped<HotelClassQuotaConfirmStrategy>();
            services.AddScoped<IHotelInventoryConfirmStrategyFactory, HotelInventoryConfirmStrategyFactory>();

            services.AddScoped<HotelGlobalQuotaCancelStrategy>();
            services.AddScoped<HotelClassQuotaCancelStrategy>();
            services.AddScoped<IHotelInventoryCancelStrategyFactory, HotelInventoryCancelStrategyFactory>();

            services.AddScoped<IHotelHoldService, HotelHoldService>();
            services.AddScoped<IHotelReservationConfirmationService, HotelReservationConfirmationService>();
            services.AddScoped<IHotelPaymentService, HotelPaymentService>();
            services.AddScoped<IHotelReservationService, HotelReservationService>();
            services.AddScoped<IHotelReservationWithPaiementService, HotelReservationWithPaiementService>();
            services.AddScoped<IHotelDashboardService, HotelDashboardService>();
            services.AddScoped<IHotelCommandeFlexPayService, HotelCommandeFlexPayService>();
            services.AddScoped<IHotelFlexPayCallbackService, HotelFlexPayCallbackService>();
            services.AddScoped<IHotelHoldExpirationRunner, HotelHoldExpirationRunner>();
            services.AddHostedService<HotelHoldExpirationHostedService>();
            return services;
        }
    }
}
