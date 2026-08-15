namespace CongoTravel.Helpers.Restaurant
{
    /// <summary>Codes ticket restaurant (<c>RestaurantTickets.TicketCode</c>).</summary>
    public static class RestaurantTicketCodeGenerator
    {
        private const string Prefix = "REST-TKT";
        private const int MaxLength = 100;

        /// <summary>Format : <c>REST-TKT-{idSociete:000}-{yyyyMMddHHmmss}-{4 chiffres}</c>.</summary>
        public static string GenerateTicketCodeCandidate(int idSociete, DateTime? utcNow = null)
        {
            var now = utcNow ?? DateTime.UtcNow;
            var random = Random.Shared.Next(1000, 10000);
            return $"{Prefix}-{idSociete:D3}-{now:yyyyMMddHHmmss}-{random:D4}";
        }

        public static string? NormalizeTicketCode(string? ticketCode)
        {
            if (string.IsNullOrWhiteSpace(ticketCode))
                return null;

            var normalized = ticketCode.Trim();
            return normalized.Length > MaxLength ? normalized[..MaxLength] : normalized;
        }

        public static bool IsValidTicketCodeFormat(string? ticketCode)
        {
            var normalized = NormalizeTicketCode(ticketCode);
            if (string.IsNullOrEmpty(normalized))
                return false;

            return normalized.StartsWith($"{Prefix}-", StringComparison.Ordinal);
        }
    }
}
