namespace CongoTravel.Helpers
{
    /// <summary>
    /// Mapping des statuts renvoyés par l'API FlexPay check / callback.
    /// Aligné sur StatusFlexPay et Integration-FlexPay-From-LexMusicaAPI : 0 = réussi, 1 = échec, 2 = en attente.
    /// </summary>
    public static class FlexPayStatusHelper
    {
        public static bool IsSuccess(string? status) =>
            string.Equals(status?.Trim(), "0", StringComparison.Ordinal);

        public static bool IsFailure(string? status) =>
            string.Equals(status?.Trim(), "1", StringComparison.Ordinal);

        public static bool IsPending(string? status) =>
            string.Equals(status?.Trim(), "2", StringComparison.Ordinal);

        /// <summary>
        /// Statut inconnu ou vide : traiter comme en attente pour éviter un faux refus au verifier.
        /// </summary>
        public static bool ShouldTreatAsPending(string? status) =>
            IsPending(status) || (!IsSuccess(status) && !IsFailure(status));
    }
}
