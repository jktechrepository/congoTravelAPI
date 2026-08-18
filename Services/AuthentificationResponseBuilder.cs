using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Models.DTOs.Authentification;
using CongoTravel.Services.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CongoTravel.Services
{
    /// <summary>
    /// Construit <see cref="AuthentificationResponse"/> comme <c>POST /api/Utilisateur/authentifier</c>.
    /// </summary>
    public class AuthentificationResponseBuilder
    {
        private readonly CongoTravelDbContext _context;
        private readonly ISimpleJwtService _jwtService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IPermissionService _permissionService;
        private readonly IConfigSocieteRepository _configSocieteRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthentificationResponseBuilder> _logger;

        public AuthentificationResponseBuilder(
            CongoTravelDbContext context,
            ISimpleJwtService jwtService,
            IRefreshTokenService refreshTokenService,
            IPermissionService permissionService,
            IConfigSocieteRepository configSocieteRepository,
            IConfiguration configuration,
            ILogger<AuthentificationResponseBuilder> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _refreshTokenService = refreshTokenService;
            _permissionService = permissionService;
            _configSocieteRepository = configSocieteRepository;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AuthentificationResponse> BuildAsync(
            Utilisateur utilisateur,
            string? deviceInfo = null,
            string? ipAddress = null,
            CancellationToken cancellationToken = default)
        {
            var loaded = await _context.Utilisateurs
                .Include(u => u.Societe)
                .Include(u => u.Client)
                .Include(u => u.Agent)
                .Include(u => u.UserRoles.Where(ur => ur.Statut == true))
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.IdUtilisateur == utilisateur.IdUtilisateur, cancellationToken);

            if (loaded == null)
                throw new ExternalAuthException(404, "Informations utilisateur non trouvées");

            if (utilisateur.IdAgent.HasValue)
                loaded.IdAgent = utilisateur.IdAgent;
            if (utilisateur.IdClient.HasValue)
                loaded.IdClient = utilisateur.IdClient;

            loaded.IsConnecte = true;
            await _context.SaveChangesAsync(cancellationToken);

            var accessToken = _jwtService.GenerateToken(loaded, loaded.IdAgent);
            var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "1440");
            var refreshToken = await _refreshTokenService.GenerateRefreshTokenAsync(
                loaded.IdUtilisateur,
                string.IsNullOrWhiteSpace(deviceInfo) ? null : deviceInfo,
                ipAddress);

            var permissionsList = (await _permissionService.GetUserPermissionsAsync(loaded.IdUtilisateur)).ToList();
            var userRolesList = (await _permissionService.GetUserRolesAsync(loaded.IdUtilisateur)).ToList();
            var primaryRole = await _permissionService.GetUserPrimaryRoleAsync(loaded.IdUtilisateur);

            ClientInfoDto? clientInfo = null;
            if (loaded.Client != null)
            {
                clientInfo = MapClient(loaded.Client);
            }
            else if (loaded.IdClient.HasValue)
            {
                var client = await _context.Clients
                    .FirstOrDefaultAsync(c => c.IdClient == loaded.IdClient.Value, cancellationToken);
                if (client != null)
                    clientInfo = MapClient(client);
            }

            AgentInfoDto? agentInfo = null;
            if (loaded.Agent != null)
            {
                agentInfo = MapAgent(loaded.Agent);
            }
            else if (loaded.IdAgent.HasValue)
            {
                var agent = await _context.Agents.FindAsync(new object[] { loaded.IdAgent.Value }, cancellationToken);
                if (agent != null)
                    agentInfo = MapAgent(agent);
            }

            _logger.LogInformation(
                "AuthentificationResponse construite pour utilisateur {UserId}",
                loaded.IdUtilisateur);

            var activitesSociete = new List<string>();
            if (loaded.IdSociete is > 0)
            {
                var config = await _configSocieteRepository.GetOrCreateAsync(
                    loaded.IdSociete.Value,
                    cancellationToken);
                activitesSociete = ConfigSocieteDefaults.GetActivitesActives(config).ToList();
            }

            return new AuthentificationResponse
            {
                Success = true,
                Message = "Authentification réussie",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                TokenType = "Bearer",
                ExpiresIn = expirationMinutes * 60,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
                DoitChangerMotDePasse = loaded.DoitChangerMotDePasse == true,
                Utilisateur = new Utilisateur
                {
                    IdUtilisateur = loaded.IdUtilisateur,
                    ReferenceUtilisateur = loaded.ReferenceUtilisateur,
                    NomComplet = loaded.NomComplet,
                    Email = loaded.Email,
                    DefaultUsername = loaded.DefaultUsername,
                    Telephone = loaded.Telephone,
                    PhotoUrl = loaded.PhotoUrl,
                    LieuNaissance = loaded.LieuNaissance,
                    DateNaissance = loaded.DateNaissance,
                    Genre = loaded.Genre,
                    Statut = loaded.Statut,
                    IdAgent = loaded.IdAgent,
                    IdClient = loaded.IdClient,
                    IdSite = loaded.IdSite,
                    DateCreation = loaded.DateCreation,
                    IsConnecte = loaded.IsConnecte,
                    IdSociete = loaded.IdSociete,
                    Societe = loaded.Societe,
                    IdRole = loaded.IdRole,
                    AuthProvider = loaded.AuthProvider,
                    ExternalSubjectId = loaded.ExternalSubjectId,
                    EmailVerified = loaded.EmailVerified
                },
                NomRole = primaryRole?.Nom ?? loaded.Role?.Nom ?? "",
                NomSociete = loaded.Societe?.Nom ?? "",
                AcceptNotification = true,
                Permissions = permissionsList,
                Roles = userRolesList,
                PrimaryRole = primaryRole,
                Client = clientInfo,
                Agent = agentInfo,
                ActivitesSociete = activitesSociete
            };
        }

        private static ClientInfoDto MapClient(Client client) => new()
        {
            IdClient = client.IdClient,
            NomClient = client.NomClient,
            Telephone = client.Telephone,
            EmailClient = client.EmailClient,
            GenreClient = client.GenreClient,
            AdresseClient = client.AdresseClient,
            Statut = client.Statut
        };

        private static AgentInfoDto MapAgent(Agent agent) => new()
        {
            IdAgent = agent.IdAgent,
            Matricule = agent.Matricule,
            NomComplet = agent.NomComplet,
            Genre = agent.Genre,
            DateNaissance = agent.DateNaissance,
            TelephoneAgent = agent.TelephoneAgent,
            EmailAgent = agent.EmailAgent,
            Statut = agent.Statut,
            Fonction = agent.Fonction,
            RoleAgent = agent.RoleAgent,
            PhotoUrl = agent.PhotoUrl,
            IdSociete = agent.IdSociete,
            IdSite = agent.IdSite,
            AdresseResidence = agent.AdresseResidence,
            Zone = agent.Zone
        };
    }
}
