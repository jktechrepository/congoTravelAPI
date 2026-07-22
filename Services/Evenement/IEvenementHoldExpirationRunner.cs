using CongoTravel.Data;

namespace CongoTravel.Services.Evenement
{
    /// <summary>Exécute l'expiration des holds événementiels via <c>sp_ExpireEvenementHolds</c>.</summary>
    public interface IEvenementHoldExpirationRunner
    {
        Task ExpireHoldsAsync(CongoTravelDbContext context, CancellationToken cancellationToken = default);
    }
}
