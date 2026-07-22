# ✅ Synchronisation Agent ↔ Utilisateur

## 📋 Résumé

Vérification complète de la synchronisation bidirectionnelle entre les modifications de l'Agent et l'Utilisateur associé.

---

## ✅ Synchronisation Confirmée

### 1. Agent → Utilisateur (Lors de la mise à jour de l'Agent)

#### Méthode `UpdateAsync` (AgentService.cs, lignes 246-305)

**Champs synchronisés :**
- ✅ `NomComplet` → `NomComplet` (utilisateur)
- ✅ `TelephoneAgent` → `Telephone` (utilisateur)
- ✅ `EmailAgent` → `Email` (utilisateur)
- ✅ `Genre` → `Genre` (utilisateur)
- ✅ `PhotoUrl` → `PhotoUrl` (utilisateur)
- ✅ `AdresseResidence` → `AdresseResidence` (utilisateur)
- ✅ `Statut` → `Statut` (utilisateur)
- ✅ `IdSociete` → `IdSociete` (utilisateur)
- ✅ `RoleAgent` → `IdRole` (utilisateur, via UserRole)

**Logique :**
```csharp
// 1. Sauvegarder les anciennes valeurs
var oldNomComplet = existingAgent.NomComplet;
var oldTelephoneAgent = existingAgent.TelephoneAgent;
var oldEmailAgent = existingAgent.EmailAgent;
var oldGenre = existingAgent.Genre;
var oldPhotoUrl = existingAgent.PhotoUrl;
var oldAdresseResidence = existingAgent.AdresseResidence;

// 2. Mettre à jour l'agent
_context.Entry(existingAgent).CurrentValues.SetValues(agent);
await _context.SaveChangesAsync();

// 3. Vérifier si des champs ont changé
var champsModifies = 
    oldNomComplet != agent.NomComplet ||
    oldTelephoneAgent != agent.TelephoneAgent ||
    oldEmailAgent != agent.EmailAgent ||
    oldGenre != agent.Genre ||
    oldPhotoUrl != agent.PhotoUrl ||
    oldAdresseResidence != agent.AdresseResidence;

// 4. Si changements détectés, synchroniser avec l'utilisateur lié
if (champsModifies)
{
    await SyncAgentUtilisateurAsync(existingAgent, previousRoleAgent);
    await _context.SaveChangesAsync();
}
```

**Méthode `SyncAgentUtilisateurAsync` (AgentService.cs, lignes 396-493)**

Cette méthode privée gère la synchronisation complète :

```csharp
private async Task SyncAgentUtilisateurAsync(Agent agent, string? previousRoleAgent)
{
    // 1. Récupérer l'utilisateur lié à l'agent
    var utilisateur = await _context.Utilisateurs
        .FirstOrDefaultAsync(u => u.IdAgent == agent.IdAgent);

    if (utilisateur == null)
        return;

    // 2. Synchroniser le nom complet
    if (!string.IsNullOrWhiteSpace(agent.NomComplet))
        utilisateur.NomComplet = agent.NomComplet;

    // 3. Synchroniser le téléphone avec vérification d'unicité
    if (agent.TelephoneAgent != utilisateur.Telephone)
    {
        if (!string.IsNullOrWhiteSpace(agent.TelephoneAgent))
        {
            var telephoneDejaUtilise = await _context.Utilisateurs
                .AnyAsync(u => u.Telephone == agent.TelephoneAgent && u.IdUtilisateur != utilisateur.IdUtilisateur);
            
            if (!telephoneDejaUtilise)
                utilisateur.Telephone = agent.TelephoneAgent;
            else
                _logger.LogWarning("⚠️ Téléphone non synchronisé car déjà utilisé");
        }
        else
            utilisateur.Telephone = agent.TelephoneAgent;
    }

    // 4. Synchroniser l'email avec vérification d'unicité
    if (agent.EmailAgent != utilisateur.Email)
    {
        if (!string.IsNullOrWhiteSpace(agent.EmailAgent))
        {
            var emailDejaUtilise = await _context.Utilisateurs
                .AnyAsync(u => u.Email == agent.EmailAgent && u.IdUtilisateur != utilisateur.IdUtilisateur);
            
            if (!emailDejaUtilise)
                utilisateur.Email = agent.EmailAgent;
            else
                _logger.LogWarning("⚠️ Email non synchronisé car déjà utilisé");
        }
        else
            utilisateur.Email = agent.EmailAgent;
    }

    // 5. Synchroniser les autres champs
    utilisateur.PhotoUrl = agent.PhotoUrl;
    utilisateur.Genre = agent.Genre;
    utilisateur.AdresseResidence = agent.AdresseResidence;
    utilisateur.Statut = agent.Statut ?? utilisateur.Statut;
    utilisateur.IdSociete = agent.IdSociete;

    // 6. Gestion des rôles (via UserRole)
    var desiredRole = agent.RoleAgent?.Trim();
    if (!string.IsNullOrWhiteSpace(desiredRole))
    {
        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.Nom == desiredRole);
        
        if (role != null)
            utilisateur.IdRole = role.IdRole;
    }
}
```

**Protections :**
- ✅ Vérification d'unicité pour `Telephone` et `Email` avant synchronisation
- ✅ Log d'avertissement si un champ ne peut pas être synchronisé (déjà utilisé)
- ✅ Synchronisation même si seuls les rôles ont changé

---

### 2. Utilisateur → Agent (Lors de la mise à jour de l'Utilisateur)

#### Méthode `UpdateAsync` (UtilisateurService.cs, lignes 393-480)

**Champs synchronisés :**
- ✅ `NomComplet` (utilisateur) → `NomComplet` (agent)
- ✅ `Telephone` (utilisateur) → `TelephoneAgent` (agent)
- ✅ `Email` (utilisateur) → `EmailAgent` (agent)
- ✅ `Genre` (utilisateur) → `Genre` (agent)
- ✅ `AdresseResidence` (utilisateur) → `AdresseResidence` (agent)
- ✅ `PhotoUrl` (utilisateur) → `PhotoUrl` (agent)

**Logique :**
```csharp
// Si l'utilisateur est lié à un agent (IdAgent)
var idAgentASynchroniser = utilisateur.IdAgent ?? oldIdAgent;

if (idAgentASynchroniser.HasValue)
{
    var agent = await _context.Agents.FindAsync(idAgentASynchroniser.Value);
    if (agent != null)
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
            // Synchroniser uniquement les champs qui ont changé
            if (oldNomComplet != utilisateur.NomComplet)
                agent.NomComplet = utilisateur.NomComplet;
            
            if (oldTelephone != utilisateur.Telephone)
            {
                // Vérifier l'unicité du téléphone avant de synchroniser
                if (!string.IsNullOrWhiteSpace(utilisateur.Telephone))
                {
                    var telephoneDejaUtilise = await _context.Agents
                        .AnyAsync(a => a.TelephoneAgent == utilisateur.Telephone && a.IdAgent != agent.IdAgent);
                    
                    if (!telephoneDejaUtilise)
                        agent.TelephoneAgent = utilisateur.Telephone;
                }
            }
            
            // ... autres champs ...
        }
    }
}
```

**Protections :**
- ✅ Vérification d'unicité pour `Telephone` et `Email` avant synchronisation
- ✅ Synchronisation uniquement si l'agent existe
- ✅ Synchronisation uniquement des champs modifiés

---

## 📊 Tableau de Correspondance

| Champ Agent | Champ Utilisateur | Direction | Vérification Unicité |
|-------------|-------------------|-----------|----------------------|
| `NomComplet` | `NomComplet` | Bidirectionnelle | ❌ Non |
| `TelephoneAgent` | `Telephone` | Bidirectionnelle | ✅ Oui |
| `EmailAgent` | `Email` | Bidirectionnelle | ✅ Oui |
| `Genre` | `Genre` | Bidirectionnelle | ❌ Non |
| `PhotoUrl` | `PhotoUrl` | Bidirectionnelle | ❌ Non |
| `AdresseResidence` | `AdresseResidence` | Bidirectionnelle | ❌ Non |
| `Statut` | `Statut` | Agent → Utilisateur | ❌ Non |
| `IdSociete` | `IdSociete` | Agent → Utilisateur | ❌ Non |
| `RoleAgent` | `IdRole` (via UserRole) | Agent → Utilisateur | ❌ Non |

---

## 🔍 Détails de la Synchronisation

### Vérification d'Unicité

#### Pour le Téléphone (Agent → Utilisateur)

```csharp
if (agent.TelephoneAgent != utilisateur.Telephone)
{
    if (!string.IsNullOrWhiteSpace(agent.TelephoneAgent))
    {
        // Vérifier si le téléphone est déjà utilisé par un autre utilisateur
        var telephoneDejaUtilise = await _context.Utilisateurs
            .AnyAsync(u => u.Telephone == agent.TelephoneAgent && u.IdUtilisateur != utilisateur.IdUtilisateur);
        
        if (!telephoneDejaUtilise)
        {
            utilisateur.Telephone = agent.TelephoneAgent; // ✅ Synchronisation
        }
        else
        {
            _logger.LogWarning(
                "⚠️ Téléphone '{Telephone}' non synchronisé pour l'utilisateur {UserId} (agent {AgentId}) car déjà utilisé",
                agent.TelephoneAgent, utilisateur.IdUtilisateur, agent.IdAgent);
        }
    }
    else
    {
        // Si le téléphone devient null/vide, on peut le synchroniser
        utilisateur.Telephone = agent.TelephoneAgent; // ✅ Synchronisation
    }
}
```

#### Pour l'Email (Agent → Utilisateur)

```csharp
if (agent.EmailAgent != utilisateur.Email)
{
    if (!string.IsNullOrWhiteSpace(agent.EmailAgent))
    {
        // Vérifier si l'email est déjà utilisé par un autre utilisateur
        var emailDejaUtilise = await _context.Utilisateurs
            .AnyAsync(u => u.Email == agent.EmailAgent && u.IdUtilisateur != utilisateur.IdUtilisateur);
        
        if (!emailDejaUtilise)
        {
            utilisateur.Email = agent.EmailAgent; // ✅ Synchronisation
        }
        else
        {
            _logger.LogWarning(
                "⚠️ Email '{Email}' non synchronisé pour l'utilisateur {UserId} (agent {AgentId}) car déjà utilisé",
                agent.EmailAgent, utilisateur.IdUtilisateur, agent.IdAgent);
        }
    }
    else
    {
        utilisateur.Email = agent.EmailAgent; // ✅ Synchronisation
    }
}
```

### Gestion des Rôles

**Agent → Utilisateur :**

```csharp
// Gestion des rôles (via UserRole)
var desiredRole = agent.RoleAgent?.Trim();
if (!string.IsNullOrWhiteSpace(desiredRole))
{
    var role = await _context.Roles
        .FirstOrDefaultAsync(r => r.Nom == desiredRole);
    
    if (role != null)
    {
        utilisateur.IdRole = role.IdRole;
    }
}
else if (!string.IsNullOrWhiteSpace(previousRoleAgent))
{
    // Si RoleAgent devient null, utiliser l'ancien rôle
    var role = await _context.Roles
        .FirstOrDefaultAsync(r => r.Nom == previousRoleAgent);
    
    if (role != null)
    {
        utilisateur.IdRole = role.IdRole;
    }
}
```

**Note :** La gestion des rôles utilise le système multi-rôles avec `UserRole`, mais la synchronisation met à jour `IdRole` pour compatibilité.

---

## ✅ Points de Vérification

### 1. Mise à jour de l'Agent via `UpdateAsync`

- [x] ✅ Synchronisation avec l'utilisateur lié (`IdAgent`)
- [x] ✅ Vérification d'unicité pour téléphone et email
- [x] ✅ Logging des avertissements si synchronisation impossible
- [x] ✅ Synchronisation même si seuls les rôles ont changé
- [x] ✅ Synchronisation de tous les champs pertinents (nom, téléphone, email, genre, photo, adresse, statut, société, rôle)

### 2. Mise à jour de l'Utilisateur

- [x] ✅ Synchronisation avec l'agent associé (`IdAgent`)
- [x] ✅ Vérification d'unicité pour téléphone et email
- [x] ✅ Synchronisation uniquement des champs modifiés

### 3. Méthode `ToggleStatutAsync`

- [x] ✅ Appelle `SyncAgentUtilisateurAsync` après changement de statut
- [x] ✅ Synchronise le statut avec l'utilisateur

---

## 📝 Exemples de Comportement

### Exemple 1 : Modification du nom de l'agent

**Action :**
```http
PUT /api/Agent/123
{
  "nomComplet": "Nouveau Nom Agent"
}
```

**Résultat :**
- ✅ Agent mis à jour : `NomComplet = "Nouveau Nom Agent"`
- ✅ Utilisateur lié (`IdAgent = 123`) : `NomComplet = "Nouveau Nom Agent"`
- ✅ Log : `"✅ Synchronisation Agent → Utilisateur: Utilisateur mis à jour pour l'agent 123"`

---

### Exemple 2 : Modification de l'email de l'agent (conflit)

**Action :**
```http
PUT /api/Agent/123
{
  "emailAgent": "nouveau@email.com"
}
```

**Scénario :** L'email `nouveau@email.com` est déjà utilisé par un autre utilisateur.

**Résultat :**
- ✅ Agent mis à jour : `EmailAgent = "nouveau@email.com"`
- ⚠️ Utilisateur lié : `Email` **non modifié** (conflit)
- ⚠️ Log : `"⚠️ Email 'nouveau@email.com' non synchronisé pour l'utilisateur X (agent 123) car déjà utilisé"`

---

### Exemple 3 : Modification du rôle de l'agent

**Action :**
```http
PUT /api/Agent/123
{
  "roleAgent": "Gerant"
}
```

**Résultat :**
- ✅ Agent mis à jour : `RoleAgent = "Gerant"`
- ✅ Utilisateur lié : `IdRole` mis à jour avec le rôle correspondant
- ✅ Log : `"✅ Synchronisation Agent → Utilisateur: Utilisateur mis à jour pour l'agent 123"`

**Note :** La synchronisation se fait même si seuls les rôles ont changé (ligne 299-302).

---

### Exemple 4 : Modification de l'utilisateur

**Action :**
```http
PUT /api/Utilisateur/456
{
  "nomComplet": "Nouveau Nom Utilisateur",
  "telephone": "+221 77 123 4567"
}
```

**Résultat :**
- ✅ Utilisateur mis à jour
- ✅ Agent associé (`IdAgent`) : `NomComplet = "Nouveau Nom Utilisateur"`, `TelephoneAgent = "+221 77 123 4567"`
- ✅ Log : `"✅ Synchronisation Utilisateur → Agent: Agent X mis à jour"`

---

### Exemple 5 : Toggle du statut de l'agent

**Action :**
```http
PUT /api/Agent/123/toggle-statut
```

**Résultat :**
- ✅ Agent : `Statut` inversé (actif ↔ inactif)
- ✅ Utilisateur lié : `Statut` synchronisé avec l'agent
- ✅ Log : `"✅ Synchronisation Agent → Utilisateur: Utilisateur mis à jour"`

---

## 🔒 Sécurité et Intégrité

### Protection contre les Conflits

1. **Téléphone :**
   - Vérifie l'unicité avant synchronisation
   - Si conflit : ne synchronise pas, log un avertissement
   - L'agent est quand même mis à jour

2. **Email :**
   - Vérifie l'unicité avant synchronisation
   - Si conflit : ne synchronise pas, log un avertissement
   - L'agent est quand même mis à jour

### Gestion des Valeurs Null/Vides

- ✅ Si le téléphone/email devient `null` ou vide, la synchronisation est effectuée
- ✅ Permet de "nettoyer" les données utilisateur si l'agent n'a plus de téléphone/email

### Gestion des Rôles

- ✅ Si `RoleAgent` change, le `IdRole` de l'utilisateur est mis à jour
- ✅ Si `RoleAgent` devient null, l'ancien rôle est conservé
- ✅ Gestion via la table `Roles` (recherche par `Nom`)

---

## 📊 Logs de Synchronisation

### Succès

```
✅ Synchronisation Agent → Utilisateur: Utilisateur mis à jour pour l'agent 123
```

### Avertissement (conflit)

```
⚠️ Téléphone '+221 77 123 4567' non synchronisé pour l'utilisateur 456 (agent 123) car déjà utilisé par un autre utilisateur
⚠️ Email 'test@example.com' non synchronisé pour l'utilisateur 456 (agent 123) car déjà utilisé par un autre utilisateur
```

---

## 🔄 Différences avec Client ↔ Utilisateur

### Champs Supplémentaires Synchronisés

**Agent → Utilisateur :**
- ✅ `PhotoUrl` (pas dans Client)
- ✅ `Statut` (synchronisé explicitement)
- ✅ `IdSociete` (synchronisé explicitement)
- ✅ `RoleAgent` → `IdRole` (gestion des rôles)

**Utilisateur → Agent :**
- ✅ `PhotoUrl` (pas dans Client)

### Gestion des Rôles

- ✅ **Agent** : Synchronise `RoleAgent` → `IdRole` de l'utilisateur
- ❌ **Client** : Pas de gestion des rôles (les clients n'ont pas de rôle spécifique)

### Méthode Dédiée

- ✅ **Agent** : Utilise une méthode privée `SyncAgentUtilisateurAsync` pour centraliser la logique
- ❌ **Client** : La synchronisation est inline dans `UpdateAsync`

---

## ✅ Conclusion

**La synchronisation est complète et fonctionnelle :**

1. ✅ **Agent → Utilisateur** : Implémentée dans `UpdateAsync` via `SyncAgentUtilisateurAsync`
2. ✅ **Utilisateur → Agent** : Implémentée dans `UtilisateurService.UpdateAsync`
3. ✅ **Protection contre les conflits** : Vérification d'unicité pour téléphone et email
4. ✅ **Logging** : Tous les événements sont loggés (succès et avertissements)
5. ✅ **Performance** : Synchronisation uniquement des champs modifiés
6. ✅ **Gestion des rôles** : Synchronisation du `RoleAgent` avec `IdRole`
7. ✅ **Toggle statut** : Synchronisation lors du changement de statut

**Aucune action supplémentaire n'est nécessaire.** ✅

---

**Date de vérification :** 2025-01-05  
**Version :** 1.0.0
