using System.Text.RegularExpressions;

namespace CongoTravel.Helpers
{
    public static class MobileMoneyPhoneHelper
    {
        private static readonly Regex DigitsOnly = new(@"^\d+$", RegexOptions.Compiled);

        public static bool TryNormalize(string? raw, out string normalized, out string? error)
        {
            normalized = string.Empty;
            error = null;

            if (string.IsNullOrWhiteSpace(raw))
            {
                error = "Numéro Mobile Money requis.";
                return false;
            }

            normalized = raw.Trim().Replace(" ", string.Empty).Replace("+", string.Empty);
            if (!DigitsOnly.IsMatch(normalized))
            {
                error = "Le numéro Mobile Money doit contenir uniquement des chiffres.";
                return false;
            }

            if (normalized.Length < 9 || normalized.Length > 15)
            {
                error = "Le numéro Mobile Money doit contenir entre 9 et 15 chiffres.";
                return false;
            }

            return true;
        }
    }
}
