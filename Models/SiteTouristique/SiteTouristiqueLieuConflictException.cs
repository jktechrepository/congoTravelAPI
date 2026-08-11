namespace CongoTravel.Models.SiteTouristique
{
    /// <summary>Conflit métier lieu site touristique (ex. code dupliqué) — HTTP 409.</summary>
    public class SiteTouristiqueLieuConflictException : Exception
    {
        public SiteTouristiqueLieuConflictException(string message) : base(message)
        {
        }
    }
}
