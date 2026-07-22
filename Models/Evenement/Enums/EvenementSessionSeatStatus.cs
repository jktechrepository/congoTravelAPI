namespace CongoTravel.Models.Evenement.Enums
{
    /// <summary>Statut d'un siège de session (aligné SQL <c>EvenementSessionSeats.SeatStatus</c>).</summary>
    public enum EvenementSessionSeatStatus
    {
        Available,
        Held,
        Sold,
        Blocked
    }
}
