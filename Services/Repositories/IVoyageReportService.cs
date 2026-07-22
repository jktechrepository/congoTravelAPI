using CongoTravel.Models.DTOs.Voyage;

namespace CongoTravel.Services.Repositories
{
    public interface IVoyageReportService
    {
        Task<VoyageReportOperationResult> ReporterAsync(
            int idVoyage,
            int idSociete,
            int idUtilisateur,
            string? userName,
            ReporterVoyageDto dto,
            CancellationToken cancellationToken = default);
    }
}
