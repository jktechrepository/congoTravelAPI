namespace CongoTravel.Models.Evenement.Enums
{
    /// <summary>Mode d'inventaire d'une session événementielle (aligné SQL <c>EvenementSessions.InventoryMode</c>).</summary>
    public enum EvenementInventoryMode
    {
        SeatNumbered,
        ClassQuota,
        GlobalQuota
    }
}
