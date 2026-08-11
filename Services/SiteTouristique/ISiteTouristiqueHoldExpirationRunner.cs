using CongoTravel.Data;

namespace CongoTravel.Services.SiteTouristique
{
    /// <summary>Exécute l'expiration des holds site touristiques via <c>sp_ExpireSiteTouristiqueHolds</c>.</summary>
    public interface ISiteTouristiqueHoldExpirationRunner
    {
        Task ExpireHoldsAsync(CongoTravelDbContext context, CancellationToken cancellationToken = default);
    }
}
