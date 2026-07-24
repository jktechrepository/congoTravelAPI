namespace CongoTravel.Services
{
    /// <summary>Erreur métier auth externe (Google / Apple) avec code HTTP.</summary>
    public class ExternalAuthException : Exception
    {
        public int StatusCode { get; }

        public ExternalAuthException(int statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }
    }

    /// <summary>Alias rétrocompatibilité Google.</summary>
    public class GoogleAuthException : ExternalAuthException
    {
        public GoogleAuthException(int statusCode, string message) : base(statusCode, message)
        {
        }
    }
}
