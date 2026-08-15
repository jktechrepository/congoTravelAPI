using CongoTravel.Models;

namespace CongoTravel.Services.Repositories
{
    /// <summary>
    /// Interface pour le repository des notifications
    /// </summary>
    public interface INotificationRepository
    {
        Task<IEnumerable<Notification>> GetAllAsync();
        Task<Notification?> GetByIdAsync(int id);
        Task<IEnumerable<Notification>> GetByDestinataireAsync(int idDestinataire);
        Task<IEnumerable<Notification>> GetByExpediteurAsync(int idExpediteur);
        Task<IEnumerable<Notification>> GetBySocieteAsync(int idSociete);
        Task<IEnumerable<Notification>> GetByClasseAsync(int idClasse);
        Task<IEnumerable<Notification>> GetByTypeAsync(string type);
        Task<IEnumerable<Notification>> GetNonLuesAsync(int idDestinataire);
        Task<Notification> CreateAsync(Notification notification);
        Task<Notification?> UpdateAsync(Notification notification);
        Task<bool> MarquerCommeLueAsync(int id);
        Task<bool> MarquerToutesCommeLuesAsync(int idDestinataire);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}
