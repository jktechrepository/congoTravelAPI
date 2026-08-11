namespace CongoTravel.Helpers.Restaurant
{
    /// <summary>Constantes pipeline FlexPay restaurant (Phase 3 — autonome du transport).</summary>
    public static class RestaurantFlexPayConstants
    {
        public const string Provider = "FLEXPAY";

        public const string CallbackRoute = "/api/restaurants/flexpay/callback";

        public const string VerifierRoutePrefix = "/api/restaurants/flexpay/verifier";
    }
}
