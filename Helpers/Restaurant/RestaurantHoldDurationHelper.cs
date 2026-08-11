using CongoTravel.Models;

namespace CongoTravel.Helpers.Restaurant
{
    /// <summary>Durée des holds restaurant (<c>ConfigSociete.DureeHoldRestaurantMinutes</c>).</summary>
    public static class RestaurantHoldDurationHelper
    {
        public static int ResolveHoldMinutes(ConfigSociete? config)
        {
            if (config == null || config.DureeHoldRestaurantMinutes <= 0)
                return ConfigSocieteDefaults.DureeHoldRestaurantMinutes;

            return Math.Clamp(config.DureeHoldRestaurantMinutes, 1, 120);
        }

        public static DateTime ComputeExpiresAtUtc(DateTime utcNow, int holdMinutes) =>
            utcNow.AddMinutes(Math.Clamp(holdMinutes, 1, 120));

        public static DateTime ComputeExpiresAtUtc(DateTime utcNow, ConfigSociete? config) =>
            ComputeExpiresAtUtc(utcNow, ResolveHoldMinutes(config));
    }
}
