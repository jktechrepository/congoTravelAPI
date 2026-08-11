namespace CongoTravel.Models.SiteTouristique
{
    /// <summary>Capacité indisponible ou conflit d'inventaire (HTTP 409).</summary>
    public class SiteTouristiqueHoldConflictException : Exception
    {
        public SiteTouristiqueHoldConflictException(string message) : base(message)
        {
        }
    }
}
