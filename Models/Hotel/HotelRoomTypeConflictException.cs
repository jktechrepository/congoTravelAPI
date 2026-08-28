namespace CongoTravel.Models.Hotel
{
    public class HotelRoomTypeConflictException : InvalidOperationException
    {
        public HotelRoomTypeConflictException(string message) : base(message) { }
    }
}
