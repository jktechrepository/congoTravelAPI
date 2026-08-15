using CongoTravel.Models;

namespace CongoTravel.Services.Repositories
{
    public interface IUtilisateurRepository
    {
        Task<IEnumerable<Utilisateur>> GetAllAsync();
        Task<Utilisateur> GetByIdAsync(int id);
        Task<Utilisateur> GetByEmailAsync(string email);
        Task<Utilisateur> GetByDefaultUsernameAsync(string defaultUsername);
        Task<Utilisateur> GetByReferenceAsync(Guid reference);
        Task<IEnumerable<Utilisateur>> GetBySocieteAsync(int idSociete);
        Task<IEnumerable<Utilisateur>> GetByRoleAsync(int idRole);
        Task<IEnumerable<Utilisateur>> GetByStatutAsync(bool statut);
        Task<IEnumerable<Utilisateur>> GetByConnecteAsync(bool isConnecte);
        Task<IEnumerable<Utilisateur>> GetByDateCreationAsync(DateTime date);
        Task<IEnumerable<Utilisateur>> GetByDateRangeAsync(DateTime dateDebut, DateTime dateFin);
        Task<IEnumerable<Utilisateur>> GetByNomCompletAsync(string nomComplet);
        Task<IEnumerable<Utilisateur>> GetByTelephoneAsync(string telephone);
        Task<Utilisateur> CreateAsync(Utilisateur utilisateur);
        Task<Utilisateur> UpdateAsync(Utilisateur utilisateur);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> ExistsByEmailAsync(string email);
        Task<bool> ExistsByReferenceAsync(Guid reference);
        Task<bool> AuthentifierAsync(string email, string motDePasse);
        Task<bool> ChangerMotDePasseAsync(int id, string ancienMotDePasse, string nouveauMotDePasse);
        Task<bool> MarquerCommeConnecteAsync(int id);
        Task<bool> MarquerCommeDeconnecteAsync(int id);
        Task<IEnumerable<Notification>> GetNotificationsEnvoyeesAsync(int idUtilisateur);
        Task<IEnumerable<Notification>> GetNotificationsRecuesAsync(int idUtilisateur);
        
        // ✅ SOFT DELETE
        Task<bool> ToggleStatutAsync(int id);
        Task<bool> DeactivateAsync(int id);
        
        // ✅ RÉINITIALISATION MOT DE PASSE
        Task<int> ReinitialiserMotDePasseMasseAsync(int idSociete, int idRole, string nouveauMotDePasse);
        Task<bool> ReinitialiserMotDePasseIndividuelAsync(int idUtilisateur, string nouveauMotDePasse);

        // ✅ MULTI-RÔLES : gestion des rôles utilisateur
        Task<bool> AddRoleToUserAsync(int userId, int roleId, int? assignedByUserId = null, bool isPrimary = false);
        Task<bool> RemoveRoleFromUserAsync(int userId, int roleId);
    }
}
