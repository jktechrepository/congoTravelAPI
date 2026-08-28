namespace CongoTravel.Models.Hotel
{
    public class HotelConflictException : InvalidOperationException
    {
        public HotelConflictException(string message) : base(message) { }
    }
}
