namespace CongoTravel.Models.Evenement.Enums
{
    /// <summary>Statut réservation événement (aligné SQL <c>EvenementReservations.Status</c>).</summary>
    public enum EvenementReservationStatus
    {
        HOLD,
        CONFIRMED,
        CANCELLED,
        EXPIRED
    }
}
