# ✅ Modification : CodeCons comme Champ Unique

## 📋 Résumé

Modification de la logique d'unicité pour utiliser `CodeCons` au lieu de `NomClient` comme champ unique pour les clients. Le champ `DefaultUsername` de l'utilisateur associé utilise maintenant `CodeCons`.

---

## 🔄 Modifications Apportées

### 1. Déduplication dans ExcelClientService

**Fichier :** `Services/ExcelClientService.cs`

**Méthode :** `DeduplicateInFile`

**Avant :**
```csharp
// Clé unique basée sur le nom
var key = client.NomClient?.ToLower() ?? "";
```

**Après :**
```csharp
// ✨ Clé unique basée sur CodeCons (seul champ unique)
var key = client.CodeCons?.Trim() ?? "";
```

**Effet :** La détection de doublons dans le fichier Excel utilise maintenant `CodeCons` au lieu de `NomClient`.

---

### 2. Vérification de Doublons lors de la Création

**Fichier :** `Services/ExcelClientService.cs`

**Méthode :** `CreateClientsFromValidDataAsync`

**Avant :**
```csharp
// Vérifier si le client existe déjà (par nom)
Client? existingClient = null;

if (!string.IsNullOrWhiteSpace(dto.NomClient))
{
    var clientsByNom = await _clientRepository.GetByNomAsync(dto.NomClient);
    existingClient = clientsByNom.FirstOrDefault();
}
```

**Après :**
```csharp
// ✨ Vérifier si le client existe déjà (par CodeCons - seul champ unique)
Client? existingClient = null;

if (!string.IsNullOrWhiteSpace(dto.CodeCons))
{
    existingClient = await _clientRepository.GetByCodeConsAsync(dto.CodeCons);
}
```

**Effet :** La vérification d'existence d'un client utilise maintenant `CodeCons` au lieu de `NomClient`.

---

### 3. DefaultUsername basé sur CodeCons

**Fichier :** `Services/ClientService.cs`

**Méthode :** `CreateDefaultClientUserAsync`

**Avant :**
```csharp
// Générer le DefaultUsername
string baseUsername = nomComplet.Replace(" ", "").Replace("-", "").Replace("'", "");
if (string.IsNullOrWhiteSpace(baseUsername))
{
    baseUsername = "Client";
}
if (baseUsername.Length > 20)
{
    baseUsername = baseUsername.Substring(0, 20);
}
Random random = new Random();
int randomNumber = random.Next(1, 1000);
string defaultUsername = $"{baseUsername}{randomNumber}";
```

**Après :**
```csharp
// ✨ Utiliser CodeCons comme DefaultUsername (seul champ unique)
// Recharger le client pour s'assurer d'avoir le CodeCons généré
var clientWithCodeCons = await _context.Clients
    .FirstOrDefaultAsync(c => c.IdClient == client.IdClient);

string defaultUsername;
if (!string.IsNullOrWhiteSpace(clientWithCodeCons?.CodeCons))
{
    // Utiliser CodeCons comme DefaultUsername
    defaultUsername = clientWithCodeCons.CodeCons.Trim();
    _logger.LogInformation("✅ Utilisation du CodeCons comme DefaultUsername: {CodeCons}", defaultUsername);
}
else
{
    // Fallback si CodeCons n'est pas disponible (ne devrait pas arriver normalement)
    _logger.LogWarning("⚠️ CodeCons non disponible pour le client {ClientId}, utilisation d'un username par défaut", client.IdClient);
    string baseUsername = nomComplet.Replace(" ", "").Replace("-", "").Replace("'", "");
    if (string.IsNullOrWhiteSpace(baseUsername))
    {
        baseUsername = "Client";
    }
    if (baseUsername.Length > 20)
    {
        baseUsername = baseUsername.Substring(0, 20);
    }
    Random random = new Random();
    int randomNumber = random.Next(1, 1000);
    defaultUsername = $"{baseUsername}{randomNumber}";
}
```

**Effet :** Le champ `DefaultUsername` de l'utilisateur associé au client utilise maintenant `CodeCons` au lieu d'un username généré à partir du nom.

---

### 4. Index Unique sur CodeCons

**Fichier :** `Data/CongoTravelDbContext.cs`

**Ajout :**
```csharp
// ✨ Index unique sur CodeCons (seul champ unique pour Client)
modelBuilder.Entity<Client>()
    .HasIndex(c => c.CodeCons)
    .IsUnique()
    .HasDatabaseName("IX_Client_CodeCons_Unique");
```

**Effet :** Ajout d'une contrainte d'unicité au niveau de la base de données pour `CodeCons`.

---

## 📊 Comportement Avant/Après

### Avant

| Aspect | Comportement |
|--------|--------------|
| **Champ unique** | `NomClient` (non unique en base, mais utilisé pour la déduplication) |
| **Déduplication Excel** | Basée sur `NomClient` |
| **Vérification doublons** | Recherche par `NomClient` |
| **DefaultUsername** | Généré à partir de `NomClient` + nombre aléatoire |

### Après

| Aspect | Comportement |
|--------|--------------|
| **Champ unique** | `CodeCons` (unique en base avec index unique) |
| **Déduplication Excel** | Basée sur `CodeCons` |
| **Vérification doublons** | Recherche par `CodeCons` |
| **DefaultUsername** | Utilise directement `CodeCons` |

---

## 🔍 Détails Techniques

### Génération de CodeCons

Le `CodeCons` est généré automatiquement lors de la création d'un client si :
- `IdAxe` est fourni
- `CodeCons` est vide ou null

Format : `{codeCabine}/{codeAxe}/{0001-9999}`

Exemple : `A/a1/0465`

### Gestion des Cas Limites

1. **CodeCons non fourni dans Excel** :
   - Le `CodeCons` sera généré automatiquement lors de la création du client
   - La déduplication dans le fichier ne fonctionnera pas pour ces lignes
   - La vérification de doublons en base se fera après la génération

2. **CodeCons null lors de la création de l'utilisateur** :
   - Un fallback est prévu pour générer un username par défaut
   - Un warning est loggé pour indiquer le problème

---

## ✅ Checklist de Validation

- [x] Déduplication dans Excel basée sur `CodeCons`
- [x] Vérification de doublons basée sur `CodeCons`
- [x] `DefaultUsername` utilise `CodeCons`
- [x] Index unique ajouté sur `CodeCons` dans la base de données
- [x] Code compile sans erreurs
- [x] Gestion des cas limites (CodeCons null/vide)

---

## 🚀 Migration Base de Données

Pour appliquer l'index unique sur `CodeCons` en production, exécuter :

```sql
-- Vérifier d'abord s'il n'y a pas de doublons
SELECT CodeCons, COUNT(*) as Count
FROM Clients
WHERE CodeCons IS NOT NULL
GROUP BY CodeCons
HAVING COUNT(*) > 1;

-- Si aucun doublon, créer l'index unique
CREATE UNIQUE INDEX IX_Client_CodeCons_Unique ON Clients(CodeCons);
```

**⚠️ Important :** Vérifier qu'il n'y a pas de doublons avant de créer l'index unique.

---

## 📝 Notes

- `NomClient` n'est plus considéré comme unique
- Plusieurs clients peuvent avoir le même `NomClient`
- Seul `CodeCons` est unique et utilisé pour identifier un client de manière unique
- Le `DefaultUsername` de l'utilisateur associé correspond maintenant au `CodeCons` du client

---

**Date de modification :** 2025-01-05  
**Version :** 1.0.0
