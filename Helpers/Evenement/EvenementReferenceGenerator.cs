namespace CongoTravel.Helpers.Evenement
{
    /// <summary>Génération de références métier événement (candidats ; unicité vérifiée en couche service).</summary>
    public static class EvenementReferenceGenerator
    {
        private const string ReservationPrefix = "EVT-RES";
        private const string PaymentPrefix = "EVT-PAY";

        /// <summary>Format : <c>EVT-RES-{idSociete:0000}-{yyyyMMddHHmmss}-{8 hex}</c> (max 64 car.).</summary>
        public static string GenerateReservationReferenceCandidate(int idSociete, DateTime? utcNow = null)
        {
            var now = utcNow ?? DateTime.UtcNow;
            var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            return $"{ReservationPrefix}-{idSociete:D4}-{now:yyyyMMddHHmmss}-{suffix}";
        }

        /// <summary>Format : <c>EVT-PAY-{idSociete:0000}-{yyyyMMddHHmmss}-{8 hex}</c> (max 100 car.).</summary>
        public static string GeneratePaymentReferenceCandidate(int idSociete, DateTime? utcNow = null)
        {
            var now = utcNow ?? DateTime.UtcNow;
            var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            return $"{PaymentPrefix}-{idSociete:D4}-{now:yyyyMMddHHmmss}-{suffix}";
        }
    }
}
