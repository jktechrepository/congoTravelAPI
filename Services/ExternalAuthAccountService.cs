using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Authentification;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace CongoTravel.Services
{
    /// <summary>
    /// Lookup / link / create Client+Utilisateur pour les providers OAuth externes.
    /// </summary>
    public class ExternalAuthAccountService
    {
        private readonly CongoTravelDbContext _context;
        private readonly ILogger<ExternalAuthAccountService> _logger;

        public ExternalAuthAccountService(
            CongoTravelDbContext context,
            ILogger<ExternalAuthAccountService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Utilisateur> ResolveUserAsync(
            string authProvider,
            ExternalAuthIdentity identity,
            bool requireEmailVerifiedForCreateOrLink,
            CancellationToken cancellationToken = default)
        {
            var utilisateur = await FindByProviderSubAsync(authProvider, identity.Sub, cancellationToken);

            if (utilisateur == null)
            {
                if (string.IsNullOrWhiteSpace(identity.Email))
                {
                    throw new ExternalAuthException(400,
                        $"L'email {authProvider} est requis pour créer ou lier un compte (souvent uniquement à la première connexion).");
                }

                if (requireEmailVerifiedForCreateOrLink && !identity.EmailVerified)
                {
                    throw new ExternalAuthException(400,
                        $"L'email {authProvider} doit être vérifié pour créer ou lier un compte.");
                }

                var email = NormalizeEmail(identity.Email);
                utilisateur = await FindByEmailAsync(email, cancellationToken);

                if (utilisateur != null)
                {
                    LinkProvider(utilisateur, authProvider, identity);
                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation(
                        "Compte existant {UserId} lié à {Provider} sub {Sub}",
                        utilisateur.IdUtilisateur, authProvider, identity.Sub);
                }
                else
                {
                    utilisateur = await CreateClientAndUserAsync(authProvider, identity, email, cancellationToken);
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(identity.Picture) && string.IsNullOrWhiteSpace(utilisateur.PhotoUrl))
                    utilisateur.PhotoUrl = identity.Picture;
                utilisateur.EmailVerified = identity.EmailVerified;
                if (!string.IsNullOrWhiteSpace(identity.Email) && string.IsNullOrWhiteSpace(utilisateur.Email))
                    utilisateur.Email = NormalizeEmail(identity.Email);
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (utilisateur.Statut == false)
                throw new ExternalAuthException(403, "Ce compte est désactivé.");

            return utilisateur;
        }

        private Task<Utilisateur?> FindByProviderSubAsync(string provider, string sub, CancellationToken ct) =>
            _context.Utilisateurs
                .FirstOrDefaultAsync(
                    u => u.AuthProvider == provider && u.ExternalSubjectId == sub,
                    ct);

        private async Task<Utilisateur?> FindByEmailAsync(string email, CancellationToken ct)
        {
            var user = await _context.Utilisateurs
                .FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == email, ct);
            if (user != null)
                return user;

            var client = await _context.Clients
                .FirstOrDefaultAsync(
                    c => c.EmailClient != null && c.EmailClient.ToLower() == email && c.Statut,
                    ct);
            if (client == null)
                return null;

            return await _context.Utilisateurs
                .FirstOrDefaultAsync(u => u.IdClient == client.IdClient, ct);
        }

        private static void LinkProvider(Utilisateur utilisateur, string authProvider, ExternalAuthIdentity identity)
        {
            if (!string.IsNullOrWhiteSpace(utilisateur.ExternalSubjectId)
                && !string.Equals(utilisateur.ExternalSubjectId, identity.Sub, StringComparison.Ordinal)
                && string.Equals(utilisateur.AuthProvider, authProvider, StringComparison.OrdinalIgnoreCase))
            {
                throw new ExternalAuthException(409, $"Cet email est déjà lié à un autre compte {authProvider}.");
            }

            // Ne pas écraser un autre provider déjà lié à un sub différent
            if (!string.IsNullOrWhiteSpace(utilisateur.AuthProvider)
                && !string.Equals(utilisateur.AuthProvider, authProvider, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(utilisateur.ExternalSubjectId)
                && !string.Equals(utilisateur.ExternalSubjectId, identity.Sub, StringComparison.Ordinal))
            {
                throw new ExternalAuthException(409,
                    $"Cet email est déjà lié au provider {utilisateur.AuthProvider}.");
            }

            utilisateur.AuthProvider = authProvider;
            utilisateur.ExternalSubjectId = identity.Sub;
            utilisateur.EmailVerified = identity.EmailVerified;
            if (!string.IsNullOrWhiteSpace(identity.Picture) && string.IsNullOrWhiteSpace(utilisateur.PhotoUrl))
                utilisateur.PhotoUrl = identity.Picture;
            if (!string.IsNullOrWhiteSpace(identity.Name) && string.IsNullOrWhiteSpace(utilisateur.NomComplet))
                utilisateur.NomComplet = identity.Name.Trim();
        }

        private async Task<Utilisateur> CreateClientAndUserAsync(
            string authProvider,
            ExternalAuthIdentity identity,
            string email,
            CancellationToken ct)
        {
            // MySqlRetryingExecutionStrategy : transactions utilisateur dans ExecuteAsync.
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                var clientRole = await _context.Roles.FirstOrDefaultAsync(r => r.Nom == "Client", ct);
                if (clientRole == null)
                    throw new ExternalAuthException(500, "Le rôle 'Client' n'existe pas.");

                var societe = await _context.Societes.FirstOrDefaultAsync(ct);
                if (societe == null)
                    throw new ExternalAuthException(500, "Aucune société trouvée. Impossible de créer un compte client.");

                var emailTaken = await _context.Clients
                    .AnyAsync(c => c.EmailClient != null && c.EmailClient.ToLower() == email, ct);
                if (emailTaken)
                    throw new ExternalAuthException(409, "Cet email est déjà utilisé par un autre client.");

                var nom = string.IsNullOrWhiteSpace(identity.Name) ? email.Split('@')[0] : identity.Name.Trim();

                await using var tx = await _context.Database.BeginTransactionAsync(ct);
                try
                {
                    var client = new Client
                    {
                        NomClient = nom,
                        EmailClient = email,
                        Telephone = null,
                        Statut = true,
                        IsActif = true,
                        DateCreation = DateTime.UtcNow
                    };
                    _context.Clients.Add(client);
                    await _context.SaveChangesAsync(ct);

                    var randomPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                    var utilisateur = new Utilisateur
                    {
                        IdClient = client.IdClient,
                        ReferenceUtilisateur = Guid.NewGuid(),
                        NomComplet = nom,
                        Email = email,
                        DefaultUsername = $"client_{client.IdClient}_{Guid.NewGuid():N}",
                        Telephone = null,
                        PhotoUrl = identity.Picture,
                        MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(randomPassword),
                        Statut = true,
                        DateCreation = DateTime.UtcNow,
                        IsConnecte = false,
                        DoitChangerMotDePasse = false,
                        IdSociete = societe.IdSociete,
                        AuthProvider = authProvider,
                        ExternalSubjectId = identity.Sub,
                        EmailVerified = identity.EmailVerified
                    };

                    _context.Utilisateurs.Add(utilisateur);
                    await _context.SaveChangesAsync(ct);

                    _context.UserRoles.Add(new UserRole
                    {
                        IdUtilisateur = utilisateur.IdUtilisateur,
                        IdRole = clientRole.IdRole,
                        IsPrimary = true,
                        Statut = true,
                        DateAttribution = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);

                    _logger.LogInformation(
                        "Nouveau compte {Provider} créé: Client {ClientId}, Utilisateur {UserId}, sub {Sub}",
                        authProvider, client.IdClient, utilisateur.IdUtilisateur, identity.Sub);

                    return utilisateur;
                }
                catch (ExternalAuthException)
                {
                    await tx.RollbackAsync(ct);
                    throw;
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync(ct);
                    _logger.LogError(ex, "Échec création compte {Provider} pour {Email}", authProvider, email);
                    throw new ExternalAuthException(500, $"Erreur lors de la création du compte {authProvider}.");
                }
            });
        }

        private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
    }
}
