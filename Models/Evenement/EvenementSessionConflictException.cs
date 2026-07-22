namespace CongoTravel.Models.Evenement
{
    /// <summary>Conflit métier session événement (ex. code dupliqué) — HTTP 409.</summary>
    public class EvenementSessionConflictException : Exception
    {
        public EvenementSessionConflictException(string message) : base(message)
        {
        }
    }
}
