# Endpoint de recherche des clients par société

## Description

Endpoints pour lister ou rechercher les **clients ayant au moins une réservation non supprimée** (`Reservations.Statut = true`) dans la société demandée. Un client inscrit sans réservation chez cet opérateur n’apparaît pas.

**Permission requise :** `Client.ReadAll`  
**Scope JWT :** un utilisateur non Super-Admin ne peut interroger que `idSociete` = société de son token (sinon `403`).

## Endpoints disponibles

### Liste non paginée
```
GET /api/Client/societe/{idSociete}
```

### Recherche multi-champs (paginée)
```
GET /api/Client/societe/{idSociete}/paged?searchTerm={searchTerm}&includeInactive={includeInactive}&pageNumber=1&pageSize=20
```

### Recherche multi-champs (liste complète)
```
GET /api/Client/societe/{idSociete}/recherche?searchTerm={searchTerm}&includeInactive={includeInactive}
```

## Paramètres

### Paramètres communs
- **`idSociete`** (int, route, obligatoire) : société cible
- **`searchTerm`** (string, query, optionnel) : recherche multi-champs
- **`includeInactive`** (bool, query, optionnel) : inclure les clients `IsActif = false` (défaut : `false`)

### Pagination (`/paged` uniquement)
- **`pageNumber`** (int, défaut `1`)
- **`pageSize`** (int, défaut `20`, max `100`)
- **`sortBy`** : `NomClient`, `DateCreation`, `IdClient` (défaut : `DateCreation`)
- **`sortDescending`** (bool, défaut `true` — derniers enregistrements en premier)

`GET /api/Client` et `GET /api/Client/paged` utilisent les mêmes paramètres de tri par défaut.

## Fonctionnalités

### Champs de recherche (`searchTerm`)
- **NomClient**
- **AdresseClient**
- **Telephone**
- **EmailClient**
- **GenreClient**

### Filtres automatiques
- `Client.Statut == true` et non soft-deleted
- `IsActif == true` sauf si `includeInactive=true`
- **Au moins une réservation** avec `Reservations.IdSociete = idSociete` et `Reservations.Statut = true` (tous statuts métier : `EN_ATTENTE`, `CONFIRMEE`, `ANNULEE`, etc.)

## Performances

### 🚀 **Optimisations**
- **Requête unique** avec tous les includes nécessaires
- **Indexation** implicite sur les champs de recherche
- **Filtrage côté serveur** pour limiter le volume
- **Tri optimisé** par `DateCreation` descendant

### Cas d'usage
```bash
# Liste clients voyageurs société 1 (ayant réservé)
GET /api/Client/societe/1

# Recherche par nom (clients actifs uniquement)
GET /api/Client/societe/1/recherche?searchTerm=jean

# Recherche paginée
GET /api/Client/societe/1/paged?searchTerm=dupont&pageNumber=2&pageSize=50&sortBy=NomClient&sortDescending=false&includeInactive=true

# Recherche par téléphone
GET /api/Client/societe/1/recherche?searchTerm=0612345678
```

## 🎯 **Exemples d'Utilisation**

### 📋 **Recherche Multi-Champs (Complète)**
```bash
# Recherche par nom
curl -X GET "https://localhost:7110/api/Client/societe/1/recherche?searchTerm=dupont" \
  -H "Authorization: VOTRE_TOKEN_JWT"

# Recherche par CodeCons (AVEC SLASH) - NOUVEAU
curl -X GET "https://localhost:7110/api/Client/societe/1/recherche?searchTerm=ABC%2F12345" \
  -H "Authorization: VOTRE_TOKEN_JWT"

# Recherche TOUS les clients (actifs + inactifs) - NOUVEAU
curl -X GET "https://localhost:7110/api/Client/societe/1/recherche?searchTerm=dupont&includeInactive=true" \
  -H "Authorization: VOTRE_TOKEN_JWT"

# Recherche par adresse (NOUVEAU)
curl -X GET "https://localhost:7110/api/Client/societe/1/recherche?searchTerm=15 rue" \
  -H "Authorization: VOTRE_TOKEN_JWT"

# Recherche par téléphone (NOUVEAU)
curl -X GET "https://localhost:7110/api/Client/societe/1/recherche?searchTerm=0612345678" \
  -H "Authorization: VOTRE_TOKEN_JWT"

# Recherche par email (NOUVEAU)
curl -X GET "https://localhost:7110/api/Client/societe/1/recherche?searchTerm=jean@email.com" \
  -H "Authorization: VOTRE_TOKEN_JWT"
```

### 📄 **Recherche Multi-Champs (Paginée)**
```bash
# Recherche paginée par CodeCons (AVEC SLASH) - NOUVEAU
curl -X GET "https://localhost:7110/api/Client/societe/1/paged?searchTerm=ABC%2F12345&page=1&pageSize=20" \
  -H "Authorization: VOTRE_TOKEN_JWT"

# Recherche paginée par nom avec tri et clients inactifs - NOUVEAU
curl -X GET "https://localhost:7110/api/Client/societe/1/paged?searchTerm=dupont&page=2&pageSize=50&sortBy=NomClient&sortDescending=false&includeInactive=true" \
  -H "Authorization: VOTRE_TOKEN_JWT"

# Recherche paginée simple
curl -X GET "https://localhost:7110/api/Client/societe/1/paged?page=1&pageSize=10" \
  -H "Authorization: VOTRE_TOKEN_JWT"
```

## 📄 **Format de Réponse**

### ✅ **Succès (200 OK)**
```json
[
  {
    "idClient": 1,
    "nomClient": "Jean Dupont",
    "adresseClient": "15 Rue de la Paix",
    "telephone": "0612345678",
    "emailClient": "jean.dupont@email.com",
    "codeCons": "ABC123456",
    "statut": true,
    "isActif": true,
    "dateCreation": "2024-01-15T10:30:00Z",
    "axe": {
      "idAxe": 1,
      "codeAxe": "AXE001",
      "nomAxe": "Axe Nord"
    },
    "clientsUsages": [...]
  }
]
```

### ❌ **Erreurs**
```json
// Société non trouvée
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Not Found",
  "status": 404,
  "detail": "Société avec ID 999 non trouvée"
}

// Non autorisé
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Token JWT invalide ou expiré"
}
```

## 🔐 **Sécurité**

### **Authentification Requise**
- **Bearer Token JWT** obligatoire dans l'en-tête `Authorization`
- **Rôles autorisés** : Tous les rôles authentifiés
- **Auto Bearer** : Le middleware ajoute automatiquement "Bearer" si oublié

### **Audit Trail**
- ✅ Toutes les recherches sont tracées dans `AuditLog`
- Informations enregistrées : utilisateur, société, terme recherché, timestamp

## 🚨 **Points d'Attention**

### **Performance**
- **Recherche multi-champs** : Plus gourmande mais plus flexible
- **Recherche CodeCons exact** : Optimisée et rapide
- **Recherche partielle** : Utilise `LIKE` en base de données

### **Sécurité**
- **Validation** des paramètres d'entrée
- **Protection** contre les injections SQL (via Entity Framework)
- **Limitation** aux sociétés autorisées

## 🔄 **Évolutions Prévues**

### **Phase 2 (Court terme)**
- **Recherche floue** (fuzzy search)
- **Suggestion automatique** de corrections orthographiques
- **Recherche par plage** (dates, montants)

### **Phase 3 (Moyen terme)**
- **Indexation全文** (full-text search)
- **Recherche avancée** avec filtres combinés
- **Cache de recherche** pour améliorer la performance

## Tests recommandés

### Cas de test obligatoires
1. **Recherche vide** : `/recherche` sans `searchTerm` → liste complète des clients **ayant réservé** dans la société
2. **Recherche infructueuse** : `/recherche?searchTerm=inexistant` → liste vide
3. **Pagination** : `/paged?pageNumber=1&pageSize=10` → sous-ensemble du périmètre réservation
4. **Client sans réservation** : absent des 3 routes `/societe/...`
5. **Scope JWT** : token société 1 sur `idSociete=2` → `403`
6. **Non autorisé** : sans token → `401`

Smoke HTTP : [`SMOKE_CLIENT_SOCIETE.http`](../../../SMOKE_CLIENT_SOCIETE.http) à la racine du dépôt.

### Tests de performance
1. **Recherche avec volume élevé** → temps de réponse < 2s
2. **Recherche multi-champs** → vérifier plan SQL (index `Reservations.IdSociete`, `IdClient`)

---

**🎯 Les endpoints de recherche sont maintenant optimisés et prêts à l'emploi !**
