namespace CongoTravel.Models.Restaurant
{
    /// <summary>Conflit métier sur <c>RestaurantZone</c> (ex. code dupliqué par restaurant).</summary>
    public class RestaurantZoneConflictException : Exception
    {
        public RestaurantZoneConflictException(string message) : base(message)
        {
        }
    }
}
