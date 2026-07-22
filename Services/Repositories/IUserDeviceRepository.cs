using CongoTravel.Models;

namespace CongoTravel.Services.Repositories
{
    /// <summary>
    /// Interface pour le repository des appareils utilisateurs
    /// </summary>
    public interface IUserDeviceRepository
    {
        Task<IEnumerable<UserDevice>> GetAllAsync();
        Task<UserDevice?> GetByIdAsync(int id);
        Task<UserDevice?> GetByFcmTokenAsync(string fcmToken);
        Task<IEnumerable<UserDevice>> GetByUtilisateurIdAsync(int idUtilisateur);
        Task<IEnumerable<string>> GetActiveTokensByUtilisateurIdAsync(int idUtilisateur);
        Task<IEnumerable<string>> GetActiveTokensByRoleAsync(int idRole);
        Task<IEnumerable<string>> GetActiveTokensBySocieteAsync(int idSociete);
        Task<IEnumerable<string>> GetActiveTokensByClasseAsync(int idClasse);
        Task<UserDevice> CreateAsync(UserDevice userDevice);
        Task<UserDevice> CreateOrUpdateAsync(int idUtilisateur, string fcmToken, string? deviceType = null, string? deviceModel = null, string? osVersion = null);
        Task<UserDevice?> UpdateAsync(UserDevice userDevice);
        Task<bool> DeleteAsync(int id);
        Task<bool> DeleteByFcmTokenAsync(string fcmToken);
        Task<bool> ExistsAsync(int id);
        Task<bool> ExistsByFcmTokenAsync(string fcmToken);
    }
}
