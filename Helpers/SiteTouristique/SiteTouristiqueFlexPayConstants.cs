namespace CongoTravel.Helpers.SiteTouristique
{
    /// <summary>Constantes pipeline FlexPay site touristique (Phase 5 — autonome du transport).</summary>
    public static class SiteTouristiqueFlexPayConstants
    {
        public const string Provider = "FLEXPAY";

        public const string CallbackRoute = "/api/sites-touristiques/flexpay/callback";

        public const string VerifierRoutePrefix = "/api/sites-touristiques/flexpay/verifier";
    }
}
