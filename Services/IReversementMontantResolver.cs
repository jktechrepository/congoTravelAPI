using CongoTravel.Models;

namespace CongoTravel.Services
{
    public class ReversementMontantResult
    {
        public decimal Montant { get; set; }
        public string CodeDevise { get; set; } = "CDF";
        public string? Motif { get; set; }
    }

    public interface IReversementMontantResolver
    {
        Task<ReversementMontantResult?> ResolveAsync(
            Paiement paiement,
            Reservation reservation,
            ConfigSociete config,
            CancellationToken cancellationToken = default);
    }
}
