namespace CongoTravel.Models.Enums
{
    /// <summary>
    /// Regroupement métier pour reporting : auto-service client vs staff (agent).
    /// Dérivé de <see cref="OrigineOperation"/> granulaire — non persisté en base.
    /// </summary>
    public static class OrigineOperationGroupe
    {
        public const string CLIENT = "CLIENT";
        public const string AGENT = "AGENT";
        public const string INCONNU = "INCONNU";

        public static readonly string[] All = { CLIENT, AGENT, INCONNU };

        public static bool IsValid(string? value) =>
            !string.IsNullOrWhiteSpace(value)
            && All.Contains(value, StringComparer.OrdinalIgnoreCase);
    }
}
