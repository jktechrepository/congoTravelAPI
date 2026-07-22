using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Services
{
    public class RoleService : IRoleRepository
    {
        private readonly CongoTravelDbContext _context;

        public RoleService(CongoTravelDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Récupère tous les rôles actifs, triés par nom
        /// </summary>
        public async Task<IEnumerable<Role>> GetAllAsync()
        {
            return await _context.Roles
                .Where(r => r.Statut == true) // ✅ Filtrer uniquement les rôles actifs
                .OrderBy(r => r.Niveau) // Trier par niveau hiérarchique
                .ThenBy(r => r.Nom) // Puis par nom
                .ToListAsync();
        }

        public async Task<IEnumerable<Role>> GetAllAsync(string nomRole)
        {
            if (!string.IsNullOrEmpty(nomRole))
            {
                if (nomRole == "Super-Admin")
                {
                    return await _context.Roles
                        .Where(r => r.Statut == true) // ✅ Filtrer uniquement les rôles actifs
                        .OrderBy(r => r.Nom)
                        .ToListAsync();
                }
                else if (nomRole == "Admin")
                {
                    return await _context.Roles
                        .Where(r => r.Statut == true) // ✅ Filtrer uniquement les rôles actifs
                        .Where(r => r.Nom != "Super-Admin")
                        .OrderBy(r => r.Nom)
                        .ToListAsync();
                }
                else if (nomRole == "Gerant")
                {
                    return await _context.Roles
                        .Where(r => r.Statut == true) // ✅ Filtrer uniquement les rôles actifs
                        .Where(r => r.Nom != "Super-Admin" && r.Nom != "Admin")
                        .OrderBy(r => r.Nom)
                        .ToListAsync();
                }
                else
                {
                    return await _context.Roles
                        .Where(r => r.Statut == true) // ✅ Filtrer uniquement les rôles actifs
                        .Where(r => r.Nom != "Super-Admin" && r.Nom != "Admin" && r.Nom != "Gerant")
                        .OrderBy(r => r.Nom)
                        .ToListAsync();
                }

            }
            else
            {
                return null;
            }
        }

        public async Task<Role> GetByIdAsync(int id)
        {
            return await _context.Roles
                .Include(r => r.Utilisateurs)
                .FirstOrDefaultAsync(r => r.IdRole == id);
        }

        public async Task<Role> GetByNomAsync(string nom)
        {
            return await _context.Roles
                .Include(r => r.Utilisateurs)
                .FirstOrDefaultAsync(r => r.Nom == nom);
        }

        public async Task<Role> CreateAsync(Role role)
        {
            role.DateCreation = DateTime.Now;
            
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
            return role;
        }

        public async Task<Role> UpdateAsync(Role role)
        {
            var existingRole = await _context.Roles.FindAsync(role.IdRole);
            if (existingRole == null)
                return null;

            _context.Entry(existingRole).CurrentValues.SetValues(role);
            await _context.SaveChangesAsync();
            return existingRole;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
                return false;

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Roles.AnyAsync(r => r.IdRole == id);
        }

        public async Task<bool> ExistsByNomAsync(string nom)
        {
            return await _context.Roles.AnyAsync(r => r.Nom == nom);
        }

        public async Task<IEnumerable<Utilisateur>> GetUtilisateursAsync(int idRole)
        {
            return await _context.Utilisateurs
                .Include(u => u.Societe)
                .Where(u => u.IdRole == idRole)
                .OrderByDescending(u => u.DateCreation)
                .ToListAsync();
        }

        // ✅ SOFT DELETE: Toggle le statut d'un rôle (actif <-> inactif)
        public async Task<bool> ToggleStatutAsync(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
                return false;

            role.Statut = role.Statut != true;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
