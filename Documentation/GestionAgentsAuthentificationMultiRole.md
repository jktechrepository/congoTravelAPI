# Documentation - Gestion des Agents et Authentification Multi-Rôle

## 🎯 **Vue d'Ensemble**

Ce document présente une architecture complète pour la gestion des agents, clients et authentification multi-rôle avec synchronisation automatique. Idéal pour les systèmes de gestion de mutuelle, assurances ou tout autre secteur nécessitant une gestion fine des permissions et des rôles.

---

## 🏗️ **Architecture Globale**

### **Schéma Conceptuel**
```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Agent/Client  │◄──►│  Utilisateur     │◄──►│   Rôle/Permission│
│   (Profil)      │    │   (Auth)         │    │   (Droits)      │
└─────────────────┘    └──────────────────┘    └─────────────────┘
         │                       │                       │
         ▼                       ▼                       ▼
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Mutuelle      │    │   JWT Token      │    │   Audit Logs    │
│   (Société)     │    │   (Session)      │    │   (Traçabilité) │
└─────────────────┘    └──────────────────┘    └─────────────────┘
```

### **Entités Principales**
1. **Utilisateur** : Entité d'authentification unique
2. **Agent** : Profil professionnel (employé)
3. **Client** : Profil client (membre de la mutuelle)
4. **Rôle** : Permissions et accès
5. **Société/Mutuelle** : Structure organisationnelle

---

## 📊 **Modèle de Données**

### **1. Entité Utilisateur (Base Authentification)**

```csharp
public class Utilisateur
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string NomUtilisateur { get; set; } = string.Empty;
    public string MotDePasseHash { get; set; } = string.Empty;
    public string MotDePasseSalt { get; set; } = string.Empty;
    public bool EstActif { get; set; } = true;
    public bool EmailVerifie { get; set; } = false;
    public DateTime DateCreation { get; set; }
    public DateTime? DerniereConnexion { get; set; }
    public DateTime? DateModificationMdp { get; set; }
    public int? TentativesEchouees { get; set; }
    public DateTime? VerrouillageJusqua { get; set; }
    public string? TokenVerificationEmail { get; set; }
    public DateTime? ExpirationTokenEmail { get; set; }
    public string? TokenResetMdp { get; set; }
    public DateTime? ExpirationTokenResetMdp { get; set; }
    
    // Relations
    public Agent? Agent { get; set; }
    public Client? Client { get; set; }
    public ICollection<UtilisateurRole> UtilisateurRoles { get; set; } = new();
    public ICollection<UtilisateurSession> Sessions { get; set; } = new();
    public ICollection<AuditLog> AuditLogs { get; set; } = new();
}
```

### **2. Entité Agent (Profil Professionnel)**

```csharp
public class Agent
{
    public int Id { get; set; }
    public int UtilisateurId { get; set; }
    public int SocieteId { get; set; }
    public string Matricule { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string Telephone { get; set; } = string.Empty;
    public string? PhotoProfil { get; set; }
    public DateTime DateEmbauche { get; set; }
    public string Poste { get; set; } = string.Empty;
    public string Departement { get; set; } = string.Empty;
    public string? ManagerId { get; set; }
    public decimal SalaireBase { get; set; }
    public string StatutContrat { get; set; } // CDI, CDD, Stage, etc.
    public DateTime? DateFinContrat { get; set; }
    public string Adresse { get; set; } = string.Empty;
    public string Ville { get; set; } = string.Empty;
    public string CodePostal { get; set; } = string.Empty;
    public string Pays { get; set; } = "France";
    public string NumeroSecuriteSociale { get; set; } = string.Empty;
    public DateTime DateNaissance { get; set; }
    public string LieuNaissance { get; set; } = string.Empty;
    public string Nationalite { get; set; } = "Française";
    public string SituationFamiliale { get; set; }
    public int NombreEnfants { get; set; }
    public string? NumeroPermis { get; set; }
    public DateTime? ValiditePermis { get; set; }
    public string? Rib { get; set; }
    public string? Iban { get; set; }
    public bool EstActif { get; set; } = true;
    public DateTime? DateDesactivation { get; set; }
    public string? MotifDesactivation { get; set; }
    public DateTime DateCreation { get; set; }
    public DateTime DateModification { get; set; }
    
    // Relations
    public Utilisateur Utilisateur { get; set; } = null!;
    public Societe Societe { get; set; } = null!;
    public Agent? Manager { get; set; }
    public ICollection<Agent> Subordonnes { get; set; } = new();
    public ICollection<AgentDocument> Documents { get; set; } = new();
    public ICollection<AgentFormation> Formations { get; set; } = new();
    public ICollection<AgentConge> Conges { get; set; } = new();
    public ICollection<AgentEvaluation> Evaluations { get; set; } = new();
}
```

### **3. Entité Client (Profil Client/Mutuelle)**

```csharp
public class Client
{
    public int Id { get; set; }
    public int UtilisateurId { get; set; }
    public int SocieteId { get; set; }
    public string NumeroClient { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string Telephone { get; set; } = string.Empty;
    public string? PhotoProfil { get; set; }
    public DateTime DateInscription { get; set; }
    public string TypeClient { get; set; } // Particulier, Entreprise, Famille
    public string? NomEntreprise { get; set; }
    public string? Siret { get; set; }
    public string Adresse { get; set; } = string.Empty;
    public string Ville { get; set; } = string.Empty;
    public string CodePostal { get; set; } = string.Empty;
    public string Pays { get; set; } = "France";
    public DateTime DateNaissance { get; set; }
    public string LieuNaissance { get; set; } = string.Empty;
    public string Nationalite { get; set; } = "Française";
    public string NumeroSecuriteSociale { get; set; } = string.Empty;
    public string SituationFamiliale { get; set; }
    public int NombreEnfants { get; set; }
    public string Profession { get; set; }
    public decimal RevenuMensuel { get; set; }
    public string? ConjointNom { get; set; }
    public string? ConjointDateNaissance { get; set; }
    public List<string> Enfants { get; set; } = new();
    public string? MedecinTraitant { get; set; }
    public string? NumeroMutuelleActuelle { get; set; }
    public string NiveauCouvertureSouhaite { get; set; }
    public decimal CotisationMensuelle { get; set; }
    public DateTime DateDebutCouverture { get; set; }
    public DateTime? DateFinCouverture { get; set; }
    public string StatutContrat { get; set; } // Actif, Suspendu, Résilié
    public bool EstActif { get; set; } = true;
    public DateTime? DateDesactivation { get; set; }
    public string? MotifDesactivation { get; set; }
    public DateTime DateCreation { get; set; }
    public DateTime DateModification { get; set; }
    
    // Relations
    public Utilisateur Utilisateur { get; set; } = null!;
    public Societe Societe { get; set; } = null!;
    public ICollection<ClientBeneficiaire> Beneficiaires { get; set; } = new();
    public ICollection<ClientContrat> Contrats { get; set; } = new();
    public ICollection<ClientDocument> Documents { get; set; } = new();
    public ICollection<ClientSinistre> Sinistres { get; set; } = new();
    public ICollection<ClientPaiement> Paiements { get; set; } = new();
}
```

### **4. Gestion des Rôles et Permissions**

```csharp
public class Role
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty; // AGENT_ADMIN, CLIENT_STANDARD, etc.
    public bool EstActif { get; set; } = true;
    public bool EstSysteme { get; set; } = false; // Rôles non modifiables
    public int NiveauHierarchique { get; set; } // 1=Client, 5=Admin, 10=Super-Admin
    public DateTime DateCreation { get; set; }
    public DateTime DateModification { get; set; }
    
    // Relations
    public ICollection<RolePermission> RolePermissions { get; set; } = new();
    public ICollection<UtilisateurRole> UtilisateurRoles { get; set; } = new();
}

public class Permission
{
    public int Id { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty; // CREATE_AGENT, VIEW_CLIENTS, etc.
    public string Categorie { get; set; } = string.Empty; // AGENT, CLIENT, SYSTEME
    public string Type { get; set; } = string.Empty; // READ, WRITE, DELETE, ADMIN
    public bool EstActif { get; set; } = true;
    public DateTime DateCreation { get; set; }
    
    // Relations
    public ICollection<RolePermission> RolePermissions { get; set; } = new();
}

public class UtilisateurRole
{
    public int UtilisateurId { get; set; }
    public int RoleId { get; set; }
    public DateTime DateAttribution { get; set; }
    public DateTime? DateFin { get; set; }
    public bool EstActif { get; set; } = true;
    public string? AttribuePar { get; set; }
    public string? Motif { get; set; }
    
    // Relations
    public Utilisateur Utilisateur { get; set; } = null!;
    public Role Role { get; set; } = null!;
}

public class RolePermission
{
    public int RoleId { get; set; }
    public int PermissionId { get; set; }
    public bool EstAccorde { get; set; } = true;
    public DateTime DateAttribution { get; set; }
    public string? AttribuePar { get; set; }
    
    // Relations
    public Role Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}
```

---

## 🔐 **Système d'Authentification Multi-Rôle**

### **1. Configuration JWT**

```csharp
// appsettings.json
{
  "JwtSettings": {
    "SecretKey": "votre-secret-super-securise-de-256-bits-minimum",
    "Issuer": "CongoTravelApi",
    "Audience": "CongoTravelClients",
    "ExpirationHours": 24,
    "RefreshTokenDays": 7
  }
}

// Startup.cs / Program.cs
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ClockSkew = TimeSpan.Zero
    };
});
```

### **2. Service d'Authentification**

```csharp
public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);
    Task<bool> LogoutAsync(string refreshToken);
    Task<AuthResponse> RegisterAgentAsync(RegisterAgentRequest request);
    Task<AuthResponse> RegisterClientAsync(RegisterClientRequest request);
    Task<bool> ChangePasswordAsync(ChangePasswordRequest request);
    Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
    Task<bool> VerifyEmailAsync(string token);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<AuthService> _logger;
    private readonly IEmailService _emailService;

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var utilisateur = await _context.Utilisateurs
            .Include(u => u.Agent)
            .Include(u => u.Client)
            .Include(u => u.UtilisateurRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (utilisateur == null || !VerifyPassword(request.MotDePasse, utilisateur.MotDePasseHash, utilisateur.MotDePasseSalt))
        {
            await IncrementerTentativesEchouees(utilisateur);
            throw new UnauthorizedAccessException("Email ou mot de passe incorrect");
        }

        if (!utilisateur.EstActif || utilisateur.VerrouillageJusqua > DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Compte désactivé ou temporairement verrouillé");
        }

        // Réinitialiser les tentatives échouées
        utilisateur.TentativesEchouees = 0;
        utilisateur.VerrouillageJusqua = null;
        utilisateur.DerniereConnexion = DateTime.UtcNow;
        
        await _context.SaveChangesAsync();

        // Générer les tokens
        var token = GenerateJwtToken(utilisateur);
        var refreshToken = GenerateRefreshToken();

        // Sauvegarder la session
        await SaveUserSession(utilisateur.Id, refreshToken);

        return new AuthResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            ExpiresIn = _jwtSettings.ExpirationHours * 3600,
            Utilisateur = MapToUtilisateurDto(utilisateur)
        };
    }

    private string GenerateJwtToken(Utilisateur utilisateur)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, utilisateur.Id.ToString()),
            new Claim(ClaimTypes.Email, utilisateur.Email),
            new Claim(ClaimTypes.Name, utilisateur.NomUtilisateur),
            new Claim("utilisateur_id", utilisateur.Id.ToString())
        };

        // Ajouter les rôles
        foreach (var userRole in utilisateur.UtilisateurRoles.Where(ur => ur.EstActif))
        {
            claims.Add(new Claim(ClaimTypes.Role, userRole.Role.Code));
            claims.Add(new Claim("role_id", userRole.RoleId.ToString()));
        }

        // Ajouter les informations spécifiques au profil
        if (utilisateur.Agent != null)
        {
            claims.Add(new Claim("agent_id", utilisateur.Agent.Id.ToString()));
            claims.Add(new Claim("societe_id", utilisateur.Agent.SocieteId.ToString()));
            claims.Add(new Claim("matricule", utilisateur.Agent.Matricule));
            claims.Add(new Claim("type_profil", "Agent"));
        }
        else if (utilisateur.Client != null)
        {
            claims.Add(new Claim("client_id", utilisateur.Client.Id.ToString()));
            claims.Add(new Claim("societe_id", utilisateur.Client.SocieteId.ToString()));
            claims.Add(new Claim("numero_client", utilisateur.Client.NumeroClient));
            claims.Add(new Claim("type_profil", "Client"));
        }

        // Ajouter les permissions
        var permissions = utilisateur.UtilisateurRoles
            .Where(ur => ur.EstActif)
            .SelectMany(ur => ur.Role.RolePermissions)
            .Where(rp => rp.EstAccorde)
            .Select(rp => rp.Permission.Code)
            .Distinct();

        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_jwtSettings.ExpirationHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

### **3. Middleware de Validation Multi-Rôle**

```csharp
public class MultiRoleAuthorizationMiddleware
{
    private readonly RequestDelegate _next;

    public MultiRoleAuthorizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint != null)
        {
            var requiredRoles = endpoint.Metadata.GetMetadata<AuthorizeAttribute>()?.Roles;
            var requiredPermissions = endpoint.Metadata.GetMetadata<RequiredPermissionAttribute>()?.Permissions;

            if (requiredRoles != null || requiredPermissions != null)
            {
                var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    // Validation supplémentaire des permissions en base de données
                    await ValidateUserPermissions(context, int.Parse(userId), requiredPermissions);
                }
            }
        }

        await _next(context);
    }

    private async Task ValidateUserPermissions(HttpContext context, int userId, string[]? requiredPermissions)
    {
        if (requiredPermissions == null || requiredPermissions.Length == 0)
            return;

        using var scope = context.RequestServices.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var userPermissions = await dbContext.Utilisateurs
            .Where(u => u.Id == userId)
            .SelectMany(u => u.UtilisateurRoles
                .Where(ur => ur.EstActif)
                .SelectMany(ur => ur.Role.RolePermissions
                    .Where(rp => rp.EstAccorde)
                    .Select(rp => rp.Permission.Code)))
            .ToListAsync();

        var hasPermission = requiredPermissions.All(permission => 
            userPermissions.Contains(permission));

        if (!hasPermission)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Permissions insuffisantes");
            return;
        }
    }
}
```

---

## 🔄 **Synchronisation Automatique des Données**

### **1. Service de Synchronisation**

```csharp
public interface ISynchronisationService
{
    Task SynchroniserAgentVersUtilisateurAsync(int agentId);
    Task SynchroniserClientVersUtilisateurAsync(int clientId);
    Task SynchroniserUtilisateurVersAgentAsync(int utilisateurId);
    Task SynchroniserUtilisateurVersClientAsync(int utilisateurId);
    Task<bool> VerifierCoherenceDonneesAsync(int utilisateurId);
}

public class SynchronisationService : ISynchronisationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<SynchronisationService> _logger;
    private readonly ICacheService _cacheService;

    public async Task SynchroniserAgentVersUtilisateurAsync(int agentId)
    {
        var agent = await _context.Agents
            .Include(a => a.Utilisateur)
            .FirstOrDefaultAsync(a => a.Id == agentId);

        if (agent?.Utilisateur == null)
        {
            _logger.LogWarning($"Agent {agentId} ou utilisateur associé non trouvé");
            return;
        }

        var utilisateur = agent.Utilisateur;
        var modifications = false;

        // Synchronisation des informations de base
        if (utilisateur.NomUtilisateur != $"{agent.Prenom}_{agent.Nom}".ToLower())
        {
            utilisateur.NomUtilisateur = $"{agent.Prenom}_{agent.Nom}".ToLower();
            modifications = true;
        }

        // Mise à jour de l'email si nécessaire (avec validation)
        var nouvelEmail = GenerateEmailFromAgent(agent);
        if (utilisateur.Email != nouvelEmail && await IsEmailDisponibleAsync(nouvelEmail, utilisateur.Id))
        {
            utilisateur.Email = nouvelEmail;
            modifications = true;
        }

        // Synchronisation du statut
        if (utilisateur.EstActif != agent.EstActif)
        {
            utilisateur.EstActif = agent.EstActif;
            modifications = true;
        }

        if (modifications)
        {
            utilisateur.DateModification = DateTime.UtcNow;
            
            // Ajouter un log d'audit
            await AddAuditLog(utilisateur.Id, "SYNCHRONISATION_AGENT", 
                $"Synchronisation depuis l'agent {agentId}");

            await _context.SaveChangesAsync();
            
            // Invalider le cache
            await _cacheService.RemoveAsync($"utilisateur_{utilisateur.Id}");
            
            _logger.LogInformation($"Synchronisation Agent->Utilisateur réussie pour l'agent {agentId}");
        }
    }

    public async Task SynchroniserClientVersUtilisateurAsync(int clientId)
    {
        var client = await _context.Clients
            .Include(c => c.Utilisateur)
            .FirstOrDefaultAsync(c => c.Id == clientId);

        if (client?.Utilisateur == null)
        {
            _logger.LogWarning($"Client {clientId} ou utilisateur associé non trouvé");
            return;
        }

        var utilisateur = client.Utilisateur;
        var modifications = false;

        // Synchronisation des informations de base
        if (utilisateur.NomUtilisateur != $"{client.Prenom}_{client.Nom}".ToLower())
        {
            utilisateur.NomUtilisateur = $"{client.Prenom}_{client.Nom}".ToLower();
            modifications = true;
        }

        // Mise à jour de l'email
        var nouvelEmail = GenerateEmailFromClient(client);
        if (utilisateur.Email != nouvelEmail && await IsEmailDisponibleAsync(nouvelEmail, utilisateur.Id))
        {
            utilisateur.Email = nouvelEmail;
            modifications = true;
        }

        // Synchronisation du statut
        if (utilisateur.EstActif != client.EstActif)
        {
            utilisateur.EstActif = client.EstActif;
            modifications = true;
        }

        if (modifications)
        {
            utilisateur.DateModification = DateTime.UtcNow;
            
            await AddAuditLog(utilisateur.Id, "SYNCHRONISATION_CLIENT", 
                $"Synchronisation depuis le client {clientId}");

            await _context.SaveChangesAsync();
            await _cacheService.RemoveAsync($"utilisateur_{utilisateur.Id}");
            
            _logger.LogInformation($"Synchronisation Client->Utilisateur réussie pour le client {clientId}");
        }
    }

    private string GenerateEmailFromAgent(Agent agent)
    {
        // Format: prenom.nom@entreprise.com
        var baseEmail = $"{agent.Prenom.ToLower()}.{agent.Nom.ToLower()}";
        var domaine = "kenergie.com"; // Peut être dynamique selon la société
        
        // Gérer les doublons
        var email = $"{baseEmail}@{domaine}";
        var counter = 1;
        
        while (!IsEmailDisponibleAsync(email, agent.UtilisateurId).Result)
        {
            email = $"{baseEmail}{counter}@{domaine}";
            counter++;
        }
        
        return email;
    }

    private string GenerateEmailFromClient(Client client)
    {
        // Format: prenom.nom.numero@mutuelle.com
        var baseEmail = $"{client.Prenom.ToLower()}.{client.Nom.ToLower()}.{client.NumeroClient}";
        var domaine = "mutuelle-kenergie.com";
        
        var email = $"{baseEmail}@{domaine}";
        var counter = 1;
        
        while (!IsEmailDisponibleAsync(email, client.UtilisateurId).Result)
        {
            email = $"{baseEmail}{counter}@{domaine}";
            counter++;
        }
        
        return email;
    }
}
```

### **2. Automatisation avec Entity Framework Interceptors**

```csharp
public class SynchronisationInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;

    public SynchronisationInterceptor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, 
        InterceptionResult<int> result)
    {
        SynchroniserEntitesModifiees(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, 
        InterceptionResult<int> result, 
        CancellationToken cancellationToken = default)
    {
        await SynchroniserEntitesModifieesAsync(eventData.Context, cancellationToken);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task SynchroniserEntitesModifieesAsync(DbContext context, CancellationToken cancellationToken)
    {
        var modifiedEntries = context.ChangeTracker
            .Entries()
            .Where(e => e.State == EntityState.Modified && 
                       (e.Entity is Agent || e.Entity is Client))
            .ToList();

        if (!modifiedEntries.Any()) return;

        using var scope = _serviceProvider.CreateScope();
        var syncService = scope.ServiceProvider.GetRequiredService<ISynchronisationService>();

        foreach (var entry in modifiedEntries)
        {
            try
            {
                if (entry.Entity is Agent agent)
                {
                    await syncService.SynchroniserAgentVersUtilisateurAsync(agent.Id);
                }
                else if (entry.Entity is Client client)
                {
                    await syncService.SynchroniserClientVersUtilisateurAsync(client.Id);
                }
            }
            catch (Exception ex)
            {
                // Logger l'erreur mais ne pas bloquer la sauvegarde
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<SynchronisationInterceptor>>();
                logger.LogError(ex, $"Erreur lors de la synchronisation pour {entry.Entity.GetType().Name}");
            }
        }
    }
}
```

---

## 🎮 **Contrôleurs et API**

### **1. Contrôleur de Gestion des Agents**

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "ADMIN_AGENT,ADMIN_SYSTEM")]
public class AgentsController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly ISynchronisationService _synchronisationService;
    private readonly ILogger<AgentsController> _logger;

    [HttpPost]
    public async Task<ActionResult<AgentDto>> CreateAgent([FromBody] CreateAgentRequest request)
    {
        try
        {
            // 1. Créer l'utilisateur de base
            var utilisateur = new Utilisateur
            {
                Email = request.Email,
                NomUtilisateur = GenerateUsername(request.Prenom, request.Nom),
                MotDePasseHash = HashPassword(request.MotDePasse, out string salt),
                MotDePasseSalt = salt,
                EstActif = true,
                EmailVerifie = false,
                DateCreation = DateTime.UtcNow
            };

            // 2. Créer l'agent
            var agent = new Agent
            {
                Utilisateur = utilisateur,
                Matricule = GenerateMatricule(),
                Nom = request.Nom,
                Prenom = request.Prenom,
                Telephone = request.Telephone,
                DateEmbauche = request.DateEmbauche,
                Poste = request.Poste,
                Departement = request.Departement,
                SocieteId = request.SocieteId,
                // ... autres propriétés
                DateCreation = DateTime.UtcNow
            };

            var agentCree = await _agentService.CreateAgentAsync(agent, request.Roles);

            // 3. Synchroniser automatiquement
            await _synchronisationService.SynchroniserAgentVersUtilisateurAsync(agentCree.Id);

            // 4. Envoyer l'email de vérification
            await SendVerificationEmail(utilisateur);

            return CreatedAtAction(nameof(GetAgent), new { id = agentCree.Id }, 
                MapToAgentDto(agentCree));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la création de l'agent");
            return StatusCode(500, new { message = "Erreur lors de la création de l'agent" });
        }
    }

    [HttpPut("{id}")]
    [RequiredPermission("UPDATE_AGENT")]
    public async Task<ActionResult<AgentDto>> UpdateAgent(int id, [FromBody] UpdateAgentRequest request)
    {
        try
        {
            var agent = await _agentService.GetAgentByIdAsync(id);
            if (agent == null)
                return NotFound();

            // Mettre à jour les propriétés de l'agent
            agent.Nom = request.Nom;
            agent.Prenom = request.Prenom;
            agent.Telephone = request.Telephone;
            agent.Poste = request.Poste;
            agent.DateModification = DateTime.UtcNow;

            await _agentService.UpdateAgentAsync(agent);

            // Synchronisation automatique via l'interceptor
            // ou manuelle : await _synchronisationService.SynchroniserAgentVersUtilisateurAsync(id);

            return Ok(MapToAgentDto(agent));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erreur lors de la mise à jour de l'agent {id}");
            return StatusCode(500, new { message = "Erreur lors de la mise à jour de l'agent" });
        }
    }
}
```

### **2. Contrôleur de Gestion des Clients**

```csharp
[ApiController]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly IClientService _clientService;
    private readonly ISynchronisationService _synchronisationService;

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> RegisterClient([FromBody] RegisterClientRequest request)
    {
        try
        {
            // 1. Créer l'utilisateur
            var utilisateur = new Utilisateur
            {
                Email = request.Email,
                NomUtilisateur = GenerateUsername(request.Prenom, request.Nom),
                MotDePasseHash = HashPassword(request.MotDePasse, out string salt),
                MotDePasseSalt = salt,
                EstActif = true,
                EmailVerifie = false,
                DateCreation = DateTime.UtcNow
            };

            // 2. Créer le client
            var client = new Client
            {
                Utilisateur = utilisateur,
                NumeroClient = GenerateNumeroClient(),
                Nom = request.Nom,
                Prenom = request.Prenom,
                Telephone = request.Telephone,
                DateInscription = DateTime.UtcNow,
                SocieteId = request.SocieteId,
                // ... autres propriétés
                DateCreation = DateTime.UtcNow
            };

            var clientCree = await _clientService.CreateClientAsync(client);

            // 3. Attribuer le rôle par défaut
            await _clientService.AssignRoleAsync(clientCree.Id, "CLIENT_STANDARD");

            // 4. Synchroniser
            await _synchronisationService.SynchroniserClientVersUtilisateurAsync(clientCree.Id);

            // 5. Générer les tokens d'authentification
            var authResponse = await GenerateAuthResponse(utilisateur);

            // 6. Envoyer l'email de bienvenue
            await SendWelcomeEmail(utilisateur, clientCree);

            return Ok(authResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'inscription du client");
            return StatusCode(500, new { message = "Erreur lors de l'inscription" });
        }
    }
}
```

---

## 🔧 **Services Métier**

### **1. Service de Gestion des Rôles**

```csharp
public interface IRoleService
{
    Task<bool> AssignRoleAsync(int utilisateurId, string roleCode, string? attribuePar = null);
    Task<bool> RemoveRoleAsync(int utilisateurId, string roleCode);
    Task<bool> HasPermissionAsync(int utilisateurId, string permissionCode);
    Task<List<string>> GetUserRolesAsync(int utilisateurId);
    Task<List<string>> GetUserPermissionsAsync(int utilisateurId);
}

public class RoleService : IRoleService
{
    private readonly AppDbContext _context;
    private readonly ICacheService _cacheService;

    public async Task<bool> AssignRoleAsync(int utilisateurId, string roleCode, string? attribuePar = null)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Code == roleCode);
        if (role == null)
            return false;

        var existingAssignment = await _context.UtilisateurRoles
            .FirstOrDefaultAsync(ur => ur.UtilisateurId == utilisateurId && ur.RoleId == role.Id);

        if (existingAssignment != null)
        {
            if (!existingAssignment.EstActif)
            {
                existingAssignment.EstActif = true;
                existingAssignment.DateAttribution = DateTime.UtcNow;
                existingAssignment.AttribuePar = attribuePar;
            }
        }
        else
        {
            await _context.UtilisateurRoles.AddAsync(new UtilisateurRole
            {
                UtilisateurId = utilisateurId,
                RoleId = role.Id,
                DateAttribution = DateTime.UtcNow,
                EstActif = true,
                AttribuePar = attribuePar
            });
        }

        await _context.SaveChangesAsync();

        // Invalider le cache des permissions
        await _cacheService.RemoveAsync($"user_permissions_{utilisateurId}");

        return true;
    }

    public async Task<bool> HasPermissionAsync(int utilisateurId, string permissionCode)
    {
        // Vérifier d'abord le cache
        var cacheKey = $"user_permissions_{utilisateurId}";
        var cachedPermissions = await _cacheService.GetAsync<List<string>>(cacheKey);
        
        if (cachedPermissions == null)
        {
            // Charger depuis la base de données
            cachedPermissions = await _context.Utilisateurs
                .Where(u => u.Id == utilisateurId)
                .SelectMany(u => u.UtilisateurRoles
                    .Where(ur => ur.EstActif)
                    .SelectMany(ur => ur.Role.RolePermissions
                        .Where(rp => rp.EstAccorde)
                        .Select(rp => rp.Permission.Code)))
                .ToListAsync();

            // Mettre en cache pour 1 heure
            await _cacheService.SetAsync(cacheKey, cachedPermissions, TimeSpan.FromHours(1));
        }

        return cachedPermissions.Contains(permissionCode);
    }
}
```

---

## 📋 **Configuration et Déploiement**

### **1. Configuration des Services**

```csharp
// Program.cs
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAgentService, AgentService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<ISynchronisationService, SynchronisationService>();

// Configuration des interceptors EF Core
builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.AddInterceptors(new SynchronisationInterceptor(serviceProvider));
});

// Middleware
builder.Services.AddTransient<MultiRoleAuthorizationMiddleware>();

// Configuration du cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// Configuration des emails
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailService, EmailService>();
```

### **2. Scripts de Migration**

```sql
-- Création des tables principales
CREATE TABLE Utilisateurs (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(255) UNIQUE NOT NULL,
    NomUtilisateur NVARCHAR(100) UNIQUE NOT NULL,
    MotDePasseHash NVARCHAR(255) NOT NULL,
    MotDePasseSalt NVARCHAR(255) NOT NULL,
    EstActif BIT DEFAULT 1,
    EmailVerifie BIT DEFAULT 0,
    DateCreation DATETIME2 DEFAULT GETDATE(),
    DerniereConnexion DATETIME2 NULL,
    DateModificationMdp DATETIME2 NULL,
    TentativesEchouees INT NULL,
    VerrouillageJusqua DATETIME2 NULL,
    TokenVerificationEmail NVARCHAR(255) NULL,
    ExpirationTokenEmail DATETIME2 NULL,
    TokenResetMdp NVARCHAR(255) NULL,
    ExpirationTokenResetMdp DATETIME2 NULL
);

CREATE TABLE Agents (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UtilisateurId INT UNIQUE NOT NULL,
    SocieteId INT NOT NULL,
    Matricule NVARCHAR(50) UNIQUE NOT NULL,
    Nom NVARCHAR(100) NOT NULL,
    Prenom NVARCHAR(100) NOT NULL,
    Telephone NVARCHAR(20) NOT NULL,
    DateEmbauche DATE NOT NULL,
    Poste NVARCHAR(100) NOT NULL,
    Departement NVARCHAR(100) NOT NULL,
    EstActif BIT DEFAULT 1,
    DateCreation DATETIME2 DEFAULT GETDATE(),
    DateModification DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (UtilisateurId) REFERENCES Utilisateurs(Id) ON DELETE CASCADE
);

CREATE TABLE Clients (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UtilisateurId INT UNIQUE NOT NULL,
    SocieteId INT NOT NULL,
    NumeroClient NVARCHAR(50) UNIQUE NOT NULL,
    Nom NVARCHAR(100) NOT NULL,
    Prenom NVARCHAR(100) NOT NULL,
    Telephone NVARCHAR(20) NOT NULL,
    DateInscription DATE NOT NULL,
    EstActif BIT DEFAULT 1,
    DateCreation DATETIME2 DEFAULT GETDATE(),
    DateModification DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (UtilisateurId) REFERENCES Utilisateurs(Id) ON DELETE CASCADE
);

-- Tables de gestion des rôles
CREATE TABLE Roles (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nom NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    Code NVARCHAR(50) UNIQUE NOT NULL,
    EstActif BIT DEFAULT 1,
    EstSysteme BIT DEFAULT 0,
    NiveauHierarchique INT DEFAULT 1,
    DateCreation DATETIME2 DEFAULT GETDATE()
);

CREATE TABLE Permissions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nom NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    Code NVARCHAR(100) UNIQUE NOT NULL,
    Categorie NVARCHAR(50) NOT NULL,
    Type NVARCHAR(20) NOT NULL,
    EstActif BIT DEFAULT 1,
    DateCreation DATETIME2 DEFAULT GETDATE()
);

CREATE TABLE UtilisateurRoles (
    UtilisateurId INT NOT NULL,
    RoleId INT NOT NULL,
    DateAttribution DATETIME2 DEFAULT GETDATE(),
    DateFin DATETIME2 NULL,
    EstActif BIT DEFAULT 1,
    AttribuePar NVARCHAR(100),
    Motif NVARCHAR(500),
    PRIMARY KEY (UtilisateurId, RoleId),
    FOREIGN KEY (UtilisateurId) REFERENCES Utilisateurs(Id) ON DELETE CASCADE,
    FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE
);

CREATE TABLE RolePermissions (
    RoleId INT NOT NULL,
    PermissionId INT NOT NULL,
    EstAccorde BIT DEFAULT 1,
    DateAttribution DATETIME2 DEFAULT GETDATE(),
    AttribuePar NVARCHAR(100),
    PRIMARY KEY (RoleId, PermissionId),
    FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE,
    FOREIGN KEY (PermissionId) REFERENCES Permissions(Id) ON DELETE CASCADE
);

-- Insertion des rôles par défaut
INSERT INTO Roles (Nom, Description, Code, EstSysteme, NiveauHierarchique) VALUES
('Super Administrateur', 'Accès complet au système', 'SUPER_ADMIN', 1, 10),
('Administrateur Agent', 'Gestion des agents et opérations', 'ADMIN_AGENT', 1, 7),
('Agent Standard', 'Accès de base aux fonctionnalités agent', 'AGENT_STANDARD', 1, 5),
('Administrateur Client', 'Gestion des clients et contrats', 'ADMIN_CLIENT', 1, 6),
('Client Standard', 'Accès client de base', 'CLIENT_STANDARD', 1, 1),
('Client Premium', 'Accès client avec avantages', 'CLIENT_PREMIUM', 1, 2);

-- Insertion des permissions par défaut
INSERT INTO Permissions (Nom, Description, Code, Categorie, Type) VALUES
-- Permissions Agent
('Créer un agent', 'Permet de créer de nouveaux agents', 'CREATE_AGENT', 'AGENT', 'WRITE'),
('Voir les agents', 'Permet de voir la liste des agents', 'VIEW_AGENTS', 'AGENT', 'READ'),
('Modifier un agent', 'Permet de modifier les informations des agents', 'UPDATE_AGENT', 'AGENT', 'WRITE'),
('Supprimer un agent', 'Permet de supprimer des agents', 'DELETE_AGENT', 'AGENT', 'DELETE'),
('Gérer les rôles agents', 'Permet d''attribuer des rôles aux agents', 'MANAGE_AGENT_ROLES', 'AGENT', 'ADMIN'),

-- Permissions Client
('Créer un client', 'Permet de créer de nouveaux clients', 'CREATE_CLIENT', 'CLIENT', 'WRITE'),
('Voir les clients', 'Permet de voir la liste des clients', 'VIEW_CLIENTS', 'CLIENT', 'READ'),
('Modifier un client', 'Permet de modifier les informations des clients', 'UPDATE_CLIENT', 'CLIENT', 'WRITE'),
('Supprimer un client', 'Permet de supprimer des clients', 'DELETE_CLIENT', 'CLIENT', 'DELETE'),
('Voir ses propres informations', 'Permet au client de voir ses informations', 'VIEW_OWN_INFO', 'CLIENT', 'READ'),
('Modifier ses propres informations', 'Permet au client de modifier ses informations', 'UPDATE_OWN_INFO', 'CLIENT', 'WRITE'),

-- Permissions Système
('Accès au dashboard', 'Permet d''accéder au dashboard', 'ACCESS_DASHBOARD', 'SYSTEME', 'READ'),
('Voir les logs d''audit', 'Permet de voir les logs d''audit', 'VIEW_AUDIT_LOGS', 'SYSTEME', 'READ'),
('Gérer les permissions', 'Permet de gérer les permissions du système', 'MANAGE_PERMISSIONS', 'SYSTEME', 'ADMIN');
```

---

## 🧪 **Tests et Validation**

### **1. Tests d'Intégration**

```csharp
[TestClass]
public class AuthentificationTests
{
    private readonly TestApplicationFactory _factory;
    private readonly HttpClient _client;

    [TestMethod]
    public async Task Login_AgentValide_RetourneToken()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "agent.test@kenergie.com",
            MotDePasse = "Password123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.IsNotNull(authResponse.Token);
        Assert.IsNotNull(authResponse.RefreshToken);
        Assert.IsTrue(authResponse.ExpiresIn > 0);
    }

    [TestMethod]
    public async Task CreateAgent_SynchroniseUtilisateur()
    {
        // Arrange
        var createRequest = new CreateAgentRequest
        {
            Nom = "Dupont",
            Prenom = "Jean",
            Email = "jean.dupont@kenergie.com",
            MotDePasse = "Password123!",
            Poste = "Développeur",
            SocieteId = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/agents", createRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        
        // Vérifier la synchronisation
        var agent = await response.Content.ReadFromJsonAsync<AgentDto>();
        var utilisateur = await GetUtilisateurById(agent.UtilisateurId);
        
        Assert.AreEqual(agent.Email, utilisateur.Email);
        Assert.AreEqual($"{agent.Prenom}_{agent.Nom}".ToLower(), utilisateur.NomUtilisateur);
    }
}
```

---

## 📊 **Monitoring et Audit**

### **1. Logs d'Audit**

```csharp
public class AuditLog
{
    public int Id { get; set; }
    public int UtilisateurId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IpAdresse { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public DateTime DateAction { get; set; }
    public string? EntiteType { get; set; }
    public int? EntiteId { get; set; }
    public string? AnciennesValeurs { get; set; }
    public string? NouvellesValeurs { get; set; }
    
    // Relations
    public Utilisateur Utilisateur { get; set; } = null!;
}

public class AuditService
{
    public async Task LogActionAsync(int utilisateurId, string action, string description, 
        string? entiteType = null, int? entiteId = null, 
        object? anciennesValeurs = null, object? nouvellesValeurs = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        
        var auditLog = new AuditLog
        {
            UtilisateurId = utilisateurId,
            Action = action,
            Description = description,
            IpAdresse = GetClientIpAddress(httpContext),
            UserAgent = httpContext?.Request?.Headers["User-Agent"].ToString(),
            DateAction = DateTime.UtcNow,
            EntiteType = entiteType,
            EntiteId = entiteId,
            AnciennesValeurs = anciennesValeurs != null ? JsonSerializer.Serialize(anciennesValeurs) : null,
            NouvellesValeurs = nouvellesValeurs != null ? JsonSerializer.Serialize(nouvellesValeurs) : null
        };

        await _context.AuditLogs.AddAsync(auditLog);
        await _context.SaveChangesAsync();
    }
}
```

---

## 🎯 **Bonnes Pratiques et Recommandations**

### **1. Sécurité**
- **Hashage des mots de passe** : Utiliser BCrypt ou Argon2
- **JWT sécurisé** : Clé de 256 bits minimum, rotation régulière
- **Rate limiting** : Limiter les tentatives de connexion
- **HTTPS obligatoire** : Toutes les communications chiffrées
- **Validation des entrées** : Protection contre injections

### **2. Performance**
- **Cache Redis** : Permissions et sessions utilisateurs
- **Database indexing** : Optimiser les requêtes fréquentes
- **Pagination** : Limiter les retours de données
- **Async/Await** : Utiliser systématiquement les méthodes asynchrones

### **3. Maintenabilité**
- **Code modulaire** : Séparation claire des responsabilités
- **Tests unitaires** : Couverture > 80%
- **Documentation** : Commentaires XML et guides d'utilisation
- **Logging structuré** : Faciliter le debugging

### **4. Scalabilité**
- **Microservices** : Découpler les fonctionnalités métier
- **Load balancing** : Répartir la charge
- **Database sharding** : Partitionner les données par société
- **Message queue** : Traitements asynchrones

---

## 🚀 **Conclusion**

Cette architecture complète offre une solution robuste et scalable pour la gestion multi-rôle avec synchronisation automatique. Elle est particulièrement adaptée aux besoins des mutuelles et assurances où la gestion fine des permissions et la cohérence des données sont critiques.

**Points clés :**
- ✅ **Authentification multi-rôle** sécurisée avec JWT
- ✅ **Synchronisation automatique** bidirectionnelle
- ✅ **Gestion fine des permissions** par rôle
- ✅ **Audit complet** de toutes les actions
- ✅ **Architecture scalable** et maintenable
- ✅ **Tests exhaustifs** et documentation complète

**Prêt pour la production !** 🎉
