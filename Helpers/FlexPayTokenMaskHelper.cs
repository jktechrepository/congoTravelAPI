namespace CongoTravel.Helpers
{
    public static class FlexPayTokenMaskHelper
    {
        public static string Mask(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return string.Empty;
            var t = token.Trim();
            if (t.Length <= 8)
                return "****";
            return new string('*', Math.Min(t.Length - 4, 12)) + t[^4..];
        }
    }
}
