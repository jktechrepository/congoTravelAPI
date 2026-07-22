using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Site;
using CongoTravel.Services.Repositories;

namespace CongoTravel.Services
{
    public class SiteService : ISiteRepository
    {
        private readonly CongoTravelDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<SiteService> _logger;

        public SiteService(
            CongoTravelDbContext context,
            IEmailService emailService,
            ILogger<SiteService> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<IReadOnlyList<Site>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Sites.AsNoTracking()
                .OrderBy(a => a.IdSociete)
                .ThenBy(a => a.CodeSite)
                .ToListAsync(cancellationToken);
        }

        public async Task<Site?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Sites.AsNoTracking()
                .FirstOrDefaultAsync(a => a.IdSite == id, cancellationToken);
        }

        public async Task<IReadOnlyList<Site>> GetBySocieteAsync(int idSociete, CancellationToken cancellationToken = default)
        {
            return await _context.Sites.AsNoTracking()
                .Where(a => a.IdSociete == idSociete)
                .OrderBy(a => a.CodeSite)
                .ToListAsync(cancellationToken);
        }

        public async Task<Site> CreateAsync(Site site, CancellationToken cancellationToken = default)
        {
            site.CodeSite = site.CodeSite.Trim();
            site.NomSite = site.NomSite.Trim();
            site.NomResponsableSite = site.NomResponsableSite.Trim();
            site.Email = string.IsNullOrWhiteSpace(site.Email) ? null : site.Email.Trim();
            site.Telephone = string.IsNullOrWhiteSpace(site.Telephone) ? null : site.Telephone.Trim();
            site.NumeroMobileMoney = string.IsNullOrWhiteSpace(site.NumeroMobileMoney) ? null : site.NumeroMobileMoney.Trim();
            site.Genre = site.Genre.Trim();
            site.DateCreation = DateTime.UtcNow;
            site.DateModification = null;

            var exists = await _context.Sites.AnyAsync(
                a => a.IdSociete == site.IdSociete && a.CodeSite == site.CodeSite,
                cancellationToken);
            if (exists)
                throw new InvalidOperationException(
                    $"Le code d'site '{site.CodeSite}' existe déjà pour cette société.");

            _context.Sites.Add(site);
            await _context.SaveChangesAsync(cancellationToken);

            if (site.IsSitePrincipal)
            {
                if (!site.Statut)
                    throw new InvalidOperationException("Un site principal doit être actif.");
                await SitePrincipalHelper.EnsureSinglePrincipalAsync(
                    _context, site.IdSociete, site.IdSite, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return site;
        }

        /// <inheritdoc />
        public async Task<SiteBootstrapCreationResult> CreateWithGerantAsync(
            SiteCreateDto dto,
            CancellationToken cancellationToken = default)
        {
            // MySqlRetryingExecutionStrategy : les transactions utilisateur doivent être dans ExecuteAsync.
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var societe = await _context.Societes.AsNoTracking()
                        .FirstOrDefaultAsync(s => s.IdSociete == dto.IdSociete, cancellationToken);
                    if (societe == null)
                        throw new InvalidOperationException($"Société {dto.IdSociete} introuvable.");

                    var codeSite = dto.CodeSite.Trim();
                    if (await _context.Sites.AnyAsync(
                            s => s.IdSociete == dto.IdSociete && s.CodeSite == codeSite,
                            cancellationToken))
                    {
                        throw new SiteBootstrapConflictException(
                            SiteBootstrapConflictReason.SiteCodeAlreadyExists,
                            $"Le code de site '{codeSite}' existe déjà pour cette société.");
                    }

                    var site = new Site
                    {
                        IdSociete = dto.IdSociete,
                        CodeSite = codeSite,
                        NomSite = dto.NomSite.Trim(),
                        Ville = string.IsNullOrWhiteSpace(dto.Ville) ? null : dto.Ville.Trim(),
                        Adresse = string.IsNullOrWhiteSpace(dto.Adresse) ? null : dto.Adresse.Trim(),
                        Telephone = string.IsNullOrWhiteSpace(dto.Telephone) ? null : dto.Telephone.Trim(),
                        NumeroMobileMoney = string.IsNullOrWhiteSpace(dto.NumeroMobileMoney) ? null : dto.NumeroMobileMoney.Trim(),
                        NomResponsableSite = dto.NomResponsableSite.Trim(),
                        Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
                        Genre = dto.Genre.Trim(),
                        Statut = dto.Statut,
                        IsSitePrincipal = dto.IsSitePrincipal,
                        DateCreation = DateTime.UtcNow,
                        DateModification = null
                    };

                    if (site.IsSitePrincipal && !site.Statut)
                    {
                        throw new InvalidOperationException("Un site principal doit être actif.");
                    }

                    _context.Sites.Add(site);
                    await _context.SaveChangesAsync(cancellationToken);

                    if (site.IsSitePrincipal)
                    {
                        await SitePrincipalHelper.EnsureSinglePrincipalAsync(
                            _context, site.IdSociete, site.IdSite, cancellationToken);
                        await _context.SaveChangesAsync(cancellationToken);
                    }

                    var siteEmail = site.Email?.Trim();
                    var siteTelephone = site.Telephone?.Trim();
                    var gerEmail = string.IsNullOrWhiteSpace(siteEmail) ? siteTelephone : siteEmail;
                    if (string.IsNullOrWhiteSpace(gerEmail))
                    {
                        throw new InvalidOperationException(
                            "Au moins un contact du responsable du site est requis: renseignez Email ou Telephone.");
                    }

                    var contact = societe.EmailContact?.Trim();
                    if (!string.IsNullOrEmpty(contact) &&
                        string.Equals(gerEmail, contact, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new SiteBootstrapConflictException(
                            SiteBootstrapConflictReason.GerantEmailSameAsSocieteContact,
                            "L'email du gérant doit être différent de l'email de contact de la société (compte administrateur).");
                    }

                    if (await _context.Utilisateurs.AnyAsync(u => u.Email == gerEmail, cancellationToken))
                    {
                        throw new SiteBootstrapConflictException(
                            SiteBootstrapConflictReason.GerantEmailAlreadyExists,
                            "Cet email est déjà utilisé par un utilisateur.");
                    }

                    if (await _context.Agents.AnyAsync(a => a.EmailAgent == gerEmail, cancellationToken))
                    {
                        throw new SiteBootstrapConflictException(
                            SiteBootstrapConflictReason.AgentGerantEmailAlreadyExists,
                            "Cet email est déjà utilisé par un agent.");
                    }

                    var matricule = await GenerateUniqueMatriculeAsync(cancellationToken);

                    var gerantRole = await GetOrCreateGerantRoleAsync(cancellationToken);

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
                        DateCreation = DateTime.UtcNow
                    };

                    _context.Agents.Add(gerantAgent);
                    await _context.SaveChangesAsync(cancellationToken);

                    var motDePasse = "123456";
                    var username = await GenerateUniqueUsernameAsync(site.NomResponsableSite, cancellationToken);
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
                        DateCreation = DateTime.UtcNow,
                        IsConnecte = false,
                        DoitChangerMotDePasse = true,
                        IdRole = gerantRole.IdRole,
                        IdSociete = societe.IdSociete,
                        IdSite = site.IdSite
                    };

                    _context.Utilisateurs.Add(gerUser);
                    await _context.SaveChangesAsync(cancellationToken);

                    _context.UserRoles.Add(new UserRole
                    {
                        IdUtilisateur = gerUser.IdUtilisateur,
                        IdRole = gerantRole.IdRole,
                        IsPrimary = true,
                        Statut = true,
                        DateAttribution = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync(cancellationToken);

                    if (!string.IsNullOrWhiteSpace(siteEmail))
                    {
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
                                    genreCopy ?? "Masculin",
                                    "Gérant",
                                    matCopy);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Échec envoi email bienvenue gérant site {Email}", emailCopy);
                            }
                        });
                    }

                    await transaction.CommitAsync(cancellationToken);

                    return new SiteBootstrapCreationResult
                    {
                        Site = site,
                        GerantAgent = gerantAgent,
                        GerantUtilisateur = gerUser,
                        GerantMotDePasseParDefaut = motDePasse
                    };
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }

        private async Task<Role> GetOrCreateGerantRoleAsync(CancellationToken cancellationToken)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Nom == "Gerant", cancellationToken);
            if (role != null)
                return role;

            role = new Role
            {
                Nom = "Gerant",
                Niveau = 3,
                DateCreation = DateTime.UtcNow,
                Statut = true
            };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync(cancellationToken);
            return role;
        }

        private async Task<string> GenerateUniqueMatriculeAsync(CancellationToken cancellationToken)
        {
            string matricule;
            do
            {
                var annee = DateTime.UtcNow.Year.ToString().Substring(2);
                var guid = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
                matricule = $"NAT{annee}-{guid}";
            } while (await _context.Agents.AnyAsync(a => a.Matricule == matricule, cancellationToken));

            return matricule;
        }

        /// <summary>Génère un DefaultUsername unique (même logique que SocieteService).</summary>
        private async Task<string> GenerateUniqueUsernameAsync(string nomComplet, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(nomComplet))
                nomComplet = "Gerant";

            var baseUsername = nomComplet.Replace(" ", "").Replace("-", "").Replace("'", "");
            if (baseUsername.Length > 20)
                baseUsername = baseUsername.Substring(0, 20);

            string username;
            var attempts = 0;
            const int maxAttempts = 100;

            while (true)
            {
                var random = new Random(Guid.NewGuid().GetHashCode());
                var randomNumber = random.Next(1, 10000);
                username = $"{baseUsername}{randomNumber}";
                attempts++;

                var usernameExists = await _context.Utilisateurs.AnyAsync(u => u.DefaultUsername == username, cancellationToken);
                if (!usernameExists)
                    break;

                if (attempts >= maxAttempts)
                {
                    var guidSuffix = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
                    username = $"{baseUsername}{guidSuffix}";
                    break;
                }
            }

            return username;
        }

        public async Task<Site?> UpdateAsync(
            Site site,
            bool? isSitePrincipal = null,
            CancellationToken cancellationToken = default)
        {
            var existing = await _context.Sites.FirstOrDefaultAsync(a => a.IdSite == site.IdSite, cancellationToken);
            if (existing == null)
                return null;

            var code = site.CodeSite.Trim();
            var dup = await _context.Sites.AnyAsync(
                a => a.IdSociete == existing.IdSociete && a.CodeSite == code && a.IdSite != site.IdSite,
                cancellationToken);
            if (dup)
                throw new InvalidOperationException($"Le code d'site '{code}' existe déjà pour cette société.");

            if (existing.IsSitePrincipal && !site.Statut)
            {
                throw new InvalidOperationException(
                    "Impossible de désactiver le site principal. Transférez d'abord le statut de site principal à un autre site actif.");
            }

            existing.CodeSite = code;
            existing.NomSite = site.NomSite.Trim();
            existing.Ville = string.IsNullOrWhiteSpace(site.Ville) ? null : site.Ville.Trim();
            existing.Adresse = string.IsNullOrWhiteSpace(site.Adresse) ? null : site.Adresse.Trim();
            existing.Telephone = string.IsNullOrWhiteSpace(site.Telephone) ? null : site.Telephone.Trim();
            existing.NumeroMobileMoney = string.IsNullOrWhiteSpace(site.NumeroMobileMoney) ? null : site.NumeroMobileMoney.Trim();
            existing.NomResponsableSite = site.NomResponsableSite.Trim();
            existing.Email = string.IsNullOrWhiteSpace(site.Email) ? null : site.Email.Trim();
            existing.Genre = site.Genre.Trim();
            existing.Statut = site.Statut;

            if (isSitePrincipal == true)
            {
                if (!site.Statut)
                {
                    throw new InvalidOperationException("Un site principal doit être actif.");
                }

                await SitePrincipalHelper.EnsureSinglePrincipalAsync(
                    _context, existing.IdSociete, existing.IdSite, cancellationToken);
                existing.IsSitePrincipal = true;

                var hasOwnFlexPay = await _context.InfoPaiementsSociete.AsNoTracking()
                    .AnyAsync(i => i.IdSite == existing.IdSite && i.IdSociete == existing.IdSociete && i.Statut, cancellationToken);
                if (!hasOwnFlexPay)
                {
                    _logger.LogWarning(
                        "Site {IdSite} défini comme principal sans InfoPaiement propre — FlexPay utilisera le repli vers une autre config active de la société {IdSociete} si disponible.",
                        existing.IdSite, existing.IdSociete);
                }
            }
            else if (isSitePrincipal == false && existing.IsSitePrincipal)
            {
                existing.IsSitePrincipal = false;
            }

            existing.DateModification = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            return existing;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _context.Sites.FirstOrDefaultAsync(a => a.IdSite == id, cancellationToken);
            if (entity == null)
                return false;

            _context.Sites.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> ToggleStatutAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _context.Sites.FirstOrDefaultAsync(a => a.IdSite == id, cancellationToken);
            if (entity == null)
                return false;

            if (entity.IsSitePrincipal && entity.Statut)
            {
                throw new InvalidOperationException(
                    "Impossible de désactiver le site principal. Transférez d'abord le statut de site principal à un autre site actif.");
            }

            entity.Statut = !entity.Statut;
            entity.DateModification = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
