using CongoTravel.Models;

namespace CongoTravel.Services
{
    public interface IReversementAutomatiqueService
    {
        Task<bool> TryDeclencherApresPaiementElectroniqueAsync(
            Paiement paiement,
            Reservation reservation,
            CancellationToken cancellationToken = default);

        Task<bool> TryDeclencherAsync(
            ReversementAutomatiqueContext ctx,
            CancellationToken cancellationToken = default);
    }
}
