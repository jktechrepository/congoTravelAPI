using CongoTravel.Models;
using CongoTravel.Models.DTOs.ConfigSociete;

namespace CongoTravel.Services.Repositories
{
    public interface IConfigSocieteRepository
    {
        Task<ConfigSociete> GetOrCreateAsync(int idSociete, CancellationToken cancellationToken = default);
        Task<ConfigSociete?> GetBySocieteAsync(int idSociete, CancellationToken cancellationToken = default);
        Task<ConfigSociete> UpdateAsync(int idSociete, ConfigSocieteUpdateDto dto, CancellationToken cancellationToken = default);
    }
}
