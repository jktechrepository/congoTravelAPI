namespace CongoTravel.Models.Enums
{
    /// <summary>
    /// Canal d'origine d'une réservation ou d'un paiement (snapshot au moment de l'opération).
    /// Distinct de Channel FlexPay (orange/mpesa) et CanalUtilise notifications.
    /// </summary>
    public static class OrigineOperation
    {
        public const string CLIENT = "CLIENT";
        public const string CAISSIER = "CAISSIER";
        public const string GERANT = "GERANT";
        public const string ADMIN = "ADMIN";
        public const string FINANCIER = "FINANCIER";
        public const string SECRETAIRE = "SECRETAIRE";
        public const string SUPER_ADMIN = "SUPER_ADMIN";
        public const string AUTRE_STAFF = "AUTRE_STAFF";
        public const string INCONNU = "INCONNU";

        public static readonly string Default = INCONNU;

        public static bool IsValid(string? value) =>
            !string.IsNullOrWhiteSpace(value) && All.Contains(value);

        public static readonly string[] All =
        {
            CLIENT,
            CAISSIER,
            GERANT,
            ADMIN,
            FINANCIER,
            SECRETAIRE,
            SUPER_ADMIN,
            AUTRE_STAFF,
            INCONNU
        };
    }
}
