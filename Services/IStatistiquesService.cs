using CongoTravel.Models.DTOs.Statistiques;

namespace CongoTravel.Services
{
    public interface IStatistiquesService
    {
        Task<StatistiquesTransportDto> GetStatistiquesAsync(
            int idSociete,
            DateTime? debut = null,
            DateTime? fin = null,
            CancellationToken cancellationToken = default);
    }
}
