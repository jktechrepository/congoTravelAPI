namespace CongoTravel.Models.Restaurant
{
    /// <summary>Conflit métier établissement restaurant (ex. code dupliqué) — HTTP 409.</summary>
    public class RestaurantConflictException : Exception
    {
        public RestaurantConflictException(string message) : base(message)
        {
        }
    }
}
