using CongoTravel.Models;

namespace CongoTravel.Services
{
    /// <summary>Stub DI pour les modules satellites isolés ; Program.cs enregistre l'implémentation réelle ensuite.</summary>
    public sealed class NullReversementAutomatiqueService : IReversementAutomatiqueService
    {
        public Task<bool> TryDeclencherApresPaiementElectroniqueAsync(
            Paiement paiement,
            Reservation reservation,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> TryDeclencherAsync(
            ReversementAutomatiqueContext ctx,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }
}
