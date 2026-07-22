namespace CongoTravel.Models.Evenement.Enums
{
    /// <summary>Statut paiement événement (aligné SQL <c>EvenementPayments.Status</c>).</summary>
    public enum EvenementPaymentStatus
    {
        PENDING,
        SUCCEEDED,
        FAILED,
        REFUNDED
    }
}
