# Guide de Test - Endpoints Factures Impayées

## Prérequis

1. **Démarrer l'application** :
   ```bash
   cd /Users/mac/Documents/CongoTravelApi
   dotnet run
   ```

2. **Obtenir un token JWT** :
   ```bash
   # Authentification
   curl -k -X POST https://localhost:7110/api/Utilisateur/Authentifier \
     -H "Content-Type: application/json" \
     -d '{
       "email": "admin@kenergie.cd",
       "motDePasse": "Admin"
     }'
   
   # Copier le token de la réponse
   export TOKEN="votre_token_jwt_ici"
   ```

## Endpoints à Tester

### 1. Liste complète des factures impayées d'un client

```bash
# Remplacer {id} par l'ID d'un client existant
curl -k -X GET "https://localhost:7110/api/Client/1/factures-impayees" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json"
```

**Réponse attendue** :
```json
[
  {
    "idFacture": 1,
    "numeroFacture": "FAC-2024-001",
    "dateEmission": "2024-01-15T00:00:00",
    "moisEmission": 1,
    "anneesEmission": 2024,
    "montantTotal": 50000.00,
    "montantPaye": 20000.00,
    "montantDu": 30000.00,
    "joursRetard": 45,
    "nomCategorie": "Résidentiel"
  }
]
```

### 2. Liste paginée des factures impayées

```bash
# Page 1, 10 éléments par page
curl -k -X GET "https://localhost:7110/api/Client/1/factures-impayees/paged?pageNumber=1&pageSize=10" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json"

# Avec tri par date d'émission (décroissant)
curl -k -X GET "https://localhost:7110/api/Client/1/factures-impayees/paged?pageNumber=1&pageSize=10&sortBy=dateEmission&sortDescending=true" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json"

# Avec tri par montant dû (décroissant)
curl -k -X GET "https://localhost:7110/api/Client/1/factures-impayees/paged?pageNumber=1&pageSize=10&sortBy=montantDu&sortDescending=true" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json"

# Avec tri par jours de retard (décroissant)
curl -k -X GET "https://localhost:7110/api/Client/1/factures-impayees/paged?pageNumber=1&pageSize=10&sortBy=joursRetard&sortDescending=true" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json"

# Avec recherche
curl -k -X GET "https://localhost:7110/api/Client/1/factures-impayees/paged?pageNumber=1&pageSize=10&searchTerm=2024" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json"
```

**Réponse attendue** :
```json
{
  "data": [
    {
      "idFacture": 1,
      "numeroFacture": "FAC-2024-001",
      "dateEmission": "2024-01-15T00:00:00",
      "moisEmission": 1,
      "anneesEmission": 2024,
      "montantTotal": 50000.00,
      "montantPaye": 20000.00,
      "montantDu": 30000.00,
      "joursRetard": 45,
      "nomCategorie": "Résidentiel"
    }
  ],
  "totalCount": 5,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

### 3. Factures impayées du client connecté (liste complète)

**Important** : Cet endpoint nécessite qu'un utilisateur avec un `IdClient` soit connecté.

```bash
# S'authentifier avec un compte client
curl -k -X POST https://localhost:7110/api/Utilisateur/Authentifier \
  -H "Content-Type: application/json" \
  -d '{
    "email": "client@example.com",
    "motDePasse": "123456"
  }'

# Copier le nouveau token
export CLIENT_TOKEN="token_du_client"

# Récupérer les factures impayées du client connecté
curl -k -X GET "https://localhost:7110/api/Client/mes-factures-impayees" \
  -H "Authorization: Bearer $CLIENT_TOKEN" \
  -H "Content-Type: application/json"
```

### 4. Factures impayées du client connecté (paginée)

```bash
# Page 1, 10 éléments par page
curl -k -X GET "https://localhost:7110/api/Client/mes-factures-impayees/paged?pageNumber=1&pageSize=10" \
  -H "Authorization: Bearer $CLIENT_TOKEN" \
  -H "Content-Type: application/json"

# Avec tri et recherche
curl -k -X GET "https://localhost:7110/api/Client/mes-factures-impayees/paged?pageNumber=1&pageSize=10&sortBy=montantDu&sortDescending=true&searchTerm=2024" \
  -H "Authorization: Bearer $CLIENT_TOKEN" \
  -H "Content-Type: application/json"
```

### 5. Endpoint arriérés complet (existant)

```bash
# Récupère un résumé complet avec totaux
curl -X GET "http://localhost:5000/api/Client/1/arrieres" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json"
```

**Réponse attendue** :
```json
{
  "idClient": 1,
  "nomClient": "Client Test",
  "numeroCompteur": "CLT-001",
  "telephone": "+2250700000000",
  "emailClient": "client@example.com",
  "nombreFacturesImpayees": 5,
  "totalArrieres": 150000.00,
  "montantTotalFactures": 250000.00,
  "montantTotalPaye": 100000.00,
  "facturesImpayees": [...]
}
```

## Options de Tri Disponibles

- `dateEmission` ou `date` - Par date d'émission
- `montantDu` ou `montant` - Par montant dû
- `joursRetard` ou `retard` - Par jours de retard
- `numeroFacture` ou `numero` - Par numéro de facture

## Codes de Réponse

- **200 OK** : Succès
- **401 Unauthorized** : Token manquant ou invalide
- **403 Forbidden** : L'utilisateur n'est pas associé à un client (pour `/mes-factures-impayees`)
- **404 Not Found** : Client non trouvé

## Notes

- Tous les endpoints nécessitent une authentification JWT
- Les endpoints `/mes-factures-impayees` sont réservés aux utilisateurs ayant un `IdClient`
- La pagination utilise des valeurs par défaut : `pageNumber=1`, `pageSize=20`
- Le tri par défaut est par date d'émission (croissant)

