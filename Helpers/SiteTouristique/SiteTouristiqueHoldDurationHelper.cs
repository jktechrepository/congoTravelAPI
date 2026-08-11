using CongoTravel.Models;

namespace CongoTravel.Helpers.SiteTouristique
{
    /// <summary>Durée des holds site touristiques (<c>ConfigSociete.DureeHoldSiteTouristiqueMinutes</c>).</summary>
    public static class SiteTouristiqueHoldDurationHelper
    {
        public static int ResolveHoldMinutes(ConfigSociete? config)
        {
            if (config == null || config.DureeHoldSiteTouristiqueMinutes <= 0)
                return ConfigSocieteDefaults.DureeHoldSiteTouristiqueMinutes;

            return Math.Clamp(config.DureeHoldSiteTouristiqueMinutes, 1, 120);
        }

        public static DateTime ComputeExpiresAtUtc(DateTime utcNow, int holdMinutes) =>
            utcNow.AddMinutes(Math.Clamp(holdMinutes, 1, 120));

        public static DateTime ComputeExpiresAtUtc(DateTime utcNow, ConfigSociete? config) =>
            ComputeExpiresAtUtc(utcNow, ResolveHoldMinutes(config));
    }
}
