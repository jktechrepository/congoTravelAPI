namespace CongoTravel.Helpers.SiteTouristique
{
    /// <summary>Génération de références métier site touristique (candidats ; unicité vérifiée en couche service).</summary>
    public static class SiteTouristiqueReferenceGenerator
    {
        private const string ReservationPrefix = "ST-RES";
        private const string PaymentPrefix = "ST-PAY";

        /// <summary>Format : <c>ST-RES-{idSociete:0000}-{yyyyMMddHHmmss}-{8 hex}</c> (max 64 car.).</summary>
        public static string GenerateReservationReferenceCandidate(int idSociete, DateTime? utcNow = null)
        {
            var now = utcNow ?? DateTime.UtcNow;
            var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            return $"{ReservationPrefix}-{idSociete:D4}-{now:yyyyMMddHHmmss}-{suffix}";
        }

        /// <summary>Format : <c>ST-PAY-{idSociete:0000}-{yyyyMMddHHmmss}-{8 hex}</c> (max 100 car.).</summary>
        public static string GeneratePaymentReferenceCandidate(int idSociete, DateTime? utcNow = null)
        {
            var now = utcNow ?? DateTime.UtcNow;
            var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            return $"{PaymentPrefix}-{idSociete:D4}-{now:yyyyMMddHHmmss}-{suffix}";
        }
    }
}
