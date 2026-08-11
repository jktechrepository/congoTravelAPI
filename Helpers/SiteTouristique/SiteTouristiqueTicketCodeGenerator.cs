namespace CongoTravel.Helpers.SiteTouristique
{
    /// <summary>Codes ticket site touristique (<c>SiteTouristiqueTickets.TicketCode</c>), distincts des QR transport.</summary>
    public static class SiteTouristiqueTicketCodeGenerator
    {
        private const string Prefix = "ST-TKT";
        private const int MaxLength = 100;

        /// <summary>Format : <c>ST-TKT-{idSociete:000}-{yyyyMMddHHmmss}-{4 chiffres}</c>.</summary>
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
