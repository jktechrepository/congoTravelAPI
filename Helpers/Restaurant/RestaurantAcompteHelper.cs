namespace CongoTravel.Helpers.Restaurant
{
    /// <summary>Calcul d'acompte restaurant (créneau fixe ou % défaut établissement).</summary>
    public static class RestaurantAcompteHelper
    {
        /// <summary>
        /// <c>acompteUnitaire = MontantAcompte ?? (PrixUnitaire * AcomptePourcentDefaut / 100)</c>
        /// </summary>
        public static decimal ComputeAcompteUnitaire(
            decimal? montantAcompteCreneau,
            decimal prixUnitaire,
            decimal acomptePourcentDefaut)
        {
            if (montantAcompteCreneau.HasValue)
                return Math.Round(montantAcompteCreneau.Value, 2, MidpointRounding.AwayFromZero);

            return Math.Round(
                prixUnitaire * acomptePourcentDefaut / 100m,
                2,
                MidpointRounding.AwayFromZero);
        }

        /// <summary><c>total = Round(acompteUnitaire * quantity, 2)</c></summary>
        public static decimal ComputeAcompteTotal(decimal acompteUnitaire, int quantity) =>
            Math.Round(acompteUnitaire * quantity, 2, MidpointRounding.AwayFromZero);
    }
}
