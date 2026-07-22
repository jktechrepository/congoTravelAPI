using CongoTravel.Models;

namespace CongoTravel.Services.Repositories
{
    public interface IVoyageReportNotificationService
    {
        Task<(int Envoyees, int Echecs)> NotifyReservedClientsAsync(
            Voyage voyage,
            DateTime ancienneDateDepart,
            TimeSpan ancienneHeureDepart,
            string? motif,
            CancellationToken cancellationToken = default);
    }
}
