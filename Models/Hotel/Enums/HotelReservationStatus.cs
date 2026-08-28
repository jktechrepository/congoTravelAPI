namespace CongoTravel.Models.Hotel.Enums
{
    public enum HotelReservationStatus
    {
        HOLD,
        CONFIRMED,
        CANCELLED,
        EXPIRED
    }

    public enum HotelPaymentStatus
    {
        PENDING,
        SUCCEEDED,
        FAILED,
        REFUNDED
    }
}
