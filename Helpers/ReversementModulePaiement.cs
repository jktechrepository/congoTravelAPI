using CongoTravel.Models.Enums;

namespace CongoTravel.Helpers
{
    /// <summary>Valeurs canoniques de <c>ReversementSite.ModulePaiement</c>.</summary>
    public static class ReversementModulePaiement
    {
        public const string Transport = nameof(SocieteActiviteType.Transport);
        public const string Evenement = nameof(SocieteActiviteType.Evenement);
        public const string Restaurant = nameof(SocieteActiviteType.Restaurant);
        public const string SiteTouristique = nameof(SocieteActiviteType.SiteTouristique);
    }
}
