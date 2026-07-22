namespace CongoTravel.Models.Enums
{
    /// <summary>
    /// Statut métier du paiement (complément de <see cref="Paiement.Statut"/> booléen).
    /// </summary>
    public enum StatutPaiementMetier
    {
        EnAttente = 0,
        Reussi = 1,
        Echec = 2,
        Annule = 3,
        RemboursementEnCours = 4,
        Rembourse = 5
    }
}
