namespace CongoTravel.Models.Restaurant
{
    /// <summary>Capacité indisponible ou conflit d'inventaire restaurant (HTTP 409).</summary>
    public class RestaurantHoldConflictException : Exception
    {
        public RestaurantHoldConflictException(string message) : base(message)
        {
        }
    }
}
