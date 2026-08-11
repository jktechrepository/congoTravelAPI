namespace CongoTravel.Models.SiteTouristique
{
    /// <summary>Conflit métier sur <c>SiteTouristiqueClasse</c> (ex. code dupliqué par société).</summary>
    public class SiteTouristiqueClasseConflictException : Exception
    {
        public SiteTouristiqueClasseConflictException(string message) : base(message)
        {
        }
    }
}
