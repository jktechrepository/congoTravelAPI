# 📚 **Documentation du Module d'Authentification CongoTravel API**

## 🔐 **Vue d'ensemble**

Le module d'authentification utilise **JWT (JSON Web Tokens)** avec un système de **refresh tokens** pour sécuriser l'accès à l'API. Il supporte l'authentification par **email**, **téléphone** ou **username**.

---

## 🚀 **Endpoints Principaux**

### **1. POST `/api/Utilisateur/login`**
**Endpoint principal d'authentification**

#### **📋 Requête (`AuthentificationRequest`):**
```json
{
  "emailOuTelephone": "user@example.com",
  "motDePasse": "password123",
  "fcmToken": "firebase-token-123",
  "deviceType": "Android",
  "deviceModel": "Samsung Galaxy S21",
  "osVersion": "Android 12"
}
```

#### **📋 Réponse (`AuthentificationResponse`):**
```json
{
  "success": true,
  "message": "Authentification réussie",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh-token-123",
  "tokenType": "Bearer",
  "expiresIn": 86400,
  "expiresAt": "2026-02-26T13:20:00Z",
  "doitChangerMotDePasse": false,
  "utilisateur": {
    "idUtilisateur": 123,
    "referenceUtilisateur": "guid-123",
    "nomComplet": "Jean Dupont",
    "email": "user@example.com",
    "telephone": "+237123456789",
    "photoUrl": "https://example.com/photo.jpg",
    "genre": "M",
    "statut": true,
    "idAgent": 456,
    "idClient": 789,
    "idSociete": 1,
    "dateCreation": "2026-01-01T00:00:00Z",
    "isConnecte": true
  },
  "nomRole": "Admin",
  "nomSociete": "Ecole CongoTravel",
  "acceptNotification": true,
  "permissions": ["Societe.Read", "Paiement.Create"],
  "roles": [
    {
      "idRole": 2,
      "nom": "Admin",
      "statut": true
    }
  ],
  "primaryRole": {
    "idRole": 2,
    "nom": "Admin",
    "statut": true
  },
  "client": {
    "idClient": 789,
    "nomClient": "Client Name",
    "codeCons": "CLI001",
    "usages": [
      {
        "idUsage": 1,
        "libelle": "Usage 1",
        "nombreBatiment": 2,
        "dateAttribution": "2026-01-01",
        "statut": true
      }
    ]
  },
  "agent": {
    "idAgent": 456,
    "matricule": "AGT001",
    "nomComplet": "Agent Name",
    "genre": "M",
    "dateNaissance": "1990-01-01",
    "telephoneAgent": "+237123456789",
    "emailAgent": "agent@example.com",
    "statut": true,
    "fonction": "Collecteur",
    "roleAgent": "Agent",
    "photoUrl": "https://example.com/agent.jpg",
    "idSociete": 1,
    "adresseResidence": "Adresse",
    "zone": "Zone A"
  }
}
```

---

### **2. GET `/api/AuthTest/public`**
**Endpoint public de test**
```json
{
  "message": "API fonctionne correctement",
  "timestamp": "2026-02-25T13:20:00Z"
}
```

### **3. GET `/api/AuthTest/protected`**
**Endpoint protégé pour tester l'authentification**
```json
{
  "message": "Authentification JWT réussie !",
  "user": {
    "isAuthenticated": true,
    "userId": 123,
    "userName": "Jean Dupont",
    "userRole": "Admin",
    "societeId": 1,
    "isSuperAdmin": false,
    "isAdmin": true,
    "timestamp": "2026-02-25T13:20:00Z"
  },
  "note": "Le middleware AutoBearer a ajouté automatiquement le préfixe Bearer si nécessaire"
}
```

### **4. GET `/api/AuthTest/permissions`**
**Endpoint pour vérifier les permissions**
```json
{
  "message": "Permissions de l'utilisateur",
  "permissions": {
    "isAuthenticated": true,
    "userId": 123,
    "userRole": "Admin",
    "societeId": 1,
    "isSuperAdmin": false,
    "isAdmin": true,
    "isStaff": true,
    "hasFinanceAccess": true,
    "hasPedagogieAccess": false,
    "agentId": 456,
    "clientId": 789
  }
}
```

---

## 🔧 **Processus d'Authentification**

### **Étape 1: Recherche de l'utilisateur**
Le système recherche l'utilisateur par ordre de priorité:
1. **Email** (`user@example.com`)
2. **DefaultUsername** (`username`)
3. **Téléphone** (`+237123456789`)

### **Étape 2: Validation des identifiants**
- **Vérification du mot de passe** avec BCrypt
- **Vérification du statut** de l'utilisateur (actif/inactif)
- **Vérification du statut** de la société

### **Étape 3: Génération des tokens**
- **Access Token JWT** (valide 24h par défaut)
- **Refresh Token** (pour renouveler l'access token)

### **Étape 4: Enregistrement du device**
- **Token FCM** pour notifications push
- **Informations du device** (type, modèle, OS)

### **Étape 5: Chargement des données**
- **Informations utilisateur** complètes
- **Rôles et permissions**
- **Informations Client/Agent** si applicable

---

## 🔐 **Sécurité JWT**

### **Configuration des Tokens**
```csharp
// Dans Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("Bearer", options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"])
            ),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
```

### **Propriétés du Token**
- **Secret Key**: Clé secrète configurable
- **Expiration**: 24 heures (1440 minutes) par défaut
- **Algorithme**: HS256
- **Type**: Bearer

---

## 🎭 **Gestion des Rôles et Permissions**

### **Système Multi-Rôles**
- **Rôles multiples** possibles par utilisateur
- **Rôle principal** identifié
- **Permissions** calculées par union des rôles

### **Hiérarchie des Rôles**
1. **Super-Admin**: Accès à tout
2. **Admin**: Gestion de l'école
3. **Financier**: Accès financier
4. **Gerant**: Gération des opérations
5. **Caissier**: Encaissements
6. **Agent**: Collecte sur terrain

### **Protection des Endpoints**
```csharp
[Authorize] // Tous les utilisateurs authentifiés
[Authorize(Roles = "Admin,Super-Admin")] // Rôles spécifiques
```

---

## 🔄 **Refresh Tokens**

### **Génération**
- **Device Info**: Type et modèle du device
- **IP Address**: Adresse IP de connexion
- **Validité**: Configurable (généralement 7-30 jours)

### **Utilisation**
Permet de renouveler l'access token sans:
- **Nouvelle authentification**
- **Saisie du mot de passe**
- **Perte de session**

---

## 📱 **Support Mobile**

### **Notifications Push**
- **Firebase Cloud Messaging (FCM)**
- **Token FCM** enregistré lors du login
- **Device tracking** pour multi-support

### **Informations Device**
- **Device Type**: Android/iOS/Web
- **Device Model**: Modèle spécifique
- **OS Version**: Version du système

---

## 🔍 **Audit et Logging**

### **Logs d'Authentification**
- **Recherche utilisateur** (méthode utilisée)
- **Validation mot de passe**
- **Génération tokens**
- **Enregistrement devices**
- **Erreurs d'authentification**

### **Audit Trail**
- **Connexions réussies**
- **Tentatives échouées**
- **Changements de rôles**
- **Actions sensibles**

---

## 🚨 **Gestion des Erreurs**

### **Codes d'Erreur**
- **400**: Données invalides
- **401**: Non authentifié (token invalide/expiré)
- **403**: Non autorisé (rôle insuffisant)
- **404**: Utilisateur non trouvé
- **500**: Erreur serveur

### **Messages d'Erreur**
```json
{
  "message": "Email/Telephone ou mot de passe incorrect"
}
```

---

## 🔧 **Configuration**

### **Variables d'Environnement**
```json
{
  "Jwt": {
    "SecretKey": "votre-clé-secrète",
    "ExpirationMinutes": "1440"
  }
}
```

### **Services Injections**
- **ISimpleJwtService**: Génération JWT
- **IRefreshTokenService**: Gestion refresh tokens
- **IPermissionService**: Gestion permissions
- **ICurrentUserService**: Utilisateur courant

---

## 📝 **Exemples d'Utilisation**

### **Frontend JavaScript**
```javascript
// Login
const response = await fetch('/api/Utilisateur/login', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    emailOuTelephone: 'user@example.com',
    motDePasse: 'password123',
    fcmToken: 'firebase-token'
  })
});

const data = await response.json();
localStorage.setItem('token', data.accessToken);
```

### **Utilisation du Token**
```javascript
// Requête authentifiée
const response = await fetch('/api/Statistiques/generales/1', {
  headers: {
    'Authorization': `Bearer ${localStorage.getItem('token')}`
  }
});
```

---

## 🎯 **Bonnes Pratiques**

### **Sécurité**
- **HTTPS** obligatoire en production
- **Secret Key** forte et unique
- **Expiration** raisonnable des tokens
- **Validation** stricte des entrées

### **Performance**
- **Caching** des permissions
- **Lazy loading** des relations
- **Pagination** des listes
- **Indexation** des colonnes de recherche

### **Logging**
- **Structured logging** avec niveaux
- **Correlation IDs** pour le debugging
- **Sensitive data masking** dans les logs
- **Audit trail** complet

---

## 🔄 **Flow Complet d'Authentification**

```
1. Client → POST /api/Utilisateur/login
   {
     "emailOuTelephone": "user@example.com",
     "motDePasse": "password123"
   }

2. API → Recherche utilisateur
   - Email → Username → Téléphone

3. API → Validation identifiants
   - BCrypt verification
   - Statut utilisateur/société

4. API → Génération tokens
   - Access Token (JWT)
   - Refresh Token

5. API → Enregistrement device
   - Token FCM
   - Device info

6. API → Chargement données
   - Utilisateur complet
   - Rôles et permissions
   - Client/Agent info

7. Client ← Réponse complète
   - Tokens + Utilisateur + Permissions
```

---

## 📋 **Modèles de Données**

### **AuthentificationRequest**
```csharp
public class AuthentificationRequest
{
    [Required(ErrorMessage = "L'email ou le téléphone est requis")]
    public string? EmailOuTelephone { get; set; }

    [Required(ErrorMessage = "Le mot de passe est requis")]
    public string? MotDePasse { get; set; }

    // Informations du device (optionnelles)
    public string? FcmToken { get; set; }
    public string? DeviceType { get; set; }
    public string? DeviceModel { get; set; }
    public string? OsVersion { get; set; }
}
```

### **AuthentificationResponse**
```csharp
public class AuthentificationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public Utilisateur? Utilisateur { get; set; }
    public bool DoitChangerMotDePasse { get; set; }
    public string? NomRole { get; set; }
    public string? NomSociete { get; set; }
    public bool AcceptNotification { get; set; } = true;
    public List<string>? Permissions { get; set; }
    public List<Role>? Roles { get; set; }
    public Role? PrimaryRole { get; set; }
    public ClientInfoDto? Client { get; set; }
    public AgentInfoDto? Agent { get; set; }
}
```

---

## 🛡️ **Mesures de Sécurité**

### **Protection contre les attaques**
- **Brute Force**: Limitation des tentatives
- **Injection SQL**: Utilisation d'Entity Framework
- **XSS**: Validation et encodage des entrées
- **CSRF**: Tokens anti-CSRF
- **Session Hijacking**: Tokens JWT à courte durée

### **Validation des entrées**
- **Email**: Format email valide
- **Mot de passe**: Longueur minimale
- **Téléphone**: Format international
- **Device Info**: Validation des types

---

## 📊 **Monitoring et Métriques**

### **KPIs à surveiller**
- **Taux de succès** des authentifications
- **Temps de réponse** des endpoints
- **Nombre de tentatives** échouées
- **Tokens générés** par jour
- **Devices actifs** par utilisateur

### **Alertes**
- **Pic de tentatives** échouées
- **Tokens expirés** anormaux
- **Erreurs serveur** répétées
- **Connexions** suspectes

---

## 🚀 **Déploiement**

### **Configuration Production**
```json
{
  "Jwt": {
    "SecretKey": "votre-clé-secrète-Production",
    "ExpirationMinutes": "1440"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### **Checks de santé**
- **Endpoint public**: `/api/AuthTest/public`
- **Endpoint protégé**: `/api/AuthTest/protected`
- **Validation tokens**: Automatique

---

## 🎉 **Conclusion**

Le module d'authentification CongoTravel API offre:
- **🔐 Sécurité robuste** avec JWT
- **🔄 Refresh tokens** pour meilleure UX
- **📱 Support mobile** complet
- **🎭 Gestion rôles** flexible
- **📊 Audit complet** des activités
- **🚀 Performance** optimisée

C'est une solution **entreprise-grade** prête pour la production! 🚀✨

---

## 📞 **Support**

Pour toute question ou problème concernant l'authentification:
- **Documentation**: Ce document
- **Logs**: Logs structurés avec niveaux
- **Monitoring**: KPIs et alertes
- **Debug**: Endpoints de test disponibles

---

*Document généré le 25 février 2026 - Version 1.0*
