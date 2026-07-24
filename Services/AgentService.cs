using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.Enums;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using CongoTravel.Models.DTOs.Pagination;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace CongoTravel.Services
{
    public class AgentService : IAgentRepository
    {
        private readonly CongoTravelDbContext _context;
        private readonly IUsernameGeneratorService _usernameGenerator;
        private readonly CongoTravel.Services.Repositories.IEmailService _emailService;
        private readonly IUtilisateurRepository _utilisateurRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<AgentService> _logger;

        private static readonly HashSet<string> SystemEmailsToExclude = new(StringComparer.OrdinalIgnoreCase)
        {
            "admin@kenergie.cd",
            "superadmin@kenergie.cd",
            "admin@congotravel.cd",
            "superadmin@congotravel.cd"
        };

        public AgentService(
            CongoTravelDbContext context,
            IUsernameGeneratorService usernameGenerator,
            CongoTravel.Services.Repositories.IEmailService emailService,
            IUtilisateurRepository utilisateurRepository,
            ICurrentUserService currentUser,
            ILogger<AgentService> logger)
        {
            _context = context;
            _usernameGenerator = usernameGenerator;
            _emailService = emailService;
            _utilisateurRepository = utilisateurRepository;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<IEnumerable<Agent>> GetAllAsync()
        {
            return await ApplyVisibilityFilters(_context.Agents.AsNoTracking())
                .Where(e => e.Statut == true)
                .OrderByDescending(e => e.DateCreation)
                .ToListAsync();
        }

        public async Task<Agent> GetByIdAsync(int id)
        {
            return await ApplyVisibilityFilters(_context.Agents)
                .Where(e => e.Statut == true)
                .FirstOrDefaultAsync(e => e.IdAgent == id);
        }

        public async Task<Agent> GetByMatriculeAsync(string matricule)
        {
            return await ApplyVisibilityFilters(_context.Agents)
                .Where(e => e.Statut == true)
                .FirstOrDefaultAsync(e => e.Matricule == matricule);
        }

        public async Task<IEnumerable<Agent>> GetBySocieteAsync(int idSociete)
        {
            var query = ApplyVisibilityFilters(_context.Agents.AsNoTracking())
                .Where(e => e.IdSociete == idSociete)
                .Where(e => e.Statut == true);

            // Non Super-Admin : ne peut cibler que sa société (déjà appliqué dans ApplyVisibilityFilters)
            return await query
                .OrderByDescending(e => e.DateCreation)
                .ToListAsync();
        }

        public async Task<IEnumerable<Agent>> GetByStatutAsync(bool statut)
        {
            return await ApplyVisibilityFilters(_context.Agents.AsNoTracking())
                .Where(e => e.Statut == statut)
                .OrderByDescending(e => e.DateCreation)
                .ToListAsync();
        }

        /// <summary>
        /// Filtre hiérarchie de rôles + tenancy société (sauf Super-Admin) + emails système.
        /// </summary>
        private IQueryable<Agent> ApplyVisibilityFilters(IQueryable<Agent> query)
        {
            query = query.Where(a =>
                string.IsNullOrWhiteSpace(a.EmailAgent) ||
                !SystemEmailsToExclude.Contains(a.EmailAgent.Trim()));

            var callerRole = _currentUser.PrimaryRole;
            var hiddenRoles = RoleVisibilityHelper.GetHiddenRoleNamesForCaller(callerRole);
            if (hiddenRoles.Count > 0)
            {
                var hiddenList = hiddenRoles.Select(r => r.ToLowerInvariant()).ToList();
                query = query.Where(a =>
                    string.IsNullOrEmpty(a.RoleAgent) ||
                    !hiddenList.Contains(a.RoleAgent.ToLower()));
            }

            if (!_currentUser.IsSuperAdmin)
            {
                var idSociete = _currentUser.SocieteId;
                query = query.Where(a => a.IdSociete == idSociete);
            }

            return query;
        }
        public async Task<Agent> CreateAsync(Agent agent)
        {
            NormalizeAndValidateAgentContactForCreate(agent);

            // ✅ UNICITÉ EMAIL AGENT: Vérifier que l'email n'existe pas déjà
            if (!string.IsNullOrEmpty(agent.EmailAgent))
            {
                var emailExists = await ExistsByEmailAsync(agent.EmailAgent);
                if (emailExists)
                {
                    throw new InvalidOperationException(
                        $"Un agent avec l'email '{agent.EmailAgent}' existe déjà. " +
                        $"Chaque email agent doit être unique dans le système."
                    );
                }
            }

            // ✅ UNICITÉ SERIAL NUMBER AGENT: Vérifier que le SerialNumber n'existe pas déjà
            if (!string.IsNullOrEmpty(agent.SerialNumber))
            {
                var serialNumberExists = await ExistsBySerialNumberAsync(agent.SerialNumber);
                if (serialNumberExists)
                {
                    throw new InvalidOperationException(
                        $"Un agent avec le SerialNumber '{agent.SerialNumber}' existe déjà. " +
                        $"Chaque SerialNumber doit être unique dans le système. " +
                        $"Cet appareil est peut-être déjà lié à un autre agent."
                    );
                }
            }
            
            agent.DateCreation = DateTime.Now;
            
            // ✨ NOUVEAU : Générer le matricule automatiquement s'il n'est pas fourni
            if (string.IsNullOrWhiteSpace(agent.Matricule))
            {
                // Récupérer le nom de l'école
                var societe = await _context.Societes.FindAsync(agent.IdSociete);
                string nomSociete = societe?.Nom ?? "Societe";
                
                agent.Matricule = await GenerateMatriculeAgent(agent, nomSociete);
                Console.WriteLine($"✨ Matricule généré automatiquement pour l'agent: {agent.Matricule}");
            }
            
            _context.Agents.Add(agent);
            await _context.SaveChangesAsync();

            // ✨ NOUVEAU : Créer automatiquement un compte utilisateur pour l'agent
            try
            {
                _logger.LogInformation("🔍 Début de la création automatique du compte utilisateur pour l'agent {AgentId} (Email: {Email}, Fonction: {Fonction})", 
                    agent.IdAgent, agent.EmailAgent, agent.Fonction);
                
                var result = await CreateDefaultAgentUserAsync(agent);
                if (result == null)
                {
                    _logger.LogWarning("⚠️ CreateDefaultAgentUserAsync a retourné null pour l'agent {AgentId}", agent.IdAgent);
                }
                else
                {
                    _logger.LogInformation("✅ Compte utilisateur créé/mis à jour pour l'agent {AgentId} (IdUtilisateur: {UserId})", 
                        agent.IdAgent, result.IdUtilisateur);
                }
            }
            catch (Exception ex)
            {
                // Log l'erreur mais ne pas faire échouer la création de l'agent
                _logger.LogError(ex, "❌ ERREUR lors de la création automatique du compte utilisateur pour l'agent {AgentId}: {ErrorMessage}", 
                    agent.IdAgent, ex.Message);
            }

            return agent;
        }
        public async Task<IEnumerable<Agent>> CreateBatchAsync(IEnumerable<Agent> agents)
        {
            var agentList = agents.ToList();
            var createdAgents = new List<Agent>();
            var errors = new List<string>();

            // Récupérer l'école une fois pour tous les agents
            var firstAgent = agentList.FirstOrDefault();
            if (firstAgent?.IdSociete == null)
            {
                throw new InvalidOperationException("Tous les agents doivent avoir un IdSociete");
            }

            var societe = await _context.Societes.FindAsync(firstAgent.IdSociete);
            string nomSociete = societe?.Nom ?? "Societe";

            foreach (var agent in agentList)
            {
                try
                {
                    NormalizeAndValidateAgentContactForCreate(agent);

                    // ✅ UNICITÉ EMAIL AGENT: Vérifier que l'email n'existe pas déjà
                    if (!string.IsNullOrEmpty(agent.EmailAgent))
                    {
                        var emailExists = await ExistsByEmailAsync(agent.EmailAgent);
                        if (emailExists)
                        {
                            errors.Add($"Agent {agent.NomComplet ?? "Agent"}: Email '{agent.EmailAgent}' existe déjà");
                            continue;
                        }
                    }

                    // ✅ UNICITÉ SERIAL NUMBER AGENT: Vérifier que le SerialNumber n'existe pas déjà
                    if (!string.IsNullOrEmpty(agent.SerialNumber))
                    {
                        var serialNumberExists = await ExistsBySerialNumberAsync(agent.SerialNumber);
                        if (serialNumberExists)
                        {
                            errors.Add($"Agent {agent.NomComplet ?? "Agent"}: SerialNumber '{agent.SerialNumber}' existe déjà");
                            continue;
                        }
                    }

                    agent.DateCreation = DateTime.Now;

                    // ✨ Générer le matricule automatiquement s'il n'est pas fourni
                    if (string.IsNullOrWhiteSpace(agent.Matricule))
                    {
                        agent.Matricule = await GenerateMatriculeAgent(agent, nomSociete);
                    }

                    _context.Agents.Add(agent);
                    createdAgents.Add(agent);
                }
                catch (Exception ex)
                {
                    errors.Add($"Agent {agent.NomComplet ?? "Agent"}: {ex.Message}");
                }
            }

            // Sauvegarder tous les agents valides
            if (createdAgents.Any())
            {
                await _context.SaveChangesAsync();

                // ✨ Créer automatiquement les comptes utilisateurs pour les agents créés
                foreach (var agent in createdAgents)
                {
                    try
                    {
                        await CreateDefaultAgentUserAsync(agent);
                    }
                    catch (Exception ex)
                    {
                        // Log l'erreur mais ne pas faire échouer la création de l'agent
                        Console.WriteLine($"Erreur lors de la création automatique du compte utilisateur pour l'agent {agent.NomComplet ?? "Agent"}: {ex.Message}");
                    }
                }
            }

            if (errors.Any())
            {
                Console.WriteLine($"⚠️ Erreurs lors de la création par lot: {string.Join("; ", errors)}");
            }

            return createdAgents;
        }
        public async Task<Agent> UpdateAsync(Agent agent)
        {
            var existingAgent = await _context.Agents.FindAsync(agent.IdAgent);
            if (existingAgent == null)
                return null;

            var previousRoleAgent = existingAgent.RoleAgent;

            // Sauvegarder les anciennes valeurs pour la synchronisation
            var oldNomComplet = existingAgent.NomComplet;
            var oldTelephoneAgent = existingAgent.TelephoneAgent;
            var oldEmailAgent = existingAgent.EmailAgent;
            var oldGenre = existingAgent.Genre;
            var oldPhotoUrl = existingAgent.PhotoUrl;
            var oldAdresseResidence = existingAgent.AdresseResidence;

            // ✅ UNICITÉ EMAIL AGENT: Vérifier que le nouvel email n'est pas déjà utilisé par un autre agent
            if (!string.IsNullOrEmpty(agent.EmailAgent) && agent.EmailAgent != existingAgent.EmailAgent)
            {
                var emailExistsByOtherAgent = await _context.Agents
                    .AnyAsync(a => a.EmailAgent == agent.EmailAgent && a.IdAgent != agent.IdAgent);
                
                if (emailExistsByOtherAgent)
                {
                    throw new InvalidOperationException(
                        $"Un autre agent avec l'email '{agent.EmailAgent}' existe déjà. " +
                        $"Chaque email agent doit être unique dans le système."
                    );
                }
            }

            _context.Entry(existingAgent).CurrentValues.SetValues(agent);
            await _context.SaveChangesAsync();

            // ✨ SYNCHRONISATION: Mettre à jour les Utilisateurs liés si les champs pertinents ont changé
            var champsModifies = 
                oldNomComplet != agent.NomComplet ||
                oldTelephoneAgent != agent.TelephoneAgent ||
                oldEmailAgent != agent.EmailAgent ||
                oldGenre != agent.Genre ||
                oldPhotoUrl != agent.PhotoUrl ||
                oldAdresseResidence != agent.AdresseResidence;

            if (champsModifies)
            {
                await SyncAgentUtilisateurAsync(existingAgent, previousRoleAgent);
                await _context.SaveChangesAsync();
                _logger.LogInformation(
                    "✅ Synchronisation Agent → Utilisateur: Utilisateur mis à jour pour l'agent {AgentId}",
                    agent.IdAgent);
            }
            else
            {
                // Si seuls les rôles ont changé, synchroniser quand même
                await SyncAgentUtilisateurAsync(existingAgent, previousRoleAgent);
                await _context.SaveChangesAsync();
            }

            return existingAgent;
        }
        public async Task<bool> DeleteAsync(int id)
        {
            var agent = await _context.Agents.FindAsync(id);
            if (agent == null)
                return false;

            _context.Agents.Remove(agent);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Agents.AnyAsync(e => e.IdAgent == id);
        }

        public async Task<bool> ExistsByMatriculeAsync(string matricule)
        {
            return await _context.Agents.AnyAsync(e => e.Matricule == matricule);
        }
        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _context.Agents.AnyAsync(a => a.EmailAgent == email);
        }
        public async Task<bool> ExistsBySerialNumberAsync(string serialNumber)
        {
            return await _context.Agents.AnyAsync(a => a.SerialNumber == serialNumber);
        }
        /// <summary>
        /// ✨ NOUVEAU: Génère un matricule unique pour un agent avec format national
        /// Format: [NAT][Année(2)]-[GUID(6)]
        /// Exemple: "NAT25-A3F2B1" (11 caractères)
        /// Différenciation claire avec les élèves (qui ont le code école)
        /// Unicité garantie avec ~16.7 millions de combinaisons par année
        /// </summary>
        private async Task<string> GenerateMatriculeAgent(Agent agent, string nomSociete)
        {
            string matricule = string.Empty;

            // A. Préfixe national fixe pour tous les agents (différenciation avec élèves)
            matricule = "NAT";

            // B. Deux derniers chiffres de l'année en cours
            matricule += DateTime.Now.Year.ToString().Substring(2);

            // C. Séparateur
            matricule += "-";

            // D. ✨ GUID partiel de 6 caractères hexadécimaux (garantit l'unicité)
            // 16^6 = 16,777,216 combinaisons possibles par année
            string guid = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
            matricule += guid;

            // E. ✅ Vérification OBLIGATOIRE d'unicité en boucle (espace partagé national)
            // Contrairement aux élèves (par école), tous les agents partagent le même espace
            while (await _context.Agents.AnyAsync(a => a.Matricule == matricule))
            {
                guid = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
                matricule = "NAT" + DateTime.Now.Year.ToString().Substring(2) + "-" + guid;
            }

            return matricule;
        }
        // ✅ SOFT DELETE: Toggle le statut d'un agent (actif <-> inactif)
        public async Task<bool> ToggleStatutAsync(int id)
        {
            var agent = await _context.Agents.FindAsync(id);
            if (agent == null)
                return false;

            agent.Statut = agent.Statut != true;
            await SyncAgentUtilisateurAsync(agent, agent.RoleAgent);
            await _context.SaveChangesAsync();
            return true;
        }
        // ✅ RÉCUPÉRATION PAR SERIAL NUMBER: Récupérer un agent par son numéro de série
        public async Task<Agent> GetBySerialNumberAsync(string serialNumber)
        {
            return await _context.Agents
               // .Include(a => a.Societe)
                .Where(a => a.Statut == true) // ✅ Filtrer uniquement les agents actifs
                .FirstOrDefaultAsync(a => a.SerialNumber == serialNumber);
        }
        private async Task SyncAgentUtilisateurAsync(Agent agent, string? previousRoleAgent, CancellationToken cancellationToken = default)
        {
            var utilisateur = await _context.Utilisateurs
                .FirstOrDefaultAsync(u => u.IdAgent == agent.IdAgent, cancellationToken);

            if (utilisateur == null)
            {
                return;
            }

            // Synchroniser le nom complet
            if (!string.IsNullOrWhiteSpace(agent.NomComplet))
            {
                utilisateur.NomComplet = agent.NomComplet;
            }

            // Synchroniser le téléphone avec vérification d'unicité
            if (agent.TelephoneAgent != utilisateur.Telephone)
            {
                if (!string.IsNullOrWhiteSpace(agent.TelephoneAgent))
                {
                    var telephoneDejaUtilise = await _context.Utilisateurs
                        .AnyAsync(u => u.Telephone == agent.TelephoneAgent && u.IdUtilisateur != utilisateur.IdUtilisateur, cancellationToken);
                    
                    if (!telephoneDejaUtilise)
                    {
                        utilisateur.Telephone = agent.TelephoneAgent;
                    }
                    else
                    {
                        _logger.LogWarning(
                            "⚠️ Téléphone '{Telephone}' non synchronisé pour l'utilisateur {UserId} (agent {AgentId}) car déjà utilisé par un autre utilisateur",
                            agent.TelephoneAgent, utilisateur.IdUtilisateur, agent.IdAgent);
                    }
                }
                else
                {
                    utilisateur.Telephone = agent.TelephoneAgent;
                }
            }

            // Synchroniser l'email avec vérification d'unicité
            if (agent.EmailAgent != utilisateur.Email)
            {
                if (!string.IsNullOrWhiteSpace(agent.EmailAgent))
                {
                    var emailDejaUtilise = await _context.Utilisateurs
                        .AnyAsync(u => u.Email == agent.EmailAgent && u.IdUtilisateur != utilisateur.IdUtilisateur, cancellationToken);
                    
                    if (!emailDejaUtilise)
                    {
                        utilisateur.Email = agent.EmailAgent;
                    }
                    else
                    {
                        _logger.LogWarning(
                            "⚠️ Email '{Email}' non synchronisé pour l'utilisateur {UserId} (agent {AgentId}) car déjà utilisé par un autre utilisateur",
                            agent.EmailAgent, utilisateur.IdUtilisateur, agent.IdAgent);
                    }
                }
                else
                {
                    utilisateur.Email = agent.EmailAgent;
                }
            }

            // Synchroniser les autres champs
            utilisateur.PhotoUrl = agent.PhotoUrl;
            utilisateur.Genre = agent.Genre;
            utilisateur.AdresseResidence = agent.AdresseResidence;
            utilisateur.Statut = agent.Statut ?? utilisateur.Statut;
            utilisateur.IdSociete = agent.IdSociete;
            utilisateur.IdSite = agent.IdSite;

            // Gestion des rôles
            var desiredRole = agent.RoleAgent?.Trim();
            if (!string.IsNullOrWhiteSpace(desiredRole))
            {
                var role = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Nom == desiredRole, cancellationToken);

                if (role == null)
                {
                    throw new InvalidOperationException($"Le rôle '{desiredRole}' est introuvable dans la table des rôles.");
                }

                utilisateur.IdRole = role.IdRole;
            }
            else if (!string.IsNullOrWhiteSpace(previousRoleAgent))
            {
                var role = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Nom == previousRoleAgent, cancellationToken);

                if (role != null)
                {
                    utilisateur.IdRole = role.IdRole;
                }
            }
        }
        // ✅ MISE À JOUR DU SERIAL NUMBER: Par IdAgent
        public async Task<bool> UpdateSerialNumberByIdAsync(int idAgent, string serialNumber)
        {
            var agent = await _context.Agents.FindAsync(idAgent);
            if (agent == null)
                return false;

            // ✅ UNICITÉ SERIAL NUMBER: Vérifier que le nouveau SerialNumber n'est pas déjà utilisé par un autre agent
            if (!string.IsNullOrEmpty(serialNumber) && serialNumber != agent.SerialNumber)
            {
                var serialNumberExistsByOtherAgent = await _context.Agents
                    .AnyAsync(a => a.SerialNumber == serialNumber && a.IdAgent != idAgent);
                
                if (serialNumberExistsByOtherAgent)
                {
                    throw new InvalidOperationException(
                        $"Un autre agent avec le SerialNumber '{serialNumber}' existe déjà. " +
                        $"Chaque SerialNumber doit être unique dans le système. " +
                        $"Cet appareil est peut-être déjà lié à un autre agent."
                    );
                }
            }

            agent.SerialNumber = serialNumber;
            await _context.SaveChangesAsync();
            return true;
        }
        // ✅ MISE À JOUR DU SERIAL NUMBER: Par Matricule
        public async Task<bool> UpdateSerialNumberByMatriculeAsync(string matricule, string serialNumber)
        {
            var agent = await _context.Agents
                .FirstOrDefaultAsync(a => a.Matricule == matricule);
            
            if (agent == null)
                return false;

            // ✅ UNICITÉ SERIAL NUMBER: Vérifier que le nouveau SerialNumber n'est pas déjà utilisé par un autre agent
            if (!string.IsNullOrEmpty(serialNumber) && serialNumber != agent.SerialNumber)
            {
                var serialNumberExistsByOtherAgent = await _context.Agents
                    .AnyAsync(a => a.SerialNumber == serialNumber && a.IdAgent != agent.IdAgent);
                
                if (serialNumberExistsByOtherAgent)
                {
                    throw new InvalidOperationException(
                        $"Un autre agent avec le SerialNumber '{serialNumber}' existe déjà. " +
                        $"Chaque SerialNumber doit être unique dans le système. " +
                        $"Cet appareil est peut-être déjà lié à un autre agent."
                    );
                }
            }

            agent.SerialNumber = serialNumber;
            await _context.SaveChangesAsync();
            return true;
        }
        /// <summary>
        /// Crée automatiquement un utilisateur Agent par défaut lors de la création d'un nouvel agent
        /// ✨ RBAC: Attribution automatique du rôle en fonction de la fonction de l'agent
        /// Mapping:
        ///   - "Gerant" → Rôle "Gerant"
        ///   - "Caissier" → Rôle "Caissier"
        ///   - "Financier" / "Comptable" → Rôle "Financier"
        ///   - "Manager Général" → Rôle "Admin"
        ///   - Autres → Rôle "Caissier" (par défaut)
        /// </summary>
        private async Task<UtilisateurInfo?> CreateDefaultAgentUserAsync(Agent agent)
        {
            try
            {
                _logger.LogInformation("🔍 CreateDefaultAgentUserAsync appelé pour agent {AgentId} (Email: {Email}, Fonction: {Fonction}, RoleAgent: {RoleAgent})", 
                    agent.IdAgent, agent.EmailAgent, agent.Fonction, agent.RoleAgent);
                
                // ✨ RBAC : Déterminer le rôle
                // PRIORITÉ 1 : Utiliser RoleAgent si fourni (correspond au champ Nom dans la table Roles)
                // PRIORITÉ 2 : Sinon, déterminer le rôle à partir de la Fonction
                string nomRole;
                if (!string.IsNullOrWhiteSpace(agent.RoleAgent))
                {
                    nomRole = agent.RoleAgent.Trim();
                    _logger.LogInformation("🔍 Rôle déterminé à partir de RoleAgent: {Role}", nomRole);
                }
                else
                {
                    nomRole = DetermineRoleFromFonction(agent.Fonction);
                    _logger.LogInformation("🔍 Rôle déterminé à partir de la fonction '{Fonction}': {Role}", agent.Fonction, nomRole);
                }
                
                // Récupérer le rôle correspondant
                var agentRole = await _context.Roles.FirstOrDefaultAsync(r => r.Nom == nomRole);
                if (agentRole == null)
                {
                    _logger.LogWarning("⚠️ Rôle '{Role}' non trouvé pour la fonction '{Fonction}'. Utilisation du rôle par défaut 'Caissier'.", 
                        nomRole, agent.Fonction);
                    
                    // Fallback vers le rôle Caissier
                    agentRole = await _context.Roles.FirstOrDefaultAsync(r => r.Nom == "Caissier");
                    
                    if (agentRole == null)
                    {
                        _logger.LogError("❌ Aucun rôle trouvé (ni '{Role}' ni 'Caissier'). Les rôles n'ont peut-être pas été initialisés.", nomRole);
                        throw new InvalidOperationException(
                            $"Aucun rôle trouvé pour l'agent. " +
                            $"Assurez-vous que les rôles ont été initialisés via PermissionSeeder."
                        );
                    }
                }
                else
                {
                    _logger.LogInformation("✅ Rôle trouvé: {Role} (ID: {RoleId})", agentRole.Nom, agentRole.IdRole);
                }

                // Récupérer l'école
                var societe = await _context.Societes
                    .FirstOrDefaultAsync(e => e.IdSociete == agent.IdSociete);
                
                if (societe == null)
                {
                    _logger.LogError("❌ École non trouvée pour IdSociete {SocieteId}", agent.IdSociete);
                    return null;
                }
                
                _logger.LogInformation("✅ École trouvée: {SocieteNom} (ID: {SocieteId})", societe.Nom, societe.IdSociete);

                // Utiliser l'email de l'agent s'il est fourni, sinon null (plusieurs NULL autorisés en BDD)
                string? email = string.IsNullOrWhiteSpace(agent.EmailAgent) ? null : agent.EmailAgent.Trim();
                string telephone = agent.TelephoneAgent!.Trim();
                
                // ═══════════════════════════════════════════════════════════════════
                // ✅ MULTI-RÔLES : Vérifier si un utilisateur existe déjà par email/téléphone
                // ═══════════════════════════════════════════════════════════════════
                
                Utilisateur? existingUser = null;
                
                // 1. Vérifier si un utilisateur existe déjà pour cet agent (par IdAgent)
                existingUser = await _context.Utilisateurs
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.IdAgent == agent.IdAgent);
                
                // 2. Si pas trouvé, chercher par email ou téléphone (pour le multi-rôles)
                if (existingUser == null && (!string.IsNullOrWhiteSpace(email) || !string.IsNullOrWhiteSpace(telephone)))
                {
                    existingUser = await _context.Utilisateurs
                        .Include(u => u.UserRoles)
                            .ThenInclude(ur => ur.Role)
                        .FirstOrDefaultAsync(u => 
                            (!string.IsNullOrWhiteSpace(email) && u.Email == email) ||
                            (!string.IsNullOrWhiteSpace(telephone) && u.Telephone == telephone)
                        );
                }
                
                // 3. Si utilisateur existe, ajouter le rôle correspondant (multi-rôles)
                if (existingUser != null)
                {
                    _logger.LogInformation("✅ Utilisateur existant trouvé pour l'agent '{NomComplet}' (ID: {UserId}, Email: {Email})", 
                        agent.NomComplet ?? "Agent", existingUser.IdUtilisateur, existingUser.Email);
                    
                    // Recharger les UserRoles pour s'assurer qu'on a les données à jour
                    await _context.Entry(existingUser)
                        .Collection(u => u.UserRoles)
                        .Query()
                        .Include(ur => ur.Role)
                        .LoadAsync();
                    
                    // Vérifier si l'utilisateur a déjà le rôle correspondant
                    var hasAgentRole = existingUser.UserRoles
                        .Any(ur => ur.Role.Nom == nomRole && ur.Statut == true);
                    
                    _logger.LogInformation("🔍 Vérification rôle '{Role}' pour utilisateur {UserId}: hasAgentRole = {HasRole}", 
                        nomRole, existingUser.IdUtilisateur, hasAgentRole);
                    
                    if (!hasAgentRole)
                    {
                        // Ajouter le rôle correspondant à l'utilisateur existant
                        _logger.LogInformation("➕ Ajout du rôle '{Role}' (ID: {RoleId}) à l'utilisateur existant (ID: {UserId})", 
                            nomRole, agentRole.IdRole, existingUser.IdUtilisateur);
                        
                        var roleAdded = await _utilisateurRepository.AddRoleToUserAsync(
                            existingUser.IdUtilisateur,
                            agentRole.IdRole,
                            assignedByUserId: null,
                            isPrimary: false // Ne pas changer le rôle principal
                        );
                        
                        if (roleAdded)
                        {
                            _logger.LogInformation("✅ Rôle '{Role}' ajouté avec succès à l'utilisateur {UserId}", 
                                nomRole, existingUser.IdUtilisateur);
                            
                            // Recharger les UserRoles après l'ajout pour vérification
                            await _context.Entry(existingUser)
                                .Collection(u => u.UserRoles)
                                .Query()
                                .Include(ur => ur.Role)
                                .LoadAsync();
                            
                            // Vérifier que le rôle a bien été ajouté
                            var verifyAgentRole = existingUser.UserRoles
                                .Any(ur => ur.Role.Nom == nomRole && ur.Statut == true);
                            
                            if (verifyAgentRole)
                            {
                                _logger.LogInformation("✅ Vérification réussie : Le rôle '{Role}' est bien présent dans UserRoles pour l'utilisateur {UserId}", 
                                    nomRole, existingUser.IdUtilisateur);
                            }
                            else
                            {
                                _logger.LogWarning("⚠️ ATTENTION : Le rôle '{Role}' n'a pas été trouvé après l'ajout pour l'utilisateur {UserId}", 
                                    nomRole, existingUser.IdUtilisateur);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ Échec de l'ajout du rôle '{Role}' à l'utilisateur {UserId}", 
                                nomRole, existingUser.IdUtilisateur);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("ℹ️ L'utilisateur {UserId} a déjà le rôle '{Role}'", 
                            existingUser.IdUtilisateur, nomRole);
                    }
                    
                    // Synchroniser société, site et lien agent
                    existingUser.IdSociete = agent.IdSociete;
                    existingUser.IdSite = agent.IdSite;
                    if (existingUser.IdAgent != agent.IdAgent)
                    {
                        existingUser.IdAgent = agent.IdAgent;
                    }
                    await _context.SaveChangesAsync();
                    
                    // Retourner les infos de l'utilisateur existant (pas de mot de passe révélé)
                    var primaryRole = existingUser.UserRoles
                        .Where(ur => ur.Statut == true && ur.IsPrimary)
                        .Select(ur => ur.Role.Nom)
                        .FirstOrDefault()
                        ?? existingUser.UserRoles
                            .Where(ur => ur.Statut == true)
                            .OrderBy(ur => ur.Role.Niveau ?? 999)
                            .Select(ur => ur.Role.Nom)
                            .FirstOrDefault()
                        ?? "Caissier";
                    
                    return new UtilisateurInfo
                    {
                        IdUtilisateur = existingUser.IdUtilisateur,
                        IdAgent = existingUser.IdAgent ?? agent.IdAgent,
                        Email = existingUser.Email ?? email,
                        DefaultUsername = existingUser.DefaultUsername ?? "",
                        Telephone = existingUser.Telephone ?? telephone,
                        MotDePasseParDefaut = "", // ⚠️ Ne jamais révéler le mot de passe d'un compte existant
                        NomComplet = existingUser.NomComplet ?? "Utilisateur",
                        Role = primaryRole
                    };
                }
                
                // Construire le nom complet en premier (avec valeur par défaut si NULL)
                string nomComplet = agent.NomComplet ?? "Agent";
                
                // ✅ FIX: S'assurer que NomComplet n'est jamais NULL (champ [Required])
                if (string.IsNullOrWhiteSpace(nomComplet))
                {
                    nomComplet = "Agent"; // Valeur par défaut si NULL
                    _logger.LogWarning("⚠️ Le nom complet de l'agent est NULL, utilisation de la valeur par défaut 'Agent'");
                }
                
                // ✨ NOUVEAU : Générer le DefaultUsername basé sur le nom complet + nombre aléatoire
                // Format: NomComplet (sans espaces) + nombre aléatoire (1-999)
                // Exemple: "Julie Kalambayi Nsakadi" → "JulieKalambayiNsakadi456"
                string baseUsername = nomComplet.Replace(" ", "").Replace("-", "").Replace("'", "");
                if (string.IsNullOrWhiteSpace(baseUsername))
                {
                    baseUsername = "Agent"; // Valeur par défaut si le nom complet est vide
                }
                if (baseUsername.Length > 20)
                {
                    baseUsername = baseUsername.Substring(0, 20);
                }
                Random random = new Random();
                int randomNumber = random.Next(1, 1000);
                string defaultUsername = $"{baseUsername}{randomNumber}";
                
                // Le mot de passe par défaut est simple : 123456
                // L'utilisateur DOIT le changer à la première connexion
                string motDePasseParDefaut = "123456";
                
                // ✨ Récupérer la fonction et le matricule pour l'email
                string fonction = agent.Fonction ?? "Agent";
                string matricule = agent.Matricule ?? ""; // Le matricule a déjà été généré dans CreateAsync
                
                // ═══════════════════════════════════════════════════════════════════
                // ✅ MULTI-RÔLES : Créer un nouvel utilisateur avec UserRole
                // ═══════════════════════════════════════════════════════════════════
                
                // Créer l'utilisateur Agent par défaut (sans IdRole)
                var agentUser = new Utilisateur
                {
                    IdAgent = agent.IdAgent,
                    ReferenceUtilisateur = Guid.NewGuid(),
                    NomComplet = nomComplet,
                    Email = email,
                    DefaultUsername = defaultUsername,
                    Telephone = telephone,
                    PhotoUrl = agent.PhotoUrl,
                    DateNaissance = agent.DateNaissance,
                    Genre = agent.Genre,
                    MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(motDePasseParDefaut),
                    Statut = true,
                    DateCreation = DateTime.Now,
                    IsConnecte = false,
                    DoitChangerMotDePasse = true, // ✨ FORCER le changement de mot de passe à la première connexion
                    // ❌ Ne plus utiliser IdRole (ancien système mono-rôle)
                    // IdRole = agentRole.IdRole,
                    IdSociete = agent.IdSociete,
                    IdSite = agent.IdSite
                    // Note: L'adresse de l'agent n'est plus copiée car Agent n'hérite plus de Adresse
                    // L'utilisateur peut avoir sa propre adresse via les champs hérités de Adresse
                };

                _logger.LogInformation("🔍 Création de l'utilisateur avec les valeurs: NomComplet={NomComplet}, Email={Email}, IdSociete={SocieteId}, IdSite={SiteId}, IdAgent={AgentId}", 
                    agentUser.NomComplet, agentUser.Email, agentUser.IdSociete, agentUser.IdSite, agentUser.IdAgent);

                // ✅ Validation avant ajout
                try
                {
                _context.Utilisateurs.Add(agentUser);
                    _logger.LogInformation("✅ Utilisateur ajouté au contexte. Validation en cours...");
                    
                    // Valider que les champs requis ne sont pas NULL
                    if (string.IsNullOrWhiteSpace(agentUser.NomComplet))
                    {
                        throw new InvalidOperationException("NomComplet est requis mais est NULL ou vide");
                    }
                    if (string.IsNullOrWhiteSpace(agentUser.MotDePasseHash))
                    {
                        throw new InvalidOperationException("MotDePasseHash est requis mais est NULL ou vide");
                    }
                    
                    _logger.LogInformation("✅ Validation réussie. Sauvegarde en cours...");
                await _context.SaveChangesAsync();
                    _logger.LogInformation("✅ Utilisateur sauvegardé avec succès. IdUtilisateur={UserId}", agentUser.IdUtilisateur);
                }
                catch (Exception saveEx)
                {
                    _logger.LogError(saveEx, "❌ ERREUR lors de la sauvegarde de l'utilisateur: {ErrorMessage}", saveEx.Message);
                    if (saveEx.InnerException != null)
                    {
                        _logger.LogError(saveEx.InnerException, "❌ Exception interne: {InnerMessage}", saveEx.InnerException.Message);
                    }
                    throw; // Re-lancer l'exception pour qu'elle soit capturée par le catch principal
                }
                
                // ✅ Créer le UserRole pour le système multi-rôles
                _logger.LogInformation("🔍 Création du UserRole pour IdUtilisateur={UserId}, IdRole={RoleId}", 
                    agentUser.IdUtilisateur, agentRole.IdRole);
                
                UserRole userRole;
                try
                {
                    userRole = new UserRole
                    {
                        IdUtilisateur = agentUser.IdUtilisateur,
                        IdRole = agentRole.IdRole,
                        IsPrimary = true, // Premier rôle = principal
                        Statut = true,
                        DateAttribution = DateTime.Now,
                        IdUtilisateurAttribution = null
                    };
                    
                    _context.UserRoles.Add(userRole);
                    _logger.LogInformation("✅ UserRole ajouté au contexte. Sauvegarde en cours...");
                    
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("✅ UserRole sauvegardé avec succès. IdUserRole={UserRoleId}", userRole.IdUserRole);
                }
                catch (Exception roleEx)
                {
                    _logger.LogError(roleEx, "❌ ERREUR lors de la sauvegarde du UserRole: {ErrorMessage}", roleEx.Message);
                    if (roleEx.InnerException != null)
                    {
                        _logger.LogError(roleEx.InnerException, "❌ Exception interne: {InnerMessage}", roleEx.InnerException.Message);
                    }
                    throw; // Re-lancer l'exception pour qu'elle soit capturée par le catch principal
                }
                
                _logger.LogInformation("✅ UserRole créé avec succès : IdUtilisateur={UserId}, IdRole={RoleId} (Role: {RoleName}), IsPrimary={IsPrimary}", 
                    userRole.IdUtilisateur, userRole.IdRole, agentRole.Nom, userRole.IsPrimary);
                
                // Vérifier que le UserRole a bien été créé
                var verifyUserRole = await _context.UserRoles
                    .Include(ur => ur.Role)
                    .FirstOrDefaultAsync(ur => ur.IdUtilisateur == agentUser.IdUtilisateur && ur.IdRole == agentRole.IdRole);
                
                if (verifyUserRole != null)
                {
                    _logger.LogInformation("✅ Vérification réussie : UserRole trouvé dans la base de données (ID: {UserRoleId})", 
                        verifyUserRole.IdUserRole);
                }
                else
                {
                    _logger.LogError("❌ ERREUR : UserRole non trouvé dans la base de données après création pour utilisateur {UserId}", 
                        agentUser.IdUtilisateur);
                }
                
                _logger.LogInformation("✅ Utilisateur Agent créé avec UserRole (ID: {UserId}, Role: {RoleName})", 
                    agentUser.IdUtilisateur, agentRole.Nom);
                
                _logger.LogInformation("✅ Utilisateur Agent créé pour '{NomComplet}' - Email: {Email}, Username: {Username}", 
                    nomComplet, agentUser.Email, defaultUsername);
                
                // Envoyer l'email de bienvenue (si email fourni)
                if (!string.IsNullOrWhiteSpace(email))
                {
                    string nomSociete = societe.Nom ?? "Kenergie";
                    
                    // Envoi asynchrone (ne bloque pas si échec)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _emailService.SendWelcomeEmailAsync(
                                email,
                                nomComplet,
                                defaultUsername,
                                telephone,
                                motDePasseParDefaut,
                                agentRole.Nom,  // ✨ Utiliser le vrai nom du rôle
                                nomSociete,
                                agent.Genre,  // ✨ Passer le genre
                                fonction,     // ✨ Passer la fonction
                                matricule     // ✨ Passer le matricule
                            );
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogWarning(emailEx, "⚠️ Échec de l'envoi de l'email à {Email}: {ErrorMessage}", 
                                email, emailEx.Message);
                        }
                    });
                    
                    _logger.LogInformation("📧 Email de bienvenue programmé pour {Email}", email);
                }
                else
                {
                    _logger.LogWarning("⚠️ Aucun email fourni pour l'agent '{NomComplet}'. Notification SMS sera envoyée ultérieurement.", 
                        nomComplet);
                }
                
                // Retourner les informations du compte créé
                return new UtilisateurInfo
                {
                    IdUtilisateur = agentUser.IdUtilisateur,
                    IdAgent = agentUser.IdAgent,
                    Email = agentUser.Email ?? "",
                    DefaultUsername = agentUser.DefaultUsername ?? "",
                    Telephone = agentUser.Telephone ?? "",
                    MotDePasseParDefaut = motDePasseParDefaut,
                    NomComplet = nomComplet,
                    Role = agentRole.Nom  // ✨ Utiliser le vrai nom du rôle
                };
            }
            catch (Exception ex)
            {
                // Log l'erreur complète avec toutes les exceptions internes
                _logger.LogError(ex, "❌ ERREUR lors de la création de l'utilisateur Agent par défaut pour agent {AgentId}: {ErrorMessage}", 
                    agent.IdAgent, ex.Message);
                
                // Log l'exception interne si elle existe
                if (ex.InnerException != null)
                {
                    _logger.LogError(ex.InnerException, "❌ Exception interne: {InnerMessage}", ex.InnerException.Message);
                    
                    // Si c'est une DbUpdateException, log les détails supplémentaires
                    if (ex.InnerException is Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
                    {
                        _logger.LogError("❌ DbUpdateException détectée. Entrées: {Entries}", 
                            string.Join(", ", dbEx.Entries.Select(e => $"{e.Entity.GetType().Name} - {string.Join(", ", e.Properties.Select(p => $"{p.Metadata.Name}={p.CurrentValue}"))}")));
                    }
                }
                
                // Log le stack trace complet
                _logger.LogError("❌ StackTrace: {StackTrace}", ex.StackTrace);
                
                return null;
            }
        }

        // ✅ MULTI-RÔLES : Ajouter un rôle à un agent
        public async Task<bool> AddRoleToAgentAsync(int idAgent, string roleAgent, bool isPrimary = false, int? assignedByUserId = null)
        {
            try
            {
                _logger.LogInformation("🔍 AddRoleToAgentAsync appelé pour agent {AgentId}, RoleAgent: {RoleAgent}, IsPrimary: {IsPrimary}", 
                    idAgent, roleAgent, isPrimary);
                
                // 1. Trouver l'agent
                var agent = await _context.Agents.FindAsync(idAgent);
                if (agent == null)
                {
                    _logger.LogWarning("⚠️ Agent {AgentId} non trouvé", idAgent);
                    return false;
                }
                
                _logger.LogInformation("✅ Agent trouvé: {NomComplet} (ID: {AgentId})", agent.NomComplet ?? "Agent", agent.IdAgent);
                
                // 2. Trouver l'utilisateur associé à cet agent
                var utilisateur = await _context.Utilisateurs
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.IdAgent == idAgent);
                
                if (utilisateur == null)
                {
                    _logger.LogWarning("⚠️ Aucun utilisateur trouvé pour l'agent {AgentId}. Création d'un utilisateur...", idAgent);
                    
                    // Créer l'utilisateur si il n'existe pas
                    var userInfo = await CreateDefaultAgentUserAsync(agent);
                    if (userInfo == null)
                    {
                        _logger.LogError("❌ Échec de la création de l'utilisateur pour l'agent {AgentId}", idAgent);
                        return false;
                    }
                    
                    // Recharger l'utilisateur créé
                    utilisateur = await _context.Utilisateurs
                        .Include(u => u.UserRoles)
                            .ThenInclude(ur => ur.Role)
                        .FirstOrDefaultAsync(u => u.IdAgent == idAgent);
                    
                    if (utilisateur == null)
                    {
                        _logger.LogError("❌ Impossible de récupérer l'utilisateur créé pour l'agent {AgentId}", idAgent);
                        return false;
                    }
                }
                
                _logger.LogInformation("✅ Utilisateur trouvé: {UserId} (Email: {Email})", utilisateur.IdUtilisateur, utilisateur.Email);
                
                // 3. Trouver le rôle correspondant au RoleAgent (via Nom dans Roles)
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.Nom == roleAgent);
                if (role == null)
                {
                    _logger.LogError("❌ Rôle '{RoleAgent}' non trouvé dans la table Roles", roleAgent);
                    return false;
                }
                
                _logger.LogInformation("✅ Rôle trouvé: {RoleNom} (ID: {RoleId})", role.Nom, role.IdRole);
                
                // 4. Vérifier si l'utilisateur a déjà ce rôle
                var hasRole = utilisateur.UserRoles
                    .Any(ur => ur.Role.Nom == roleAgent && ur.Statut == true);
                
                if (hasRole)
                {
                    _logger.LogInformation("ℹ️ L'utilisateur {UserId} a déjà le rôle '{RoleAgent}'", utilisateur.IdUtilisateur, roleAgent);
                    
                    // Si le rôle existe déjà et qu'on veut le rendre principal, mettre à jour IsPrimary
                    if (isPrimary)
                    {
                        var existingUserRole = utilisateur.UserRoles
                            .FirstOrDefault(ur => ur.Role.Nom == roleAgent && ur.Statut == true);
                        
                        if (existingUserRole != null && !existingUserRole.IsPrimary)
                        {
                            // Retirer le flag IsPrimary des autres rôles
                            var otherPrimaryRoles = utilisateur.UserRoles
                                .Where(ur => ur.Statut == true && ur.IsPrimary && ur.IdUserRole != existingUserRole.IdUserRole)
                                .ToList();
                            
                            foreach (var otherRole in otherPrimaryRoles)
                            {
                                otherRole.IsPrimary = false;
                            }
                            
                            existingUserRole.IsPrimary = true;
                            await _context.SaveChangesAsync();
                            
                            _logger.LogInformation("✅ Le rôle '{RoleAgent}' a été défini comme rôle principal pour l'utilisateur {UserId}", 
                                roleAgent, utilisateur.IdUtilisateur);
                            return true;
                        }
                    }
                    
                    return false; // Le rôle existe déjà
                }
                
                // 5. Si isPrimary est true, retirer le flag IsPrimary des autres rôles
                if (isPrimary)
                {
                    var otherPrimaryRoles = utilisateur.UserRoles
                        .Where(ur => ur.Statut == true && ur.IsPrimary)
                        .ToList();
                    
                    foreach (var otherRole in otherPrimaryRoles)
                    {
                        otherRole.IsPrimary = false;
                    }
                }
                
                // 6. Ajouter le UserRole
                var userRole = new UserRole
                {
                    IdUtilisateur = utilisateur.IdUtilisateur,
                    IdRole = role.IdRole,
                    IsPrimary = isPrimary,
                    Statut = true,
                    DateAttribution = DateTime.Now,
                    IdUtilisateurAttribution = assignedByUserId
                };
                
                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("✅ Rôle '{RoleAgent}' ajouté avec succès à l'agent {AgentId} (utilisateur {UserId}, IsPrimary: {IsPrimary})", 
                    roleAgent, idAgent, utilisateur.IdUtilisateur, isPrimary);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ ERREUR lors de l'ajout du rôle '{RoleAgent}' à l'agent {AgentId}: {ErrorMessage}", 
                    roleAgent, idAgent, ex.Message);
                
                if (ex.InnerException != null)
                {
                    _logger.LogError(ex.InnerException, "❌ Exception interne: {InnerMessage}", ex.InnerException.Message);
                }
                
                return false;
            }
        }

        // ✅ MULTI-RÔLES : Ajouter plusieurs rôles à un agent
        public async Task<AddRolesResult> AddRolesToAgentAsync(int idAgent, IEnumerable<(string RoleAgent, bool IsPrimary)> roles, int? assignedByUserId = null)
        {
            var result = new AddRolesResult
            {
                TotalRoles = roles.Count()
            };

            _logger.LogInformation("🔍 AddRolesToAgentAsync appelé pour agent {AgentId} avec {RoleCount} rôle(s)", 
                idAgent, result.TotalRoles);

            // Vérifier que l'agent existe
            var agent = await _context.Agents.FindAsync(idAgent);
            if (agent == null)
            {
                _logger.LogWarning("⚠️ Agent {AgentId} non trouvé", idAgent);
                result.Message = $"Agent avec l'ID {idAgent} non trouvé";
                return result;
            }

            _logger.LogInformation("✅ Agent trouvé: {NomComplet} (ID: {AgentId})", agent.NomComplet ?? "Agent", agent.IdAgent);

            // Traiter chaque rôle
            foreach (var (roleAgent, isPrimary) in roles)
            {
                try
                {
                    _logger.LogInformation("🔄 Traitement du rôle '{RoleAgent}' (IsPrimary: {IsPrimary})", roleAgent, isPrimary);

                    var success = await AddRoleToAgentAsync(idAgent, roleAgent, isPrimary, assignedByUserId);

                    if (success)
                    {
                        result.SuccessCount++;
                        result.SuccessRoles.Add(new RoleOperationResult
                        {
                            RoleAgent = roleAgent,
                            IsPrimary = isPrimary,
                            Message = $"Rôle '{roleAgent}' ajouté avec succès"
                        });
                        _logger.LogInformation("✅ Rôle '{RoleAgent}' ajouté avec succès", roleAgent);
                    }
                    else
                    {
                        result.FailureCount++;
                        result.FailedRoles.Add(new RoleOperationResult
                        {
                            RoleAgent = roleAgent,
                            IsPrimary = isPrimary,
                            Message = $"Impossible d'ajouter le rôle '{roleAgent}'. Le rôle existe peut-être déjà ou n'a pas été trouvé."
                        });
                        _logger.LogWarning("⚠️ Échec de l'ajout du rôle '{RoleAgent}'", roleAgent);
                    }
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.FailedRoles.Add(new RoleOperationResult
                    {
                        RoleAgent = roleAgent,
                        IsPrimary = isPrimary,
                        Message = $"Erreur lors de l'ajout du rôle '{roleAgent}': {ex.Message}"
                    });
                    _logger.LogError(ex, "❌ ERREUR lors de l'ajout du rôle '{RoleAgent}' à l'agent {AgentId}", roleAgent, idAgent);
                }
            }

            // Déterminer le message de résumé
            if (result.SuccessCount == result.TotalRoles)
            {
                result.Success = true;
                result.Message = $"Tous les {result.SuccessCount} rôle(s) ont été ajoutés avec succès";
            }
            else if (result.SuccessCount > 0)
            {
                result.Success = true;
                result.Message = $"{result.SuccessCount} rôle(s) ajouté(s) avec succès, {result.FailureCount} échec(s)";
            }
            else
            {
                result.Success = false;
                result.Message = $"Aucun rôle n'a pu être ajouté. {result.FailureCount} échec(s)";
            }

            _logger.LogInformation("📊 Résultat final: {Message}", result.Message);
            return result;
        }

        // ✅ MULTI-RÔLES : Remplacer un RoleAgent par un autre
        public async Task<bool> ReplaceRoleAgentAsync(int idAgent, string ancienRoleAgent, string nouveauRoleAgent, int? assignedByUserId = null)
        {
            try
            {
                _logger.LogInformation("🔍 ReplaceRoleAgentAsync appelé pour agent {AgentId}, Ancien: {AncienRole}, Nouveau: {NouveauRole}", 
                    idAgent, ancienRoleAgent, nouveauRoleAgent);
                
                // 1. Trouver l'agent
                var agent = await _context.Agents.FindAsync(idAgent);
                if (agent == null)
                {
                    _logger.LogWarning("⚠️ Agent {AgentId} non trouvé", idAgent);
                    return false;
                }
                
                _logger.LogInformation("✅ Agent trouvé: {NomComplet} (ID: {AgentId})", agent.NomComplet ?? "Agent", agent.IdAgent);
                
                // 2. Trouver l'utilisateur associé à cet agent
                var utilisateur = await _context.Utilisateurs
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.IdAgent == idAgent);
                
                if (utilisateur == null)
                {
                    _logger.LogWarning("⚠️ Aucun utilisateur trouvé pour l'agent {AgentId}. Création d'un utilisateur...", idAgent);
                    
                    // Créer l'utilisateur si il n'existe pas
                    var userInfo = await CreateDefaultAgentUserAsync(agent);
                    if (userInfo == null)
                    {
                        _logger.LogError("❌ Échec de la création de l'utilisateur pour l'agent {AgentId}", idAgent);
                        return false;
                    }
                    
                    // Recharger l'utilisateur créé
                    utilisateur = await _context.Utilisateurs
                        .Include(u => u.UserRoles)
                            .ThenInclude(ur => ur.Role)
                        .FirstOrDefaultAsync(u => u.IdAgent == idAgent);
                    
                    if (utilisateur == null)
                    {
                        _logger.LogError("❌ Impossible de récupérer l'utilisateur créé pour l'agent {AgentId}", idAgent);
                        return false;
                    }
                }
                
                _logger.LogInformation("✅ Utilisateur trouvé: {UserId} (Email: {Email})", utilisateur.IdUtilisateur, utilisateur.Email);
                
                // 3. Vérifier que l'ancien rôle existe dans la table Roles
                var ancienRole = await _context.Roles.FirstOrDefaultAsync(r => r.Nom == ancienRoleAgent);
                if (ancienRole == null)
                {
                    _logger.LogError("❌ Ancien rôle '{AncienRole}' non trouvé dans la table Roles", ancienRoleAgent);
                    return false;
                }
                
                // 4. Vérifier que le nouveau rôle existe dans la table Roles
                var nouveauRole = await _context.Roles.FirstOrDefaultAsync(r => r.Nom == nouveauRoleAgent);
                if (nouveauRole == null)
                {
                    _logger.LogError("❌ Nouveau rôle '{NouveauRole}' non trouvé dans la table Roles", nouveauRoleAgent);
                    return false;
                }
                
                _logger.LogInformation("✅ Rôles trouvés: Ancien={AncienRole} (ID: {AncienRoleId}), Nouveau={NouveauRole} (ID: {NouveauRoleId})", 
                    ancienRole.Nom, ancienRole.IdRole, nouveauRole.Nom, nouveauRole.IdRole);
                
                // 5. Vérifier que l'utilisateur a l'ancien rôle
                var ancienUserRole = utilisateur.UserRoles
                    .FirstOrDefault(ur => ur.Role.Nom == ancienRoleAgent && ur.Statut == true);
                
                if (ancienUserRole == null)
                {
                    _logger.LogWarning("⚠️ L'utilisateur {UserId} n'a pas le rôle '{AncienRole}' actif", utilisateur.IdUtilisateur, ancienRoleAgent);
                    return false;
                }
                
                _logger.LogInformation("✅ Ancien UserRole trouvé: IdUserRole={UserRoleId}, IsPrimary={IsPrimary}", 
                    ancienUserRole.IdUserRole, ancienUserRole.IsPrimary);
                
                // 6. Vérifier si l'utilisateur a déjà le nouveau rôle
                var nouveauUserRoleExistant = utilisateur.UserRoles
                    .FirstOrDefault(ur => ur.Role.Nom == nouveauRoleAgent && ur.Statut == true);
                
                if (nouveauUserRoleExistant != null)
                {
                    _logger.LogWarning("⚠️ L'utilisateur {UserId} a déjà le rôle '{NouveauRole}'. Suppression de l'ancien rôle uniquement.", 
                        utilisateur.IdUtilisateur, nouveauRoleAgent);
                    
                    // Supprimer l'ancien rôle uniquement (soft delete)
                    ancienUserRole.Statut = false;
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("✅ Ancien rôle '{AncienRole}' supprimé (l'utilisateur avait déjà le nouveau rôle)", ancienRoleAgent);
                    
                    // Mettre à jour le champ RoleAgent dans Agents si c'était le rôle principal
                    if (ancienUserRole.IsPrimary && agent.RoleAgent == ancienRoleAgent)
                    {
                        agent.RoleAgent = nouveauRoleAgent;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("✅ Champ RoleAgent mis à jour dans Agents: {AncienRole} → {NouveauRole}", 
                            ancienRoleAgent, nouveauRoleAgent);
                    }
                    
                    return true;
                }
                
                // 7. Conserver le statut IsPrimary de l'ancien rôle pour le nouveau (comme demandé)
                bool nouveauIsPrimary = ancienUserRole.IsPrimary;
                
                // 8. Supprimer l'ancien rôle (soft delete)
                ancienUserRole.Statut = false;
                ancienUserRole.IsPrimary = false;
                
                // 9. Créer le nouveau UserRole avec le même statut IsPrimary
                var nouveauUserRole = new UserRole
                {
                    IdUtilisateur = utilisateur.IdUtilisateur,
                    IdRole = nouveauRole.IdRole,
                    IsPrimary = nouveauIsPrimary, // ✅ Conserver le statut IsPrimary de l'ancien rôle
                    Statut = true,
                    DateAttribution = DateTime.Now,
                    IdUtilisateurAttribution = assignedByUserId
                };
                
                _context.UserRoles.Add(nouveauUserRole);
                
                // 10. Mettre à jour le champ RoleAgent dans Agents si l'ancien était le rôle principal
                if (ancienUserRole.IsPrimary && agent.RoleAgent == ancienRoleAgent)
                {
                    agent.RoleAgent = nouveauRoleAgent;
                    _logger.LogInformation("✅ Mise à jour du champ RoleAgent dans Agents: {AncienRole} → {NouveauRole}", 
                        ancienRoleAgent, nouveauRoleAgent);
                }
                
                // 11. Sauvegarder les modifications
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("✅ Rôle remplacé avec succès : {AncienRole} → {NouveauRole} pour l'agent {AgentId} (utilisateur {UserId}, IsPrimary conservé: {IsPrimary})", 
                    ancienRoleAgent, nouveauRoleAgent, idAgent, utilisateur.IdUtilisateur, nouveauIsPrimary);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ ERREUR lors du remplacement du rôle '{AncienRole}' par '{NouveauRole}' pour l'agent {AgentId}: {ErrorMessage}", 
                    ancienRoleAgent, nouveauRoleAgent, idAgent, ex.Message);
                
                if (ex.InnerException != null)
                {
                    _logger.LogError(ex.InnerException, "❌ Exception interne: {InnerMessage}", ex.InnerException.Message);
                }
                
                return false;
            }
        }

        /// <summary>
        /// ✨ RBAC: Détermine le rôle approprié en fonction de la fonction de l'agent
        /// </summary>
        private static void NormalizeAndValidateAgentContactForCreate(Agent agent)
        {
            if (string.IsNullOrWhiteSpace(agent.TelephoneAgent))
                throw new ArgumentException("Le téléphone de l'agent est obligatoire.");

            agent.TelephoneAgent = agent.TelephoneAgent.Trim();
            agent.EmailAgent = string.IsNullOrWhiteSpace(agent.EmailAgent) ? null : agent.EmailAgent.Trim();
        }

        private string DetermineRoleFromFonction(string? fonction)
        {
            if (string.IsNullOrWhiteSpace(fonction))
            {
                return "Caissier"; // Rôle par défaut
            }

            // Normaliser la fonction (insensible à la casse, sans espaces)
            string fonctionNormalisee = fonction.Trim().ToLower();

            // ✅ CORRECTION 3 : Mapping Fonction → Rôle enrichi (ajout de 10+ fonctions manquantes)
            return fonctionNormalisee switch
            {
                // 👔 Direction & Administration
                "directeur" => "Gerant",
                "directrice" => "Gerant",
                "gerant" => "Gerant",
                "gérant" => "Gerant",
                "gerante" => "Gerant",
                "gérante" => "Gerant",
                "manager général" => "Admin",
                "manager generale" => "Admin",
                "administrateur" => "Admin",
                "administratrice" => "Admin",
                "préfet" => "Gerant",
                "prefet" => "Gerant",
                "préfète" => "Gerant",
                "prefete" => "Gerant",
                
                // 💰 Caisse & Transactions
                "caissier" => "Caissier",
                "caissiere" => "Caissier",
                "caissière" => "Caissier",
                "cashier" => "Caissier",
                
                // 💰 Finance & Comptabilité
                "financier" => "Financier",
                "financière" => "Financier",
                "comptable" => "Financier",
                "trésorier" => "Financier",
                "trésorière" => "Financier",
                
                // 🏢 Personnel de soutien (⚠️ Rôle "Personnel" doit exister en BDD)
                "gardien" => "Personnel",
                "gardienne" => "Personnel",
                "concierge" => "Personnel",
                "secrétaire" => "Personnel",
                "secretaire" => "Personnel",
                "personnel" => "Personnel",
                "agent d'entretien" => "Personnel",
                "agent dentretien" => "Personnel",
                "technicien" => "Personnel",
                "technicienne" => "Personnel",
                "cuisinier" => "Personnel",
                "cuisinière" => "Personnel",
                
                _ => "Caissier" // Rôle par défaut pour toutes les autres fonctions
            };
        }
        
        public async Task<PagedResult<Agent>> GetPagedAsync(int idSociete, PagedRequest request)
        {
            request ??= new PagedRequest();

            var effectiveSocieteId = _currentUser.IsSuperAdmin ? idSociete : _currentUser.SocieteId;

            var query = ApplyVisibilityFilters(_context.Agents.AsNoTracking())
                .Where(a => a.Statut == true);

            if (effectiveSocieteId > 0)
                query = query.Where(a => a.IdSociete == effectiveSocieteId);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                query = query.Where(a =>
                    a.NomComplet.ToLower().Contains(term));
            }

            query = request.SortBy switch
            {
               "DateCreation" => request.SortDescending ? query.OrderByDescending(a => a.DateCreation) : query.OrderBy(c => c.DateCreation),
                _ => request.SortDescending ? query.OrderByDescending(a => a.IdAgent) : query.OrderBy(a => a.IdAgent)
            };

            var total = await query.CountAsync();

            var data = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResult<Agent>(data, total, request.PageNumber, request.PageSize);
        }
    }
}

