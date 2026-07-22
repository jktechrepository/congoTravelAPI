using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Pagination;

namespace CongoTravel.Services.Repositories
{
    public interface IAgentRepository
    {
        Task<IEnumerable<Agent>> GetAllAsync();
        Task<Agent> GetByIdAsync(int id);
        Task<Agent> GetByMatriculeAsync(string matricule);
        Task<IEnumerable<Agent>> GetBySocieteAsync(int idSociete);
        Task<PagedResult<Agent>> GetPagedAsync(int idSociete, PagedRequest request);
      //  Task<IEnumerable<Agent>> GetBySpecialiteAsync(string specialite);
        Task<IEnumerable<Agent>> GetByStatutAsync(bool statut);
        Task<Agent> CreateAsync(Agent agent);
        Task<IEnumerable<Agent>> CreateBatchAsync(IEnumerable<Agent> agents);
        Task<Agent> UpdateAsync(Agent agent);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> ExistsByMatriculeAsync(string matricule);
        Task<bool> ExistsByEmailAsync(string email); // ✅ UNICITÉ EMAIL
        Task<bool> ExistsBySerialNumberAsync(string serialNumber); // ✅ UNICITÉ SERIAL NUMBER
        
        // ✅ SOFT DELETE
        Task<bool> ToggleStatutAsync(int id);

        // ✅ MISE À JOUR DU SERIAL NUMBER
        Task<bool> UpdateSerialNumberByIdAsync(int idAgent, string serialNumber);
        Task<bool> UpdateSerialNumberByMatriculeAsync(string matricule, string serialNumber);
        Task<Agent> GetBySerialNumberAsync(string serialNumber);
        
        // ✅ MULTI-RÔLES : Ajouter un rôle à un agent
        /// <summary>
        /// Ajoute un rôle à un agent en ajoutant le rôle à l'utilisateur associé
        /// Le RoleAgent correspond au Nom du rôle dans la table Roles
        /// </summary>
        /// <param name="idAgent">ID de l'agent</param>
        /// <param name="roleAgent">Nom du rôle à ajouter (correspond au champ Nom de la table Roles)</param>
        /// <param name="isPrimary">Indique si ce rôle doit être défini comme rôle principal</param>
        /// <param name="assignedByUserId">ID de l'utilisateur qui effectue l'assignation (optionnel)</param>
        /// <returns>True si le rôle a été ajouté avec succès, False sinon</returns>
        Task<bool> AddRoleToAgentAsync(int idAgent, string roleAgent, bool isPrimary = false, int? assignedByUserId = null);

        // ✅ MULTI-RÔLES : Ajouter plusieurs rôles à un agent
        /// <summary>
        /// Ajoute plusieurs rôles à un agent en une seule opération
        /// Le RoleAgent correspond au Nom du rôle dans la table Roles
        /// </summary>
        /// <param name="idAgent">ID de l'agent</param>
        /// <param name="roles">Liste des rôles à ajouter (chaque élément contient RoleAgent et IsPrimary)</param>
        /// <param name="assignedByUserId">ID de l'utilisateur qui effectue l'assignation (optionnel)</param>
        /// <returns>Résultat détaillé avec les rôles ajoutés avec succès et ceux qui ont échoué</returns>
        Task<AddRolesResult> AddRolesToAgentAsync(int idAgent, IEnumerable<(string RoleAgent, bool IsPrimary)> roles, int? assignedByUserId = null);

        // ✅ MULTI-RÔLES : Remplacer un RoleAgent par un autre
        /// <summary>
        /// Remplace un RoleAgent par un autre pour un agent
        /// Le statut IsPrimary n'est pas modifié lors du remplacement
        /// </summary>
        /// <param name="idAgent">ID de l'agent</param>
        /// <param name="ancienRoleAgent">Ancien RoleAgent à remplacer (correspond au champ Nom de la table Roles)</param>
        /// <param name="nouveauRoleAgent">Nouveau RoleAgent à affecter (correspond au champ Nom de la table Roles)</param>
        /// <param name="assignedByUserId">ID de l'utilisateur qui effectue le remplacement (optionnel)</param>
        /// <returns>True si le remplacement a réussi, False sinon</returns>
        Task<bool> ReplaceRoleAgentAsync(int idAgent, string ancienRoleAgent, string nouveauRoleAgent, int? assignedByUserId = null);
    }
}

