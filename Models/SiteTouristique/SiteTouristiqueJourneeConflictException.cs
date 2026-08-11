namespace CongoTravel.Models.SiteTouristique
{
    /// <summary>Conflit métier journée site touristique — HTTP 409.</summary>
    public class SiteTouristiqueJourneeConflictException : Exception
    {
        public SiteTouristiqueJourneeConflictException(string message) : base(message)
        {
        }
    }
}
