# Documentation Complète - Module de Gestion Utilisateur KenergieAPI

## Table des Matières
1. [Vue d'ensemble](#vue-densemble)
2. [Architecture Technique](#architecture-technique)
3. [Composants Principaux](#composants-principaux)
4. [Endpoints API](#endpoints-api)
5. [Modèles de Données](#modèles-de-données)
6. [Flux d'Authentification](#flux-dauthentification)
7. [Sécurité et Autorisations](#sécurité-et-autorisations)
8. [Synchronisation des Données](#synchronisation-des-données)

---

## Vue d'ensemble

Le module de gestion utilisateur est le composant central de KenergieAPI qui gère l'authentification, les autorisations et le cycle de vie complet des utilisateurs du système de facturation électrique.

### Fonctionnalités Principales
- **Authentification JWT** avec refresh tokens
- **Gestion multi-rôles** (RBAC)
- **Synchronisation automatique** Agent/Client-Utilisateur
- **Gestion des appareils** et notifications push
- **Audit complet** des modifications
- **Réinitialisation** des mots de passe

---

## Architecture Technique

### Diagramme d'Architecture

```
Client (Frontend/Mobile)
        |
        v
[UtilisateurController] - API REST
        |
        v
[UtilisateurService] - Logique Métier
        |
        v
[IUtilisateurRepository] - Interface Repository
        |
        v
[KenergieDbContext] - Entity Framework Core
        |
        v
MariaDB 10.11 (Base de données)
```

### Dépendances Principales
- **ASP.NET Core 6.0** - Framework web
- **Entity Framework Core 6.0** - ORM
- **JWT Bearer** - Authentification
- **BCrypt.Net** - Hashage mots de passe
- **AutoMapper** - Mapping DTO
- **Serilog** - Logging

---

## Composants Principaux

### 1. UtilisateurController

**Fichier** : `Controllers/UtilisateurController.cs` (2814 lignes)

**Rôle** : Controller API avec 35+ endpoints

#### Principaux Endpoints

| Méthode | Route | Description | Rôles Autorisés |
|---------|-------|-------------|-----------------|
| GET | `/api/Utilisateur` | Liste paginée des utilisateurs | Admin, Super-Admin |
| GET | `/api/Utilisateur/{id}` | Détails d'un utilisateur | Tous |
| PUT | `/api/Utilisateur/{id}` | Mise à jour utilisateur | Admin, Super-Admin |
| POST | `/api/Utilisateur/authenticate` | Authentification | Public |
| POST | `/api/Utilisateur/refresh-token` | Rafraîchir token | Public |
| PUT | `/api/Utilisateur/toggle-statut/{id}` | Auto-désactivation (`id` = self, `Utilisateur.DeactivateSelf`) ou toggle admin (`Utilisateur.Update`) | Tous rôles (self) ; Admin/Super-Admin (autres) |
| POST | `/api/Utilisateur/changer-mot-de-passe` | Changer mot de passe | Utilisateur connecté |
| POST | `/api/Utilisateur/reset-password` | Réinitialiser mot de passe | Admin, Super-Admin |

#### Auto-désactivation (`toggle-statut`)

- **Self** : `PUT /api/Utilisateur/toggle-statut/{monIdUtilisateur}` où `{monIdUtilisateur}` est **exactement** l’id du JWT (`NameIdentifier` / `sub`). Permission requise : `Utilisateur.DeactivateSelf` (tous les rôles).
- **Admin** : modifier le statut d’un **autre** utilisateur nécessite `Utilisateur.Update` (réservé Admin/Super-Admin). **Ne pas** attribuer `Utilisateur.Update` au rôle Client.
- Si l’`id` dans l’URL ne correspond pas au JWT et que l’appelant n’a pas `Utilisateur.Update`, réponse **403** avec `code: "TOGGLE_STATUT_NOT_SELF"`.

#### Permissions rôle Client (CongoTravel)

Périmètre transport : `Client.Read/ReadAll`, `ClientDashboard.ReadAll`, `PlainteClient.*`, `Reservation.Create/Read/ReadAll`, `Paiement.Read/ReadAll`, `Billet.Read/ReadAll`, `Voyage.Read/ReadAll`, `Destination.Read/ReadAll`, `Utilisateur.DeactivateSelf`.

Legacy retiré du rôle Client : `Facture.*`, `CategorieClient.*` (héritage Kenergie). Migration SQL : `Scripts/migrate_client_permissions_congotravel.sql`.

#### Sécurité
- **Protection JWT globale** sur tous les endpoints
- **Validation des rôles** par endpoint
- **Audit trail** pour toutes les modifications
- **Rate limiting** contre brute force

### 2. UtilisateurService

**Fichier** : `Services/UtilisateurService.cs` (663 lignes)

**Rôle** : Implémentation du repository avec logique métier

#### Fonctionnalités Clés
- **CRUD complet** avec validation
- **Multi-rôles** : AddRoleToUserAsync, RemoveRoleFromUserAsync
- **Synchronisation** bidirectionnelle Agent/Client-Utilisateur
- **Authentification** avec validation BCrypt
- **Soft delete** via ToggleStatutAsync
- **Réinitialisation** mots de passe (masse et individuel)

#### Méthodes Principales
```csharp
// Multi-rôles
Task<bool> AddRoleToUserAsync(int userId, int roleId, int? assignedByUserId = null, bool isPrimary = false)
Task<bool> RemoveRoleFromUserAsync(int userId, int roleId)

// CRUD
Task<IEnumerable<Utilisateur>> GetAllAsync()
Task<Utilisateur> GetByIdAsync(int id)
Task<Utilisateur> CreateAsync(Utilisateur utilisateur)
Task<Utilisateur> UpdateAsync(Utilisateur utilisateur)

// Authentification
Task<bool> AuthentifierAsync(string email, string motDePasse)
Task<bool> ChangerMotDePasseAsync(int id, string ancienMotDePasse, string nouveauMotDePasse)

// Gestion
Task<bool> ToggleStatutAsync(int id)
Task<bool> MarquerCommeConnecteAsync(int id)
Task<bool> MarquerCommeDeconnecteAsync(int id)
```

### 3. IUtilisateurRepository

**Fichier** : `Services/Repositories/IUtilisateurRepository.cs` (45 lignes)

**Rôle** : Interface définissant le contrat du repository

#### Méthodes Définies (43 méthodes)
- **Recherche** : GetByEmailAsync, GetByDefaultUsernameAsync, GetByReferenceAsync
- **Filtrage** : GetBySocieteAsync, GetByRoleAsync, GetByStatutAsync
- **CRUD** : CreateAsync, UpdateAsync, DeleteAsync
- **Validation** : ExistsAsync, ExistsByEmailAsync, AuthentifierAsync
- **Gestion** : ToggleStatutAsync, ResetPasswordAsync
- **Multi-rôles** : AddRoleToUserAsync, RemoveRoleFromUserAsync

---

## Modèles de Données

### DTOs d'Authentification

#### AgentInfoDto
```csharp
public class AgentInfoDto
{
    public int IdAgent { get; set; }
    public string? Matricule { get; set; }
    public string? NomComplet { get; set; }
    public string? Genre { get; set; }
    public string? TelephoneAgent { get; set; }
    public string? EmailAgent { get; set; }
    public string? Fonction { get; set; }
    public string? RoleAgent { get; set; }
    public string? PhotoUrl { get; set; }
    public int? IdSociete { get; set; }
    public string? AdresseResidence { get; set; }
    public string? Zone { get; set; }
}
```

#### ClientInfoDto
```csharp
public class ClientInfoDto
{
    public int IdClient { get; set; }
    public string NomClient { get; set; }
    public string? CodeCons { get; set; }
    public string? Telephone { get; set; }
    public string? EmailClient { get; set; }
    public string? GenreClient { get; set; }
    public string? AdresseClient { get; set; }
    public bool Statut { get; set; }
    public bool IsActif { get; set; }
    public int? IdAxe { get; set; }
    public List<AuthentificationUsageInfoDto> Usages { get; set; }
}
```

#### AuthentificationUsageInfoDto
```csharp
public class AuthentificationUsageInfoDto
{
    public int IdUsage { get; set; }
    public string Libelle { get; set; }
    public int NombreBatiment { get; set; }
    public DateTime DateAttribution { get; set; }
    public bool Statut { get; set; }
}
```

### DTOs de Mise à Jour

#### UpdateUtilisateurDto
```csharp
public class UpdateUtilisateurDto
{
    [Required] public int IdUtilisateur { get; set; }
    [Required] public string? NomComplet { get; set; }
    [Required, EmailAddress] public string? Email { get; set; }
    [Phone] public string? Telephone { get; set; }
    public string? PhotoUrl { get; set; }
    public string? LieuNaissance { get; set; }
    public DateTime? DateNaissance { get; set; }
    [RegularExpression("^(M|F|Autre)$")] public string? Genre { get; set; }
}
```

#### UpdateUtilisateurAdminDto
```csharp
public class UpdateUtilisateurAdminDto : UpdateUtilisateurDto
{
    [Range(1, int.MaxValue)] public int? IdRole { get; set; }
    public bool? Statut { get; set; }
    // Champs protégés : IdSociete, MotDePasseHash, ReferenceUtilisateur, DateCreation
}
```

#### UpdateUserDeviceDto
```csharp
public class UpdateUserDeviceDto
{
    [Required] public int IdUserDevice { get; set; }
    [StringLength(500)] public string? FcmToken { get; set; }
    [StringLength(100)] public string? DeviceType { get; set; }
    [StringLength(100)] public string? DeviceModel { get; set; }
    [StringLength(50)] public string? OsVersion { get; set; }
    public bool? Statut { get; set; } = true;
}
```

#### UtilisateurInfo
```csharp
public class UtilisateurInfo
{
    public int IdUtilisateur { get; set; }
    public int? IdAgent { get; set; }
    public string Email { get; set; }
    public string DefaultUsername { get; set; }
    public string? Telephone { get; set; }
    public string? MotDePasseParDefaut { get; set; }
    public string? NomComplet { get; set; }
    public string? Role { get; set; }
    public bool Created { get; set; }
    public string? Message { get; set; }
}
```

---

## Flux d'Authentification

### 1. Connexion Utilisateur

```http
POST /api/Utilisateur/authenticate
Content-Type: application/json

{
  "email": "user@example.com",
  "motDePasse": "password123",
  "fcmToken": "device_token_here"
}
```

#### Réponse
```json
{
  "success": true,
  "message": "Authentification réussie",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "refresh_token_here",
    "expiration": "2025-04-20T20:00:00Z",
    "utilisateur": {
      "idUtilisateur": 123,
      "email": "user@example.com",
      "nomComplet": "Jean Dupont",
      "role": "Admin",
      "societe": "Kenergie",
      "agentInfo": { ... },
      "clientInfo": { ... },
      "permissions": [ ... ]
    }
  }
}
```

### 2. Rafraîchissement Token

```http
POST /api/Utilisateur/refresh-token
Content-Type: application/json

{
  "refreshToken": "refresh_token_here"
}
```

### 3. Déconnexion

```http
POST /api/Utilisateur/logout
Authorization: Bearer token_here
```

---

## Sécurité et Autorisations

### 1. JWT Configuration

```csharp
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
```

### 2. Hiérarchie des Rôles

| Rôle | Permissions | Description |
|------|-------------|-------------|
| Super-Admin | TOUTES | Accès toutes sociétés, tous utilisateurs |
| Admin | Société | Gestion utilisateurs de sa société |
| Financier | Société | Accès données financières |
| Caissier | Société | Gestion paiements |
| Agent | Limité | Accès ses données personnelles |
| Client | Limité | Accès ses données personnelles |

### 3. Validation des Permissions

```csharp
[Authorize(Roles = "Admin,Super-Admin")]
public async Task<ActionResult<object>> GetUtilisateurs(...) { }

[Authorize]
public async Task<ActionResult<Utilisateur>> GetUtilisateur(int id) { }
```

---

## Synchronisation des Données

### 1. Agent <-> Utilisateur

**Champs synchronisés :**
- NomComplet (bidirectionnel)
- Telephone/TelephoneAgent (avec validation unicité)
- Email/EmailAgent (avec validation unicité)
- Genre/GenreAgent
- PhotoUrl (bidirectionnel)
- AdresseResidence/AdresseResidence (bidirectionnel)
- Statut (Agent -> Utilisateur)
- RoleAgent -> IdRole (Agent -> Utilisateur)

### 2. Client <-> Utilisateur

**Champs synchronisés :**
- NomClient -> NomComplet
- Telephone (bidirectionnel)
- EmailClient -> Email
- GenreClient -> Genre
- AdresseClient -> AdresseResidence

### 3. Mécanisme de Synchronisation

```csharp
// Dans UtilisateurService.UpdateAsync()
if (idAgentASynchroniser.HasValue)
{
    var agent = await _context.Agents.FindAsync(idAgentASynchroniser.Value);
    if (agent != null && champsModifies)
    {
        // Synchroniser les champs modifiés avec validation
        if (!string.IsNullOrWhiteSpace(utilisateur.Telephone))
        {
            var telephoneDejaUtilise = await _context.Agents
                .AnyAsync(a => a.TelephoneAgent == utilisateur.Telephone && a.IdAgent != agent.IdAgent);
            
            if (!telephoneDejaUtilise)
                agent.TelephoneAgent = utilisateur.Telephone;
        }
    }
}
```

---

## Points Techniques Importants

### 1. Performance
- **Pagination** obligatoire sur les listes (max 100 par page)
- **Indexation** sur Email, Telephone, DefaultUsername
- **Includes optimisés** pour éviter N+1 queries
- **Caching** pour les données statiques (rôles, permissions)

### 2. Sécurité
- **Hashage BCrypt** avec salt (11 rounds)
- **Tokens JWT** avec expiration configurable
- **Refresh tokens** stockés en base avec rotation
- **Audit trail** sur toutes les modifications sensibles
- **Rate limiting** contre attaques brute force

### 3. Gestion des Erreurs
- **Logging structuré** avec Serilog
- **Messages d'erreur** localisés
- **Validation des entrées** avec DataAnnotations
- **Gestion des conflits** (email/téléphone dupliqués)

### 4. Tests et Maintenance
- **Tests unitaires** sur les méthodes critiques
- **Tests d'intégration** pour les endpoints
- **Monitoring** des performances et erreurs
- **Documentation** automatique avec Swagger/OpenAPI

---

## Création société, site et gérant (`POST /api/Societe`)

La création d’une société peut inclure le provisionnement d’un **site** et d’un compte **gérant** lié à ce site, tout en conservant le compte **administrateur** automatique. Le corps JSON est un objet imbriqué `{ societe, site, gerant }`.

Référence détaillée : **`DOCUMENTATION_API_SOCIETE_CREATE_BOOTSTRAP.md`**.

---

## Conclusion

Le module de gestion utilisateur de KenergieAPI représente une solution complète et sécurisée pour la gestion des identités dans un contexte multi-sociétés de facturation électrique. Ses points forts sont :

- **Architecture robuste** avec séparation des responsabilités
- **Sécurité renforcée** avec JWT et RBAC
- **Synchronisation automatique** des données connexes
- **Performance optimisée** avec pagination et caching
- **Maintenance facilitée** avec logging et audit complet

Ce module sert de fondation pour toutes les autres fonctionnalités du système KenergieAPI.

---

**Date de documentation** : 20 avril 2026  
**Version** : 1.0.0  
**Auteur** : Cascade AI Assistant
