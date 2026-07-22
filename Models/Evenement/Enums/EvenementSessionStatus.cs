namespace CongoTravel.Models.Evenement.Enums
{
    /// <summary>Statut de publication d'une session (aligné SQL <c>EvenementSessions.Status</c>).</summary>
    public enum EvenementSessionStatus
    {
        Draft,
        Published,
        Closed,
        Cancelled
    }
}
