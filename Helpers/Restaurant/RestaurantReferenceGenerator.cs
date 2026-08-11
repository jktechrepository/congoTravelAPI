namespace CongoTravel.Helpers.Restaurant
{
    public static class RestaurantReferenceGenerator
    {
        private const string ReservationPrefix = "RST-RES";
        private const string PaymentPrefix = "RST-PAY";

        public static string GenerateReservationReferenceCandidate(int idSociete, DateTime? utcNow = null)
        {
            var now = utcNow ?? DateTime.UtcNow;
            var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            return $"{ReservationPrefix}-{idSociete:D4}-{now:yyyyMMddHHmmss}-{suffix}";
        }

        public static string GeneratePaymentReferenceCandidate(int idSociete, DateTime? utcNow = null)
        {
            var now = utcNow ?? DateTime.UtcNow;
            var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            return $"{PaymentPrefix}-{idSociete:D4}-{now:yyyyMMddHHmmss}-{suffix}";
        }
    }
}
