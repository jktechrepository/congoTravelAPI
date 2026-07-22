using CongoTravel.Data;
using CongoTravel.Models;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace CongoTravel.Services
{
    public class UtilisateurService : IUtilisateurRepository
    {
        private readonly CongoTravelDbContext _context;

        public UtilisateurService(CongoTravelDbContext context)
        {
            _context = context;
        }

        // ═══════════════════════════════════════════════════════════════════
        // ✅ MULTI-RÔLES : Attribution et retrait des rôles utilisateur
        // ═══════════════════════════════════════════════════════════════════

        public async Task<bool> AddRoleToUserAsync(int userId, int roleId, int? assignedByUserId = null, bool isPrimary = false)
        {
            var user = await _context.Utilisateurs.FindAsync(userId);
            if (user == null) return false;

            var role = await _context.Roles.FindAsync(roleId);
            if (role == null || role.Statut != true) return false;

            var existing = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.IdUtilisateur == userId && ur.IdRole == roleId);

            if (existing != null)
            {
                // Réactiver si désactivé
                existing.Statut = true;
                existing.DateAttribution = DateTime.Now;
                existing.IdUtilisateurAttribution = assignedByUserId;
                if (isPrimary)
                {
                    // Désigner comme principal et retirer le flag des autres
                    // ✅ EF Core 6.0 : Charger les entités, modifier, puis sauvegarder
                    var otherUserRoles = await _context.UserRoles
                        .Where(ur => ur.IdUtilisateur == userId && ur.IdRole != roleId)
                        .ToListAsync();
                    foreach (var ur in otherUserRoles)
                    {
                        ur.IsPrimary = false;
                    }
                    existing.IsPrimary = true;
                }
            }
            else
            {
                if (isPrimary)
                {
                    // ✅ EF Core 6.0 : Charger les entités, modifier, puis sauvegarder
                    var otherUserRoles = await _context.UserRoles
                        .Where(ur => ur.IdUtilisateur == userId)
                        .ToListAsync();
                    foreach (var ur in otherUserRoles)
                    {
                        ur.IsPrimary = false;
                    }
                }

                _context.UserRoles.Add(new UserRole
                {
                    IdUtilisateur = userId,
                    IdRole = roleId,
                    IsPrimary = isPrimary,
                    DateAttribution = DateTime.Now,
                    IdUtilisateurAttribution = assignedByUserId,
                    Statut = true
                });
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveRoleFromUserAsync(int userId, int roleId)
        {
            var existing = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.IdUtilisateur == userId && ur.IdRole == roleId);
            if (existing == null) return false;

            // Vérifier qu'il reste au moins un rôle actif après le retrait
            var activeRolesCount = await _context.UserRoles
                .CountAsync(ur => ur.IdUtilisateur == userId && ur.Statut == true);
            
            if (activeRolesCount <= 1)
            {
                // Ne peut pas retirer le dernier rôle actif
                return false;
            }

            // Soft delete
            existing.Statut = false;
            existing.IsPrimary = false;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Utilisateur>> GetAllAsync()
        {
            return await _context.Utilisateurs
                .Include(u => u.Societe)
                .Include(u => u.Role)
                .Where(u => u.Statut == true)
                .OrderByDescending(u => u.DateCreation)
                .ToListAsync();
        }

        public async Task<Utilisateur> GetByIdAsync(int id)
        {
            return await _context.Utilisateurs
                .Include(u => u.Role)
                .Include(u => u.Societe)
                .FirstOrDefaultAsync(u => u.IdUtilisateur == id);
        }

        public async Task<Utilisateur> GetByEmailAsync(string email)
        {
            return await _context.Utilisateurs
                .Include(u => u.Societe) // ✨ NOUVEAU : Inclure l'école pour vérifier son statut
                .Where(u => u.Statut == true) // ✅ Filtrer uniquement les utilisateurs actifs
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<Utilisateur> GetByDefaultUsernameAsync(string defaultUsername)
        {
            return await _context.Utilisateurs
                .Include(u => u.Societe) // ✨ Inclure l'école pour vérifier son statut
                .Where(u => u.Statut == true) // ✅ Filtrer uniquement les utilisateurs actifs
                .FirstOrDefaultAsync(u => u.DefaultUsername == defaultUsername);
        }

        public async Task<Utilisateur> GetByReferenceAsync(Guid reference)
        {
            return await _context.Utilisateurs
                .Include(u => u.Societe)
                .Include(u => u.Role)
                .Where(u => u.Statut == true)
                .FirstOrDefaultAsync(u => u.ReferenceUtilisateur == reference);
        }

        public async Task<IEnumerable<Utilisateur>> GetBySocieteAsync(int idSociete)
        {
            return await _context.Utilisateurs
                .Include(u => u.Role)
                .Include(u => u.Societe)
                .Where(u => u.IdSociete == idSociete && u.Statut == true)
                .OrderBy(u => u.NomComplet)
                .ToListAsync();
        }

        public async Task<IEnumerable<Utilisateur>> GetByRoleAsync(int idRole)
        {
            return await _context.Utilisateurs
                .Include(u => u.Societe)
                .Include(u => u.Role)
                .Where(u => u.IdRole == idRole && u.Statut == true)
                .OrderByDescending(u => u.DateCreation)
                .ToListAsync();
        }

        public async Task<IEnumerable<Utilisateur>> GetByStatutAsync(bool statut)
        {
            return await _context.Utilisateurs
                .Include(u => u.Societe)
                .Include(u => u.Role)
                .Where(u => u.Statut == statut)
                .OrderByDescending(u => u.DateCreation)
                .ToListAsync();
        }

        public async Task<IEnumerable<Utilisateur>> GetByConnecteAsync(bool isConnecte)
        {
            return await _context.Utilisateurs
                .Include(u => u.Societe)
                .Include(u => u.Role)
                .Where(u => u.IsConnecte == isConnecte && u.Statut == true)
                .OrderByDescending(u => u.DateCreation)
                .ToListAsync();
        }

        public async Task<IEnumerable<Utilisateur>> GetByDateCreationAsync(DateTime date)
        {
            return await _context.Utilisateurs
                .Include(u => u.Societe)
                .Include(u => u.Role)
                .Where(u => u.DateCreation.Date == date.Date && u.Statut == true)
                .OrderByDescending(u => u.DateCreation)
                .ToListAsync();
        }

        public async Task<IEnumerable<Utilisateur>> GetByDateRangeAsync(DateTime dateDebut, DateTime dateFin)
        {
            return await _context.Utilisateurs
                .Include(u => u.Societe)
                .Include(u => u.Role)
                .Where(u => u.DateCreation >= dateDebut && u.DateCreation <= dateFin)
                .OrderByDescending(u => u.DateCreation)
                .ToListAsync();
        }

        public async Task<IEnumerable<Utilisateur>> GetByNomCompletAsync(string nomComplet)
        {
            return await _context.Utilisateurs
                .Include(u => u.Societe)
                .Include(u => u.Role)
                .Where(u => u.Statut == true && 
                    (u.NomComplet ?? "")
                    .Contains(nomComplet, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(u => u.DateCreation)
                .ToListAsync();
        }

        public async Task<IEnumerable<Utilisateur>> GetByTelephoneAsync(string telephone)
        {
            return await _context.Utilisateurs
                .Include(u => u.Societe)
                .Include(u => u.Role)
                .Where(u => u.Telephone == telephone)
                .Where(u => u.Statut == true) // ✅ Filtrer uniquement les utilisateurs actifs
                .OrderByDescending(u => u.DateCreation)
                .ToListAsync();
        }

        public async Task<Utilisateur> CreateAsync(Utilisateur utilisateur)
        {
            // ✅ UNICITÉ EMAIL: Vérifier que l'email n'existe pas déjà
            if (!string.IsNullOrEmpty(utilisateur.Email))
            {
                var emailExists = await ExistsByEmailAsync(utilisateur.Email);
                if (emailExists)
                {
                    throw new InvalidOperationException(
                        $"Un utilisateur avec l'email '{utilisateur.Email}' existe déjà. " +
                        $"Chaque email doit être unique dans le système."
                    );
                }
            }
            
            utilisateur.ReferenceUtilisateur = Guid.NewGuid();
            utilisateur.DateCreation = DateTime.Now;
            utilisateur.Statut = true;
            utilisateur.IsConnecte = false;
            
            if (!string.IsNullOrEmpty(utilisateur.MotDePasseHash))
            {
                utilisateur.MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(utilisateur.MotDePasseHash);
            }
            
            _context.Utilisateurs.Add(utilisateur);
            await _context.SaveChangesAsync();
            return utilisateur;
        }

        public async Task<Utilisateur> UpdateAsync(Utilisateur utilisateur)
        {
            var existingUtilisateur = await _context.Utilisateurs.FindAsync(utilisateur.IdUtilisateur);
            if (existingUtilisateur == null)
                return null;

            // ✅ UNICITÉ EMAIL: Vérifier que le nouvel email n'est pas déjà utilisé par un autre utilisateur
            if (!string.IsNullOrEmpty(utilisateur.Email) && utilisateur.Email != existingUtilisateur.Email)
            {
                var emailExistsByOtherUser = await _context.Utilisateurs
                    .AnyAsync(u => u.Email == utilisateur.Email && u.IdUtilisateur != utilisateur.IdUtilisateur);
                
                if (emailExistsByOtherUser)
                {
                    throw new InvalidOperationException(
                        $"Un autre utilisateur avec l'email '{utilisateur.Email}' existe déjà. " +
                        $"Chaque email doit être unique dans le système."
                    );
                }
            }

            // Sauvegarder les anciennes valeurs pour la synchronisation
            var oldNomComplet = existingUtilisateur.NomComplet;
            var oldTelephone = existingUtilisateur.Telephone;
            var oldEmail = existingUtilisateur.Email;
            var oldGenre = existingUtilisateur.Genre;
            var oldAdresseResidence = existingUtilisateur.AdresseResidence;
            var oldPhotoUrl = existingUtilisateur.PhotoUrl;
            var oldIdClient = existingUtilisateur.IdClient;
            var oldIdAgent = existingUtilisateur.IdAgent;

            // Sauvegarder l'ancien mot de passe pour ne pas l'écraser
            var ancienMotDePasseHash = existingUtilisateur.MotDePasseHash;
            Console.WriteLine("Ancien mot de passe conservé : " + ancienMotDePasseHash);

            // Mettre à jour tous les champs sauf le mot de passe
            _context.Entry(existingUtilisateur).CurrentValues.SetValues(utilisateur);
            
            // Restaurer l'ancien mot de passe (il ne doit pas être modifié via cette méthode)
            existingUtilisateur.MotDePasseHash = ancienMotDePasseHash;
            Console.WriteLine("Mot de passe reaffecté : " + existingUtilisateur.MotDePasseHash);

            await _context.SaveChangesAsync();

            // ✨ SYNCHRONISATION: Mettre à jour le Client associé si les champs pertinents ont changé
            // Utiliser IdClient de l'utilisateur mis à jour (peut avoir changé)
            var idClientASynchroniser = utilisateur.IdClient ?? oldIdClient;

            if (idClientASynchroniser.HasValue)
            {
                var champsModifies = 
                    oldNomComplet != utilisateur.NomComplet ||
                    oldTelephone != utilisateur.Telephone ||
                    oldEmail != utilisateur.Email ||
                    oldGenre != utilisateur.Genre ||
                    oldAdresseResidence != utilisateur.AdresseResidence;

                if (champsModifies)
                {
                    var client = await _context.Clients.FindAsync(idClientASynchroniser.Value);
                    if (client != null)
                    {
                        bool clientModifie = false;

                        // Synchroniser uniquement les champs qui ont changé
                        if (oldNomComplet != utilisateur.NomComplet && !string.IsNullOrWhiteSpace(utilisateur.NomComplet))
                        {
                            client.NomClient = utilisateur.NomComplet;
                            clientModifie = true;
                        }
                        if (oldTelephone != utilisateur.Telephone)
                        {
                            // Vérifier l'unicité du téléphone avant de synchroniser
                            if (!string.IsNullOrWhiteSpace(utilisateur.Telephone))
                            {
                                var telephoneDejaUtilise = await _context.Clients
                                    .AnyAsync(c => c.Telephone == utilisateur.Telephone && c.IdClient != client.IdClient);
                                
                                if (!telephoneDejaUtilise)
                                {
                                    client.Telephone = utilisateur.Telephone;
                                    clientModifie = true;
                                }
                                else
                                {
                                    Console.WriteLine($"⚠️ Téléphone '{utilisateur.Telephone}' non synchronisé pour le client {client.IdClient} car déjà utilisé par un autre client");
                                }
                            }
                            else
                            {
                                // Si le téléphone devient null/vide, on peut le synchroniser
                                client.Telephone = utilisateur.Telephone;
                                clientModifie = true;
                            }
                        }
                        if (oldEmail != utilisateur.Email)
                        {
                            // Vérifier l'unicité de l'email avant de synchroniser
                            var emailDejaUtilise = await _context.Clients
                                .AnyAsync(c => c.EmailClient == utilisateur.Email && c.IdClient != client.IdClient);
                            
                            if (!emailDejaUtilise && !string.IsNullOrWhiteSpace(utilisateur.Email))
                            {
                                client.EmailClient = utilisateur.Email;
                                clientModifie = true;
                            }
                            else if (emailDejaUtilise)
                            {
                                Console.WriteLine($"⚠️ Email '{utilisateur.Email}' non synchronisé pour le client {client.IdClient} car déjà utilisé par un autre client");
                            }
                        }
                        if (oldGenre != utilisateur.Genre)
                        {
                            client.GenreClient = utilisateur.Genre;
                            clientModifie = true;
                        }
                        if (oldAdresseResidence != utilisateur.AdresseResidence)
                        {
                            client.AdresseClient = utilisateur.AdresseResidence ?? client.AdresseClient;
                            clientModifie = true;
                        }

                        if (clientModifie)
                        {
                            await _context.SaveChangesAsync();
                            Console.WriteLine($"✅ Synchronisation Utilisateur → Client: Client {client.IdClient} mis à jour");
                        }
                    }
                }
            }

            // ✨ SYNCHRONISATION: Mettre à jour l'Agent associé si les champs pertinents ont changé
            // Utiliser IdAgent de l'utilisateur mis à jour (peut avoir changé)
            var idAgentASynchroniser = utilisateur.IdAgent ?? oldIdAgent;

            if (idAgentASynchroniser.HasValue)
            {
                var champsModifies = 
                    oldNomComplet != utilisateur.NomComplet ||
                    oldTelephone != utilisateur.Telephone ||
                    oldEmail != utilisateur.Email ||
                    oldGenre != utilisateur.Genre ||
                    oldAdresseResidence != utilisateur.AdresseResidence ||
                    oldPhotoUrl != utilisateur.PhotoUrl;

                if (champsModifies)
                {
                    var agent = await _context.Agents.FindAsync(idAgentASynchroniser.Value);
                    if (agent != null)
                    {
                        bool agentModifie = false;

                        // Synchroniser uniquement les champs qui ont changé
                        if (oldNomComplet != utilisateur.NomComplet && !string.IsNullOrWhiteSpace(utilisateur.NomComplet))
                        {
                            agent.NomComplet = utilisateur.NomComplet;
                            agentModifie = true;
                        }
                        if (oldTelephone != utilisateur.Telephone)
                        {
                            // Vérifier l'unicité du téléphone avant de synchroniser
                            if (!string.IsNullOrWhiteSpace(utilisateur.Telephone))
                            {
                                var telephoneDejaUtilise = await _context.Agents
                                    .AnyAsync(a => a.TelephoneAgent == utilisateur.Telephone && a.IdAgent != agent.IdAgent);
                                
                                if (!telephoneDejaUtilise)
                                {
                                    agent.TelephoneAgent = utilisateur.Telephone;
                                    agentModifie = true;
                                }
                                else
                                {
                                    Console.WriteLine($"⚠️ Téléphone '{utilisateur.Telephone}' non synchronisé pour l'agent {agent.IdAgent} car déjà utilisé par un autre agent");
                                }
                            }
                            else
                            {
                                // Si le téléphone devient null/vide, on peut le synchroniser
                                agent.TelephoneAgent = utilisateur.Telephone;
                                agentModifie = true;
                            }
                        }
                        if (oldEmail != utilisateur.Email)
                        {
                            // Vérifier l'unicité de l'email avant de synchroniser
                            var emailDejaUtilise = await _context.Agents
                                .AnyAsync(a => a.EmailAgent == utilisateur.Email && a.IdAgent != agent.IdAgent);
                            
                            if (!emailDejaUtilise && !string.IsNullOrWhiteSpace(utilisateur.Email))
                            {
                                agent.EmailAgent = utilisateur.Email;
                                agentModifie = true;
                            }
                            else if (emailDejaUtilise)
                            {
                                Console.WriteLine($"⚠️ Email '{utilisateur.Email}' non synchronisé pour l'agent {agent.IdAgent} car déjà utilisé par un autre agent");
                            }
                        }
                        if (oldGenre != utilisateur.Genre)
                        {
                            agent.Genre = utilisateur.Genre;
                            agentModifie = true;
                        }
                        if (oldAdresseResidence != utilisateur.AdresseResidence)
                        {
                            agent.AdresseResidence = utilisateur.AdresseResidence ?? agent.AdresseResidence;
                            agentModifie = true;
                        }
                        if (oldPhotoUrl != utilisateur.PhotoUrl)
                        {
                            agent.PhotoUrl = utilisateur.PhotoUrl;
                            agentModifie = true;
                        }

                        if (agentModifie)
                        {
                            await _context.SaveChangesAsync();
                            Console.WriteLine($"✅ Synchronisation Utilisateur → Agent: Agent {agent.IdAgent} mis à jour");
                        }
                    }
                }
            }

            return existingUtilisateur;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var utilisateur = await _context.Utilisateurs.FindAsync(id);
            if (utilisateur == null)
                return false;

            // ✨ Soft delete : mettre Statut à false au lieu de supprimer
            utilisateur.Statut = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Utilisateurs.AnyAsync(u => u.IdUtilisateur == id);
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _context.Utilisateurs.AnyAsync(u => u.Email == email);
        }

        public async Task<bool> ExistsByReferenceAsync(Guid reference)
        {
            return await _context.Utilisateurs.AnyAsync(u => u.ReferenceUtilisateur == reference);
        }

        public async Task<bool> AuthentifierAsync(string email, string motDePasse)
        {
            var utilisateur = await _context.Utilisateurs
                .FirstOrDefaultAsync(u => u.Email == email);

            if (utilisateur == null)
                return false;

            // Vérifier si l'utilisateur est actif
            if (utilisateur.Statut != true)
                return false;

            return BCrypt.Net.BCrypt.Verify(motDePasse, utilisateur.MotDePasseHash);
        }

        public async Task<bool> ChangerMotDePasseAsync(int id, string ancienMotDePasse, string nouveauMotDePasse)
        {
            var utilisateur = await _context.Utilisateurs.FindAsync(id);
            if (utilisateur == null)
                return false;

            // Vérifier l'ancien mot de passe
            if (string.IsNullOrEmpty(utilisateur.MotDePasseHash))
                return false;

            bool ancienMotDePasseValide = BCrypt.Net.BCrypt.Verify(ancienMotDePasse, utilisateur.MotDePasseHash);
            if (!ancienMotDePasseValide)
                return false;

            // Changer le mot de passe
            utilisateur.MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(nouveauMotDePasse);
            
            // ✨ Mettre à jour le flag DoitChangerMotDePasse à false
            // Car l'utilisateur a changé son mot de passe
            utilisateur.DoitChangerMotDePasse = false;
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarquerCommeConnecteAsync(int id)
        {
            var utilisateur = await _context.Utilisateurs.FindAsync(id);
            if (utilisateur == null)
                return false;

            utilisateur.IsConnecte = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarquerCommeDeconnecteAsync(int id)
        {
            var utilisateur = await _context.Utilisateurs.FindAsync(id);
            if (utilisateur == null)
                return false;

            utilisateur.IsConnecte = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Notification>> GetNotificationsEnvoyeesAsync(int idUtilisateur)
        {
            return await _context.Notifications
                .Include(n => n.Destinataire)
                .Include(n => n.Societe)
                .Where(n => n.IdExpediteur == idUtilisateur)
                .OrderByDescending(n => n.DateCreation)
                .ToListAsync();
        }

        public async Task<IEnumerable<Notification>> GetNotificationsRecuesAsync(int idUtilisateur)
        {
            return await _context.Notifications
                .Include(n => n.Expediteur)
                .Include(n => n.Societe)
                .Where(n => n.IdDestinataire == idUtilisateur)
                .OrderByDescending(n => n.DateCreation)
                .ToListAsync();
        }


        // ✅ SOFT DELETE: Toggle le statut d'un utilisateur (actif <-> inactif)
        public async Task<bool> ToggleStatutAsync(int id)
        {
            var utilisateur = await _context.Utilisateurs.FindAsync(id);
            if (utilisateur == null)
                return false;

            utilisateur.Statut = utilisateur.Statut != true;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>Désactive un utilisateur actif (pas de toggle — réactivation réservée à un admin).</summary>
        public async Task<bool> DeactivateAsync(int id)
        {
            var utilisateur = await _context.Utilisateurs.FindAsync(id);
            if (utilisateur == null || utilisateur.Statut != true)
                return false;

            utilisateur.Statut = false;
            utilisateur.IsConnecte = false;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// ✅ Réinitialiser le mot de passe en masse pour tous les utilisateurs d'une école avec un rôle spécifique
        /// </summary>
        public async Task<int> ReinitialiserMotDePasseMasseAsync(int idSociete, int idRole, string nouveauMotDePasse)
        {
            // Récupérer tous les utilisateurs actifs de l'école avec le rôle spécifié
            var utilisateurs = await _context.Utilisateurs
                .Where(u => u.IdSociete == idSociete && u.IdRole == idRole && u.Statut == true)
                .ToListAsync();

            if (!utilisateurs.Any())
                return 0;

            // Hasher le nouveau mot de passe
            var nouveauHash = BCrypt.Net.BCrypt.HashPassword(nouveauMotDePasse);

            // Mettre à jour tous les utilisateurs
            foreach (var utilisateur in utilisateurs)
            {
                utilisateur.MotDePasseHash = nouveauHash;
                utilisateur.DoitChangerMotDePasse = true; // ✅ Forcer le changement au prochain login
            }

            await _context.SaveChangesAsync();

            return utilisateurs.Count;
        }

        /// <summary>
        /// ✅ Réinitialiser le mot de passe d'un utilisateur spécifique
        /// </summary>
        public async Task<bool> ReinitialiserMotDePasseIndividuelAsync(int idUtilisateur, string nouveauMotDePasse)
        {
            var utilisateur = await _context.Utilisateurs.FindAsync(idUtilisateur);

            if (utilisateur == null)
                return false;

            // Hasher le nouveau mot de passe
            var nouveauHash = BCrypt.Net.BCrypt.HashPassword(nouveauMotDePasse);

            // Mettre à jour l'utilisateur
            utilisateur.MotDePasseHash = nouveauHash;
            utilisateur.DoitChangerMotDePasse = true; // ✅ Forcer le changement au prochain login

            await _context.SaveChangesAsync();

            return true;
        }

    }
}
