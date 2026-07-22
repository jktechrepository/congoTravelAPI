using CongoTravel.Models;

namespace CongoTravel.Helpers.Evenement
{
    /// <summary>Durée des holds événementiels (<c>ConfigSociete.DureeHoldEvenementMinutes</c>).</summary>
    public static class EvenementHoldDurationHelper
    {
        public static int ResolveHoldMinutes(ConfigSociete? config)
        {
            if (config == null || config.DureeHoldEvenementMinutes <= 0)
                return ConfigSocieteDefaults.DureeHoldEvenementMinutes;

            return Math.Clamp(config.DureeHoldEvenementMinutes, 1, 120);
        }

        public static DateTime ComputeExpiresAtUtc(DateTime utcNow, int holdMinutes) =>
            utcNow.AddMinutes(Math.Clamp(holdMinutes, 1, 120));

        public static DateTime ComputeExpiresAtUtc(DateTime utcNow, ConfigSociete? config) =>
            ComputeExpiresAtUtc(utcNow, ResolveHoldMinutes(config));
    }
}
