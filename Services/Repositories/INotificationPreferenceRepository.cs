using CongoTravel.Models;

namespace CongoTravel.Services.Repositories
{
    public interface INotificationPreferenceRepository
    {
        Task<NotificationPreference?> GetByUtilisateurAsync(int idUtilisateur);
        Task<NotificationPreference> CreateOrUpdateAsync(NotificationPreference preference);
        Task<bool> DeleteAsync(int idUtilisateur);
    }
}

