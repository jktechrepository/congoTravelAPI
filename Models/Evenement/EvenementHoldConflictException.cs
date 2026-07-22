namespace CongoTravel.Models.Evenement
{
    /// <summary>Capacité indisponible ou conflit d'inventaire (HTTP 409).</summary>
    public class EvenementHoldConflictException : Exception
    {
        public EvenementHoldConflictException(string message) : base(message)
        {
        }
    }
}
