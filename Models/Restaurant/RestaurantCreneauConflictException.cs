namespace CongoTravel.Models.Restaurant
{
    /// <summary>Conflit métier créneau restaurant (ex. chevauchement Published) — HTTP 409.</summary>
    public class RestaurantCreneauConflictException : Exception
    {
        public RestaurantCreneauConflictException(string message) : base(message)
        {
        }
    }
}
