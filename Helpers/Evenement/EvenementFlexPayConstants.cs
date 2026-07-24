namespace CongoTravel.Helpers.Evenement
{
    /// <summary>Constantes pipeline FlexPay événementiel (Phase 5 — autonome du transport).</summary>
    public static class EvenementFlexPayConstants
    {
        public const string Provider = "FLEXPAY";

        public const string CallbackRoute = "/api/events/flexpay/callback";

        public const string VerifierRoutePrefix = "/api/events/flexpay/verifier";
    }
}
