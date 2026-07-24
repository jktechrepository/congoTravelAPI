using System;
using System.Collections.Generic;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using CongoTravel.Models.DTOs;
using CongoTravel.Models.DTOs.Pagination;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace CongoTravel.Services
{
    public class SocieteService : ISocieteRepository
    {
        private readonly CongoTravelDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<SocieteService> _logger;

        public SocieteService(
            CongoTravelDbContext context,
            IEmailService emailService,
            ICurrentUserService currentUser,
            ILogger<SocieteService> logger)
        {
            _context = context;
            _emailService = emailService;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<IEnumerable<Societe>> GetAllAsync()
        {
            return await _context.Societes
               // .Include(e => e.Classes)
              //  .Include(e => e.Utilisateurs)
                //.Include(e => e.Tuteurs)
               // .Include(e => e.Agents)
               // .Include(e => e.Sections)
               // .Include(e => e.AnneeScolaires)
               // .Include(e => e.Inscriptions)
               // .Include(e => e.GroupesMessages)
                .Where(e => e.Statut == true) // ? Filtrer uniquement les �coles actives
                .ToListAsync();
        }

        public async Task<Societe> GetByIdAsync(int id)
        {
            return await _context.Societes
               // .Include(e => e.Classes)
               // .Include(e => e.Utilisateurs)
               // .Include(e => e.Tuteurs)
               // .Include(e => e.Agents)
               // .Include(e => e.Sections)
               // .Include(e => e.AnneeScolaires)
               // .Include(e => e.Inscriptions)
               // .Include(e => e.GroupesMessages)
                .Where(e => e.Statut == true) // ? Filtrer uniquement les �coles actives
                .FirstOrDefaultAsync(e => e.IdSociete == id);
        }

        public async Task<Societe> GetByNomAsync(string nom)
        {
            return await _context.Societes
                //.Include(e => e.Classes)
               // .Include(e => e.Utilisateurs)
               // .Include(e => e.Tuteurs)
               // .Include(e => e.Agents)
               // .Include(e => e.Sections)
               // .Include(e => e.AnneeScolaires)
               // .Include(e => e.Inscriptions)
               // .Include(e => e.GroupesMessages)
                .Where(e => e.Statut == true) // ? Filtrer uniquement les �coles actives
                .FirstOrDefaultAsync(e => e.Nom == nom);
        }

        //public async Task<Societe> GetByCodeAsync(string code)
        //{
        //    return await _context.Societes
        //        .Include(e => e.Classes)
        //        .Include(e => e.Utilisateurs)
        //        .Include(e => e.Tuteurs)
        //        .Include(e => e.Caissiers)
        //        .Include(e => e.Sections)
        //        .Include(e => e.AnneeScolaires)
        //        .Include(e => e.Inscriptions)
        //        .Include(e => e.GroupesMessages)
        //        .FirstOrDefaultAsync(e => e.Code == code);
        //}

        //public async Task<IEnumerable<Societe>> GetByStatutAsync(bool statut)
        //{
        //    return await _context.Societes
        //        .Include(e => e.Classes)
        //        .Include(e => e.Utilisateurs)
        //        .Include(e => e.Tuteurs)
        //        .Include(e => e.Caissiers)
        //        .Include(e => e.Sections)
        //        .Include(e => e.AnneeScolaires)
        //        .Include(e => e.Inscriptions)
        //        .Include(e => e.GroupesMessages)
        //        .Where(e => e.Statut == statut)
        //        .ToListAsync();
        //}

        public async Task<Societe> CreateAsync(Societe societe)
        {
            societe.EmailContact = NormalizeOptionalEmail(societe.EmailContact);
            societe.DateCreation = DateTime.Now;
            
            _context.Societes.Add(societe);
            await _context.SaveChangesAsync();

            await SeedDefaultCategorieSiegesForSocieteAsync(societe.IdSociete);
            await SeedDefaultTypeVehiculeForSocieteAsync(societe.IdSociete);

            // Créer un Agent « Manager Général » et un Utilisateur lié avec le rôle Admin
            await CreateDefaultGerantAgentAsync(societe, suppressErrors: true);
            
            return societe;
        }

        /// <inheritdoc />
        public async Task<SocieteBootstrapCreationResult> CreateWithBootstrapAsync(CreateSocieteWithBootstrapDto dto)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var societe = MapBootstrapToSociete(dto.Societe);
                    societe.DateCreation = DateTime.Now;

                    _context.Societes.Add(societe);
                    await _context.SaveChangesAsync();

                    _context.ConfigSocietes.Add(ConfigSocieteDefaults.CreateForSociete(societe.IdSociete));
                    await _context.SaveChangesAsync();

                    await SeedDefaultCategorieSiegesForSocieteAsync(societe.IdSociete);
                    await SeedDefaultTypeVehiculeForSocieteAsync(societe.IdSociete);

                    await CreateDefaultGerantAgentAsync(societe, suppressErrors: false);

                    var codeSite = dto.Site.CodeSite.Trim();
                    if (await _context.Sites.AnyAsync(s => s.IdSociete == societe.IdSociete && s.CodeSite == codeSite))
                    {
                        throw new SocieteBootstrapConflictException(
                            SocieteBootstrapConflictReason.SiteCodeAlreadyExists,
                            $"Le code de site '{codeSite}' existe déjà pour cette société.");
                    }

                    var site = new Site
                    {
                        IdSociete = societe.IdSociete,
                        CodeSite = codeSite,
                        NomSite = dto.Site.NomSite.Trim(),
                        Ville = string.IsNullOrWhiteSpace(dto.Site.Ville) ? null : dto.Site.Ville.Trim(),
                        Adresse = string.IsNullOrWhiteSpace(dto.Site.Adresse) ? null : dto.Site.Adresse.Trim(),
                        Telephone = string.IsNullOrWhiteSpace(dto.Site.Telephone) ? null : dto.Site.Telephone.Trim(),
                        NumeroMobileMoney = string.IsNullOrWhiteSpace(dto.Site.NumeroMobileMoney) ? null : dto.Site.NumeroMobileMoney.Trim(),
                        NomResponsableSite = dto.Site.NomResponsableSite.Trim(),
                        Email = string.IsNullOrWhiteSpace(dto.Site.Email) ? null : dto.Site.Email.Trim(),
                        Genre = dto.Site.Genre.Trim(),
                        Statut = dto.Site.Statut,
                        IsSitePrincipal = true,
                        DateCreation = DateTime.UtcNow
                    };

                    _context.Sites.Add(site);
                    await _context.SaveChangesAsync();

                    await AssignPrincipalSiteToAdminAsync(societe.IdSociete, site.IdSite);

                    var siteEmail = site.Email?.Trim();
                    var siteTelephone = site.Telephone?.Trim();
                    var gerEmail = string.IsNullOrWhiteSpace(siteEmail) ? siteTelephone : siteEmail;
                    if (string.IsNullOrWhiteSpace(gerEmail))
                    {
                        throw new InvalidOperationException(
                            "Au moins un contact du responsable du site est requis: renseignez Site.Email ou Site.Telephone.");
                    }

                    var contact = NormalizeOptionalEmail(societe.EmailContact);
                    if (!string.IsNullOrEmpty(contact) &&
                        string.Equals(gerEmail, contact, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new SocieteBootstrapConflictException(
                            SocieteBootstrapConflictReason.GerantEmailSameAsSocieteContact,
                            "Le contact du gérant (email ou téléphone du site) doit être différent de l'email de contact de la société (compte administrateur).");
                    }

                    if (await _context.Utilisateurs.AnyAsync(u => u.Email == gerEmail))
                    {
                        throw new SocieteBootstrapConflictException(
                            SocieteBootstrapConflictReason.GerantEmailAlreadyExists,
                            "Ce contact est déjà utilisé par un utilisateur (champ Email).");
                    }

                    if (await _context.Agents.AnyAsync(a => a.EmailAgent == gerEmail))
                    {
                        throw new SocieteBootstrapConflictException(
                            SocieteBootstrapConflictReason.AgentGerantEmailAlreadyExists,
                            "Ce contact est déjà utilisé par un agent (champ EmailAgent).");
                    }

                    var matricule = await GenerateMatriculeManagerGeneral(societe);

                    var gerantRole = await GetOrCreateGerantRoleAsync();

                    var gerantAgent = new Agent
                    {
                        NomComplet = site.NomResponsableSite,
                        Genre = site.Genre,
                        DateNaissance = DateTime.UtcNow.AddYears(-35),
                        TelephoneAgent = site.Telephone,
                        EmailAgent = gerEmail,
                        Statut = true,
                        EtatCivil = null,
                        Fonction = "Gérant",
                        RoleAgent = "Gerant",
                        Matricule = matricule,
                        IdSociete = societe.IdSociete,
                        IdSite = site.IdSite,
                        DateCreation = DateTime.Now
                    };

                    _context.Agents.Add(gerantAgent);
                    await _context.SaveChangesAsync();

                    const string motDePasse = "123456";
                    var username = await GenerateUniqueUsernameAsync(site.NomResponsableSite);
                    var hash = BCrypt.Net.BCrypt.HashPassword(motDePasse, BCrypt.Net.BCrypt.GenerateSalt(11));

                    var gerUser = new Utilisateur
                    {
                        IdAgent = gerantAgent.IdAgent,
                        ReferenceUtilisateur = Guid.NewGuid(),
                        NomComplet = gerantAgent.NomComplet,
                        Email = gerEmail,
                        DefaultUsername = username,
                        Telephone = gerantAgent.TelephoneAgent,
                        DateNaissance = gerantAgent.DateNaissance,
                        Genre = gerantAgent.Genre,
                        MotDePasseHash = hash,
                        Statut = true,
                        DateCreation = DateTime.Now,
                        IsConnecte = false,
                        DoitChangerMotDePasse = true,
                        IdRole = gerantRole.IdRole,
                        IdSociete = societe.IdSociete,
                        IdSite = site.IdSite
                    };

                    _context.Utilisateurs.Add(gerUser);
                    await _context.SaveChangesAsync();

                    _context.UserRoles.Add(new UserRole
                    {
                        IdUtilisateur = gerUser.IdUtilisateur,
                        IdRole = gerantRole.IdRole,
                        IsPrimary = true,
                        Statut = true,
                        DateAttribution = DateTime.Now
                    });
                    await _context.SaveChangesAsync();

                    var gerantWelcomeQueued = false;
                    if (!string.IsNullOrWhiteSpace(siteEmail))
                    {
                        gerantWelcomeQueued = true;
                        var nomSociete = societe.Nom ?? "";
                        var emailCopy = siteEmail;
                        var nomCopy = gerUser.NomComplet ?? "";
                        var telCopy = gerantAgent.TelephoneAgent ?? "";
                        var genreCopy = gerantAgent.Genre;
                        var matCopy = gerantAgent.Matricule;
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _emailService.SendWelcomeEmailAsync(
                                    emailCopy,
                                    nomCopy,
                                    username,
                                    telCopy,
                                    motDePasse,
                                    "Gérant",
                                    nomSociete,
                                    genreCopy,
                                    "Gérant",
                                    matCopy);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Échec envoi email bienvenue gérant {Email}", emailCopy);
                            }
                        });
                    }

                    var adminUser = await _context.Utilisateurs
                        .Include(u => u.Role)
                        .Where(u => u.IdSociete == societe.IdSociete && u.Role != null && u.Role.Nom == "Admin")
                        .OrderBy(u => u.IdUtilisateur)
                        .FirstOrDefaultAsync();

                    await transaction.CommitAsync();

                    return new SocieteBootstrapCreationResult
                    {
                        Societe = societe,
                        Site = site,
                        AdminUtilisateur = adminUser,
                        GerantUtilisateur = gerUser,
                        GerantAgent = gerantAgent,
                        GerantMotDePasseParDefaut = motDePasse,
                        GerantWelcomeEmailQueued = gerantWelcomeQueued
                    };
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        private static Societe MapBootstrapToSociete(CreateSocieteBootstrapSocieteDto d)
        {
            return new Societe
            {
                Nom = d.Nom,
                Devise = d.Devise,
                Type = d.Type,
                Logo = d.Logo,
                Telephone = d.Telephone,
                EmailContact = NormalizeOptionalEmail(d.EmailContact),
                SiteWeb = d.SiteWeb,
                NomCompletResponsable = d.NomCompletResponsable,
                GenreResponsable = d.GenreResponsable,
                Description = d.Description,
                AdresseResidence = d.AdresseResidence,
                Statut = d.Statut ?? true
            };
        }

        private async Task<Role> GetOrCreateGerantRoleAsync()
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Nom == "Gerant");
            if (role != null)
                return role;

            role = new Role
            {
                Nom = "Gerant",
                Niveau = 3,
                DateCreation = DateTime.Now,
                Statut = true
            };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
            return role;
        }

        private async Task SeedDefaultCategorieSiegesForSocieteAsync(int idSociete)
        {
            var defaults = new (string Code, string Libelle)[]
            {
                ("ECO", "Économique"),
                ("PREMIERE", "Première classe"),
                ("AFFAIRES", "Classe affaires")
            };

            var anyAdded = false;
            foreach (var (code, libelle) in defaults)
            {
                if (await _context.CategorieSieges.AnyAsync(c => c.IdSociete == idSociete && c.CodeCategorieSiege == code))
                    continue;

                _context.CategorieSieges.Add(new CategorieSiege
                {
                    IdSociete = idSociete,
                    CodeCategorieSiege = code,
                    Libelle = libelle,
                    Statut = true,
                    DateCreation = DateTime.UtcNow
                });
                anyAdded = true;
            }

            if (anyAdded)
                await _context.SaveChangesAsync();
        }

        private async Task SeedDefaultTypeVehiculeForSocieteAsync(int idSociete)
        {
            const string libelle = "Terrestre";

            if (await _context.TypeVehicules.AnyAsync(t => t.IdSociete == idSociete && t.Libelle == libelle))
                return;

            _context.TypeVehicules.Add(new TypeVehicule
            {
                IdSociete = idSociete,
                Libelle = libelle,
                Statut = true,
                DateCreation = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        public async Task<Societe> UpdateAsync(Societe societe)
        {
            var existingSociete = await _context.Societes.FindAsync(societe.IdSociete);
            if (existingSociete == null)
                return null;

            _context.Entry(existingSociete).CurrentValues.SetValues(societe);
            await _context.SaveChangesAsync();
            return existingSociete;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var societe = await _context.Societes.FindAsync(id);
            if (societe == null)
                return false;

            _context.Societes.Remove(societe);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Societes.AnyAsync(e => e.IdSociete == id);
        }

        public async Task<bool> ExistsByNomAsync(string nom)
        {
            return await _context.Societes.AnyAsync(e => e.Nom == nom);
        }

        //public async Task<bool> ExistsByCodeAsync(string code)
        //{
        //    return await _context.Societes.AnyAsync(e => e.Code == code);
        //}

        public async Task<IEnumerable<Utilisateur>> GetUtilisateursAsync(int idSociete)
        {
            return await _context.Utilisateurs
                .Include(u => u.Role)
                .Where(u => u.IdSociete == idSociete)
                .ToListAsync();
        }

        public async Task<IEnumerable<Agent>> GetAgentsAsync(int idSociete)
        {
            var query = _context.Agents.AsNoTracking().Where(e => e.IdSociete == idSociete);

            var hidden = RoleVisibilityHelper.GetHiddenRoleNamesForCaller(_currentUser.PrimaryRole);
            if (hidden.Count > 0)
            {
                var hiddenList = hidden.Select(r => r.ToLowerInvariant()).ToList();
                query = query.Where(a =>
                    string.IsNullOrEmpty(a.RoleAgent) ||
                    !hiddenList.Contains(a.RoleAgent.ToLower()));
            }

            if (!_currentUser.IsSuperAdmin)
                query = query.Where(a => a.IdSociete == _currentUser.SocieteId);

            return await query.ToListAsync();
        }

        public async Task<PagedResult<Agent>> GetAgentsByRoleAsync(int idSociete, string roleNom, PagedRequest request)
        {
            request ??= new PagedRequest();

            if (string.IsNullOrWhiteSpace(roleNom))
            {
                return new PagedResult<Agent>(new List<Agent>(), 0, request.PageNumber, request.PageSize);
            }

            var normalizedRole = roleNom.Trim().ToLower();

            var query = _context.Agents
                .Where(a => a.IdSociete == idSociete &&
                            a.Statut == true &&
                            !string.IsNullOrEmpty(a.RoleAgent) &&
                            a.RoleAgent.ToLower() == normalizedRole);

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                query = query.Where(a =>
                    (a.NomComplet ?? string.Empty).ToLower().Contains(term) ||
                    (a.EmailAgent ?? string.Empty).ToLower().Contains(term) ||
                    (a.TelephoneAgent ?? string.Empty).ToLower().Contains(term) ||
                    (a.Fonction ?? string.Empty).ToLower().Contains(term));
            }

            query = request.SortBy switch
            {
                "NomComplet" => request.SortDescending ? query.OrderByDescending(a => a.NomComplet) : query.OrderBy(a => a.NomComplet),
                "DateCreation" => request.SortDescending ? query.OrderByDescending(a => a.DateCreation) : query.OrderBy(a => a.DateCreation),
                _ => request.SortDescending ? query.OrderByDescending(a => a.IdAgent) : query.OrderBy(a => a.IdAgent)
            };

            var total = await query.CountAsync();

            var data = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResult<Agent>(data, total, request.PageNumber, request.PageSize);
        }

        /// <summary>
        /// ? NOUVELLE LOGIQUE : Cr�e automatiquement un Agent (Manager G�n�ral) et son compte Utilisateur
        /// lors de la cr�ation d'une �cole
        /// 
        /// PROCESSUS :
        /// 1. Cr�er un Agent avec la fonction "Manager G�n�ral"
        /// 2. Cr�er un Utilisateur li� � cet Agent avec le r�le "Admin"
        /// 
        /// Cette approche respecte la logique m�tier :
        /// - Un Utilisateur est soit un Agent, soit un Technicien
        /// - Le Manager G�n�ral est un Agent avec des droits Admin sur toute l'�cole
        /// </summary>
        /// <param name="suppressErrors">Si false (bootstrap transactionnel), les erreurs remontent pour rollback ; si true (création simple), log seulement.</param>
        private async Task CreateDefaultGerantAgentAsync(Societe societe, bool suppressErrors = true)
        {
            try
            {
                // ? V�RIFICATION UNICIT� EMAIL : V�rifier si l'email existe d�j�
                var emailGerant = NormalizeOptionalEmail(societe.EmailContact);
                
                if (!string.IsNullOrEmpty(emailGerant))
                {
                    var emailExists = await _context.Utilisateurs.AnyAsync(u => u.Email == emailGerant);
                    if (emailExists)
                    {
                        if (!suppressErrors)
                        {
                            throw new SocieteBootstrapConflictException(
                                SocieteBootstrapConflictReason.SocieteContactEmailAlreadyUsed,
                                $"L'email de contact '{emailGerant}' est déjà utilisé par un utilisateur. Impossible de créer le compte administrateur.");
                        }

                        _logger.LogWarning("Un utilisateur avec l'email '{Email}' existe déjà. Agent administrateur par défaut non créé pour la société '{SocieteNom}'.",
                            emailGerant, societe.Nom);
                        return;
                    }
                }

                // 1?? CR�ER L'AGENT MANAGER G�N�RAL
                string nomCompletResponsable = societe.NomCompletResponsable?.Trim() ?? "Manager General";

                var managerAgent = new Agent
                {
                    NomComplet = nomCompletResponsable,
                    Genre = societe.GenreResponsable ?? "Masculin",
                    DateNaissance = DateTime.Now.AddYears(-35), // Age par d�faut : 35 ans
                    TelephoneAgent = societe.Telephone,
                    EmailAgent = emailGerant,
                    Statut = true,
                    EtatCivil = "Mari�",
                    Fonction = "Manager G�n�ral", // ? Fonction Manager G�n�ral de l'�cole
                    RoleAgent = "Admin",
                    IdSociete = societe.IdSociete,
                    // Note: L'adresse n'est plus copiée car Agent n'hérite plus de Adresse
                    // L'agent peut avoir son AdresseResidence défini séparément si nécessaire
                    DateCreation = DateTime.Now
                };

                // G�n�rer le matricule pour l'agent manager
                string matricule = await GenerateMatriculeManagerGeneral(societe);
                managerAgent.Matricule = matricule;

                _context.Agents.Add(managerAgent);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("? Agent Manager G?n?ral cr?? : {NomComplet} - Matricule: {Matricule}", 
                    nomCompletResponsable, matricule);

                // 2?? CR�ER L'UTILISATEUR LI� � CET AGENT
                await CreateDefaultGerantUserForAgentAsync(managerAgent, societe);
            }
            catch (SocieteBootstrapConflictException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (!suppressErrors)
                    throw;

                _logger.LogError(ex, "Erreur lors de la création de l'agent administrateur par défaut: {ErrorMessage}", ex.Message);
            }
        }

        /// <summary>
        /// G�n�re un matricule unique pour le Manager G�n�ral (Agent)
        /// Format: [NAT][Ann�e(2)]-[GUID(6)]
        /// </summary>
        private async Task<string> GenerateMatriculeManagerGeneral(Societe societe)
        {
            string matricule;
            
            do
            {
                // Pr�fixe national pour tous les agents
                string annee = DateTime.Now.Year.ToString().Substring(2);
                string guid = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
                matricule = $"NAT{annee}-{guid}";
                
            } while (await _context.Agents.AnyAsync(a => a.Matricule == matricule));
            
            return matricule;
        }

        /// <summary>
        /// Crée un compte Utilisateur Admin lié à l'agent Manager Général
        /// </summary>
        private async Task CreateDefaultGerantUserForAgentAsync(Agent managerAgent, Societe societe)
        {
            try
            {
                var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Nom == "Admin");
                if (adminRole == null)
                {
                    adminRole = new Role
                    {
                        Nom = "Admin",
                        Niveau = 2,
                        DateCreation = DateTime.Now,
                        Statut = true
                    };
                    _context.Roles.Add(adminRole);
                    await _context.SaveChangesAsync();
                }

                var emailAdmin = NormalizeOptionalEmail(managerAgent.EmailAgent);
                string nomComplet = managerAgent.NomComplet ?? "Manager Général";
                
                // ? V�rification finale de l'email (double s�curit�)
                if (!string.IsNullOrEmpty(emailAdmin))
                {
                    var emailExists = await _context.Utilisateurs.AnyAsync(u => u.Email == emailAdmin);
                    if (emailExists)
                    {
                        _logger.LogWarning("?? Email '{Email}' d?j? utilis?. Utilisateur admin non cr??.", emailAdmin);
                        return;
                    }
                }
                
                // ? G�n�rer un username unique
                string defaultUsername = await GenerateUniqueUsernameAsync(nomComplet);
                
                // Mot de passe par d�faut
                string motDePasseParDefaut = "123456";
                
                // Cr�er l'utilisateur Admin li� � l'agent Manager G�n�ral
                var adminUser = new Utilisateur
                {
                    IdAgent = managerAgent.IdAgent,
                    ReferenceUtilisateur = Guid.NewGuid(),
                    NomComplet = managerAgent.NomComplet,
                    Email = emailAdmin,
                    DefaultUsername = defaultUsername,
                    Telephone = managerAgent.TelephoneAgent,
                    PhotoUrl = managerAgent.PhotoUrl,
                    DateNaissance = managerAgent.DateNaissance,
                    Genre = managerAgent.Genre,
                    MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(motDePasseParDefaut),
                    Statut = true,
                    DateCreation = DateTime.Now,
                    IsConnecte = false,
                    DoitChangerMotDePasse = true,
                    IdRole = adminRole.IdRole,
                    IdSociete = societe.IdSociete
                    // Note: L'adresse de l'agent n'est plus copiée car Agent n'hérite plus de Adresse
                    // L'utilisateur peut avoir sa propre adresse via les champs hérités de Adresse
                };

                _context.Utilisateurs.Add(adminUser);
                await _context.SaveChangesAsync();
                
                // ✅ Créer aussi l’entrée UserRole pour activer le rôle côté authentification
                var userRole = new UserRole
                {
                    IdUtilisateur = adminUser.IdUtilisateur,
                    IdRole = adminRole.IdRole,
                    IsPrimary = true,
                    Statut = true,
                    DateAttribution = DateTime.Now
                };
                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Utilisateur Admin créé pour le Manager général: {NomComplet} - Email: {Email}", nomComplet, emailAdmin);
                
                // Envoyer l'email de bienvenue (si email fourni)
                if (!string.IsNullOrWhiteSpace(emailAdmin))
                {
                    string nomSociete = societe.Nom ?? "CongoTravel";
                    
                    // Envoi asynchrone (ne bloque pas si �chec)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _emailService.SendWelcomeEmailAsync(
                                emailAdmin,
                                nomComplet,
                                defaultUsername,
                                managerAgent.TelephoneAgent ?? "",
                                motDePasseParDefaut,
                                "Administrateur",
                                nomSociete,
                                managerAgent.Genre,
                                "Manager G�n�ral", // Fonction
                                managerAgent.Matricule // Matricule
                            );
                            
                            _logger.LogInformation("?? Email de bienvenue envoy? au Manager G?n?ral: {Email}", emailAdmin);
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogError(emailEx, "?? �chec de l'envoi de l'email � {Email}: {ErrorMessage}", emailAdmin, emailEx.Message);
                        }
                    });
                }
                else
                {
                    _logger.LogWarning("?? Aucun email fourni pour le Manager Gu00e9nu00e9ral: {NomComplet}", nomComplet);
                }
            }
            catch (Exception ex)
            {
                // Log l'erreur mais ne pas faire �chouer la cr�ation de l'�cole
                _logger.LogError(ex, "Erreur lors de la création de l'utilisateur Admin pour l'agent Manager général: {ErrorMessage}", ex.Message);
            }
        }

        /// <summary>
        /// ? AM�LIORATION : G�n�re un nom d'utilisateur UNIQUE avec v�rification en boucle
        /// Format: [NomResponsable][NombreAleatoire]
        /// Exemple: "Peter Tendayo" ? "PeterTendayo123"
        /// Garantit l'unicit� en v�rifiant dans la base de donn�es
        /// </summary>
        private async Task<string> GenerateUniqueUsernameAsync(string nomComplet)
        {
            if (string.IsNullOrWhiteSpace(nomComplet))
            {
                nomComplet = "Admin";
            }
            
            // Supprimer les espaces et les caract�res sp�ciaux
            string baseUsername = nomComplet.Replace(" ", "").Replace("-", "").Replace("'", "");
            
            // Limiter � 20 caract�res pour le nom de base
            if (baseUsername.Length > 20)
            {
                baseUsername = baseUsername.Substring(0, 20);
            }
            
            string username;
            int attempts = 0;
            int maxAttempts = 100; // Limite de s�curit� pour �viter une boucle infinie
            
            do
            {
                // G�n�rer un nombre al�atoire entre 1 et 9999 (plus large pour r�duire les collisions)
                Random random = new Random(Guid.NewGuid().GetHashCode()); // Seed unique pour meilleure randomisation
                int randomNumber = random.Next(1, 10000);
                
                // Combiner le nom de base avec le nombre al�atoire
                username = $"{baseUsername}{randomNumber}";
                
                attempts++;
                
                // V�rifier l'unicit� dans la base de donn�es
                var usernameExists = await _context.Utilisateurs.AnyAsync(u => u.DefaultUsername == username);
                
                if (!usernameExists)
                {
                    _logger.LogInformation("? Username unique g?n?r?: {Username} (tentative {Attempts})", username, attempts);
                    break; // Username unique trouv� !
                }
                
                if (attempts >= maxAttempts)
                {
                    // Si on a dépassé le nombre max de tentatives, ajouter un GUID partiel
                    string guidSuffix = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
                    username = $"{baseUsername}{guidSuffix}";
                    _logger.LogWarning("?? Max tentatives atteint. Username avec GUID généré: {Username}", username);
                    break;
                }
                
            } while (true);
            
            return username;
        }

        /// <summary>
        /// [DEPRECATED] Ancienne m�thode sans v�rification d'unicit� - conserv�e pour r�f�rence
        /// Utilisez GenerateUniqueUsernameAsync() � la place
        /// </summary>
        [Obsolete("Utilisez GenerateUniqueUsernameAsync() pour garantir l'unicit�")]
        private string GenerateUsernameFromName(string nomComplet)
        {
            if (string.IsNullOrWhiteSpace(nomComplet))
            {
                nomComplet = "Admin";
            }
            
            // Supprimer les espaces et les caract�res sp�ciaux
            string baseUsername = nomComplet.Replace(" ", "").Replace("-", "").Replace("'", "");
            
            // Limiter � 20 caract�res pour le nom de base
            if (baseUsername.Length > 20)
            {
                baseUsername = baseUsername.Substring(0, 20);
            }
            
            // G�n�rer un nombre al�atoire entre 1 et 999
            Random random = new Random();
            int randomNumber = random.Next(1, 1000);
            
            // Combiner le nom de base avec le nombre al�atoire
            string username = $"{baseUsername}{randomNumber}";
            
            return username;
        }

        // ? SOFT DELETE: Toggle le statut d'une �cole (actif <-> inactif)
        public async Task<bool> ToggleStatutAsync(int id)
        {
            var societe = await _context.Societes.FindAsync(id);
            if (societe == null)
                return false;

            societe.Statut = societe.Statut != true;
            await _context.SaveChangesAsync();
            return true;
        }
        
        // ? SOFT DELETE: D�finir une valeur sp�cifique pour le statut d'une �cole
        public async Task<bool> SetStatutAsync(int id, bool statut)
        {
            var societe = await _context.Societes.FindAsync(id);
            if (societe == null)
                return false;

            societe.Statut = statut;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Rattache l'agent Admin et son utilisateur au site principal créé lors du bootstrap.
        /// </summary>
        private async Task AssignPrincipalSiteToAdminAsync(int idSociete, int idSite)
        {
            var adminAgent = await _context.Agents
                .FirstOrDefaultAsync(a => a.IdSociete == idSociete && a.RoleAgent == "Admin");

            if (adminAgent == null)
                return;

            adminAgent.IdSite = idSite;

            var adminUser = await _context.Utilisateurs
                .FirstOrDefaultAsync(u => u.IdAgent == adminAgent.IdAgent);

            if (adminUser != null)
                adminUser.IdSite = idSite;

            await _context.SaveChangesAsync();
        }

        /// <summary>Chaîne vide ou blanc → null pour respecter l'unicité nullable (plusieurs comptes sans email).</summary>
        private static string? NormalizeOptionalEmail(string? email) =>
            string.IsNullOrWhiteSpace(email) ? null : email.Trim();
    }
}
