# ✅ Améliorations des Endpoints PUT et DELETE pour Client

## 📋 Résumé

Implémentation des améliorations recommandées pour les endpoints `PUT /api/Client/{id}` et `DELETE /api/Client/{id}` avec intégration complète de la logique `ClientUsage`.

---

## ✅ Améliorations Implémentées

### 1. PUT /api/Client/{id} - Mise à jour avec ClientUsage

#### Nouveau DTO

**Fichier :** `Models/DTOs/Client/UpdateClientWithUsagesDto.cs`

```csharp
public class UpdateClientWithUsagesDto
{
    // Champs du Client (tous optionnels)
    public string? NomClient { get; set; }
    public string? AdresseClient { get; set; }
    public string? Telephone { get; set; }
    public string? EmailClient { get; set; }
    public string? GenreClient { get; set; }
    public string? CodeCons { get; set; }
    public bool? Statut { get; set; }
    public bool? IsActif { get; set; }
    public int? IdAxe { get; set; }

    // Liste des usages à mettre à jour (optionnel)
    public List<ClientUsageUpdateDto>? Usages { get; set; }
}

public class ClientUsageUpdateDto
{
    public string LibelleUsage { get; set; } = string.Empty;
    public int nombreBatiment { get; set; } = 1;
    public bool Statut { get; set; } = true;
}
```

#### Nouvelle Méthode dans ClientService

**Fichier :** `Services/ClientService.cs`

```csharp
public async Task<Client> UpdateWithUsagesAsync(
    int idClient, 
    Client client, 
    List<(string LibelleUsage, int nombreBatiment, bool Statut)>? usages)
```

**Fonctionnalités :**
- ✅ Met à jour les champs du Client (seulement ceux fournis)
- ✅ Met à jour les ClientUsage si fournis dans le DTO
- ✅ Crée de nouveaux ClientUsage si l'usage n'existe pas encore
- ✅ Met à jour les ClientUsage existants (nombreBatiment, Statut)
- ✅ Supprime les ClientUsage qui ne sont plus dans la liste (soft delete si factures liées, hard delete sinon)
- ✅ Transactionnelle (rollback en cas d'erreur)
- ✅ Synchronise avec les Utilisateurs liés

#### Endpoint Modifié

**Fichier :** `Controllers/ClientController.cs`

```csharp
[HttpPut("{id}")]
public async Task<ActionResult<Client>> UpdateClient(
    int id, 
    [FromBody] UpdateClientWithUsagesDto dto)
{
    // Si des usages sont fournis, utilise UpdateWithUsagesAsync
    // Sinon, utilise UpdateAsync (comportement classique)
}
```

**Comportement :**
- ✅ Accepte `UpdateClientWithUsagesDto` au lieu de `Client`
- ✅ Si `Usages` est fourni : met à jour le client ET les usages
- ✅ Si `Usages` est null ou vide : met à jour seulement le client (comportement classique)
- ✅ Validation des usages (vérifie que les libellés existent)
- ✅ Gestion des erreurs améliorée

---

### 2. DELETE /api/Client/{id} - Soft Delete avec Validation

#### Validation des Dépendances

**Fichier :** `Controllers/ClientController.cs`

Avant la suppression, vérifie :
- ✅ `ClientFacture` liées au client
- ✅ `Paiements` liés au client
- ✅ `Factures` liées via les usages du client

**Réponse si dépendances trouvées :**
```json
{
  "message": "Impossible de supprimer ce client car des données sont liées.",
  "details": {
    "hasClientFactures": true,
    "hasPaiements": false,
    "hasFactures": true
  },
  "note": "Le client sera désactivé (soft delete) au lieu d'être supprimé."
}
```

#### Soft Delete Implémenté

**Fichier :** `Services/ClientService.cs`

```csharp
public async Task<bool> DeleteAsync(int id)
{
    // Soft delete : mettre Statut et IsActif à false
    client.Statut = false;
    client.IsActif = false;
    
    // Soft delete des ClientUsage associés
    foreach (var clientUsage in clientUsages)
    {
        clientUsage.Statut = false;
    }
}
```

**Avantages :**
- ✅ Conservation de l'historique
- ✅ Possibilité de restaurer le client
- ✅ Pas de perte de données
- ✅ Soft delete des ClientUsage associés

**Réponse de l'endpoint :**
```json
{
  "message": "Client désactivé avec succès (soft delete)",
  "clientId": 123,
  "note": "Le client et ses ClientUsage ont été désactivés. Les données sont conservées pour l'historique."
}
```

---

## 📊 Exemples d'Utilisation

### Exemple 1 : Mettre à jour le client sans modifier les usages

```http
PUT /api/Client/123
Content-Type: application/json

{
  "nomClient": "Nouveau Nom",
  "telephone": "+221 77 123 4567"
}
```

**Résultat :** Seuls les champs fournis sont mis à jour. Les usages restent inchangés.

---

### Exemple 2 : Mettre à jour le client ET ses usages

```http
PUT /api/Client/123
Content-Type: application/json

{
  "nomClient": "Nouveau Nom",
  "telephone": "+221 77 123 4567",
  "usages": [
    {
      "libelleUsage": "Résidentiel",
      "nombreBatiment": 3,
      "statut": true
    },
    {
      "libelleUsage": "Commercial",
      "nombreBatiment": 1,
      "statut": true
    }
  ]
}
```

**Résultat :**
- Le client est mis à jour
- Les usages fournis remplacent tous les usages existants
- Si un usage n'existe pas encore, il est créé
- Si un usage existant n'est plus dans la liste, il est supprimé (soft delete si factures liées)

---

### Exemple 3 : Supprimer un client

```http
DELETE /api/Client/123
```

**Scénario A : Pas de dépendances**
```json
{
  "message": "Client désactivé avec succès (soft delete)",
  "clientId": 123,
  "note": "Le client et ses ClientUsage ont été désactivés. Les données sont conservées pour l'historique."
}
```

**Scénario B : Des dépendances existent**
```json
{
  "message": "Impossible de supprimer ce client car des données sont liées.",
  "details": {
    "hasClientFactures": true,
    "hasPaiements": true,
    "hasFactures": false
  },
  "note": "Le client sera désactivé (soft delete) au lieu d'être supprimé."
}
```

---

## 🔍 Détails Techniques

### Gestion des ClientUsage dans UpdateWithUsagesAsync

1. **Validation des usages**
   - Vérifie que chaque `LibelleUsage` existe dans la table `Usages`
   - Convertit les libellés en `IdUsage`

2. **Mise à jour des ClientUsage existants**
   - Si un `ClientUsage` existe déjà pour un usage fourni : met à jour `nombreBatiment` et `Statut`

3. **Création de nouveaux ClientUsage**
   - Si un usage fourni n'a pas de `ClientUsage` existant : crée une nouvelle relation

4. **Suppression des ClientUsage**
   - Si un `ClientUsage` existant n'est plus dans la liste fournie :
     - **Soft delete** si des factures sont liées à cet usage
     - **Hard delete** sinon

### Transaction

Toutes les opérations sont exécutées dans une transaction :
- Utilise `_context.Database.CreateExecutionStrategy()` pour compatibilité MySQL
- Rollback automatique en cas d'erreur
- Cohérence garantie des données

---

## ✅ Checklist de Validation

### PUT /api/Client/{id}

- [x] DTO `UpdateClientWithUsagesDto` créé
- [x] Méthode `UpdateWithUsagesAsync` implémentée
- [x] Endpoint modifié pour accepter le nouveau DTO
- [x] Gestion des usages (création, mise à jour, suppression)
- [x] Validation des usages
- [x] Transactionnelle
- [x] Synchronisation avec Utilisateurs
- [x] Gestion des erreurs
- [x] Code compile sans erreurs

### DELETE /api/Client/{id}

- [x] Validation des dépendances (ClientFacture, Paiements, Factures)
- [x] Soft delete implémenté
- [x] Soft delete des ClientUsage associés
- [x] Réponse informative avec détails
- [x] Code compile sans erreurs

---

## 🔄 Changements Rétrocompatibles

### PUT /api/Client/{id}

**Avant :**
- Acceptait `Client` directement
- Ne gérait pas les usages

**Après :**
- Accepte `UpdateClientWithUsagesDto`
- Si `Usages` n'est pas fourni, comportement identique à avant
- **⚠️ Breaking change :** Le format de la requête a changé

**Migration :**
Les clients frontend doivent adapter leurs appels pour utiliser le nouveau DTO.

### DELETE /api/Client/{id}

**Avant :**
- Hard delete (suppression physique)
- Pas de validation des dépendances
- Retournait `NoContent()`

**Après :**
- Soft delete (désactivation)
- Validation des dépendances
- Retourne `Ok()` avec informations
- **⚠️ Breaking change :** Le comportement a changé (soft delete au lieu de hard delete)

**Migration :**
Les clients frontend doivent s'adapter au nouveau comportement (soft delete) et à la nouvelle réponse.

---

## 📝 Notes Importantes

1. **PUT avec usages :** Si `Usages` est fourni, **tous** les usages existants sont remplacés par ceux fournis. Pour ajouter/modifier un seul usage, utiliser les endpoints dédiés :
   - `POST /api/Client/{id}/usages/{idUsage}`
   - `DELETE /api/Client/{id}/usages/{idUsage}`
   - `PUT /api/ClientUsage/{id}`

2. **Soft delete :** Les clients désactivés ne sont plus visibles dans les listes normales (filtrés par `Statut = true`), mais les données sont conservées pour l'historique.

3. **Validation des dépendances :** Si des dépendances existent, l'endpoint DELETE retourne une erreur `400 Bad Request` avec les détails. Le client n'est **pas** désactivé dans ce cas.

---

**Date d'implémentation :** 2025-01-05  
**Version :** 1.0.0
