using CongoTravel.Models.DTOs.Statistiques;

namespace CongoTravel.Services.Repositories
{
    /// <summary>
    /// Interface du service SignalR pour les statistiques transport en temps réel.
    /// </summary>
    public interface ISignalRStatistiquesService
    {
        Task NotifyStatistiquesUpdatedAsync(int societeId, StatistiquesTransportDto statistiquesData);

        Task NotifyEvolutionMensuelleUpdatedAsync(int societeId, object evolutionData);

        Task NotifyRepartitionPaiementsUpdatedAsync(int societeId, object repartitionData);

        Task NotifyTopAgentsUpdatedAsync(int societeId, object topAgentsData);

        Task NotifyPerformanceMensuelleUpdatedAsync(int societeId, object performanceData);

        Task SendStatistiquesNotificationAsync(int societeId, string title, string message, string type = "info");

        Task NotifyStatistiquesRefreshRequestedAsync(int societeId, string requestedBy);

        Task SendStatistiquesConnectionTestAsync(int societeId, string message);
    }
}
