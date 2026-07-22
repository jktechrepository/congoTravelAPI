# Guide de Test - Factures Impayées et Payées

Ce guide explique comment tester les endpoints de factures impayées et payées qui utilisent maintenant la logique basée sur la table `Paiement`.

## Prérequis

1. **Base de données** : Avoir au moins :
   - Une société
   - Une catégorie de clients
   - Un client avec `IdCategorieClient` renseigné
   - Des factures pour cette catégorie avec `Montant` renseigné (montant initial)
   - Des paiements pour ces factures

2. **Authentification** : Obtenir un token JWT valide

## Scénario de Test Complet

### Étape 1 : Créer une facture

```bash
POST /api/Facture
Authorization: Bearer {token}
Content-Type: application/json

{
  "numeroFacture": "FACT-2025-001",
  "montant": 50000.00,
  "dateEmission": "2025-01-15",
  "moisEmission": 1,
  "anneesEmission": 2025,
  "idCategorie": 1,
  "statut": true
}
```

**Important** : Le champ `montant` représente le **montant initial** de la facture (ce qui doit être payé).

### Étape 2 : Créer un paiement partiel

```bash
POST /api/Paiement
Authorization: Bearer {token}
Content-Type: application/json

{
  "idFacture": 1,
  "idClient": 1,
  "montantPaye": 20000.00,
  "datePaiement": "2025-01-20",
  "methodePaiement": "Mobile Money",
  "referenceTransaction": "REF-001",
  "statut": "Validé"
}
```

### Étape 3 : Vérifier que la facture est impayée

```bash
GET /api/Client/1/factures-impayees
Authorization: Bearer {token}
```

**Résultat attendu** :
- La facture doit apparaître dans la liste
- `MontantTotal` = 50000.00
- `MontantPaye` = 20000.00
- `MontantDu` = 30000.00 (50000 - 20000)

### Étape 4 : Créer un paiement complémentaire pour compléter la facture

```bash
POST /api/Paiement
Authorization: Bearer {token}
Content-Type: application/json

{
  "idFacture": 1,
  "idClient": 1,
  "montantPaye": 30000.00,
  "datePaiement": "2025-01-25",
  "methodePaiement": "Espèces",
  "referenceTransaction": "REF-002",
  "statut": "Validé"
}
```

### Étape 5 : Vérifier que la facture est maintenant payée

```bash
GET /api/Client/1/factures-payees/paged?pageNumber=1&pageSize=10
Authorization: Bearer {token}
```

**Résultat attendu** :
- La facture doit apparaître dans la liste des factures payées
- `MontantTotal` = 50000.00
- `MontantPaye` = 50000.00 (20000 + 30000)
- `DatePaiementComplet` = date du dernier paiement

### Étape 6 : Vérifier que la facture n'apparaît plus dans les impayées

```bash
GET /api/Client/1/factures-impayees
Authorization: Bearer {token}
```

**Résultat attendu** :
- La facture ne doit **plus** apparaître dans la liste des factures impayées

## Endpoints à Tester

### 1. Factures Impayées (non paginé)

```bash
GET /api/Client/{idClient}/factures-impayees
Authorization: Bearer {token}
```

**Cas de test** :
- Client avec factures impayées → doit retourner la liste
- Client sans factures impayées → doit retourner une liste vide
- Client inexistant → doit retourner 404

### 2. Factures Impayées (paginé)

```bash
GET /api/Client/{idClient}/factures-impayees/paged?pageNumber=1&pageSize=10
Authorization: Bearer {token}
```

**Paramètres optionnels** :
- `searchTerm` : recherche par numéro de facture ou nom de catégorie
- `sortBy` : `dateemission`, `montantdu`, `joursretard`, `numerofacture`
- `sortDescending` : `true` ou `false`

**Exemple avec recherche et tri** :
```bash
GET /api/Client/1/factures-impayees/paged?pageNumber=1&pageSize=10&searchTerm=FACT&sortBy=montantdu&sortDescending=true
```

### 3. Factures Payées (paginé)

```bash
GET /api/Client/{idClient}/factures-payees/paged?pageNumber=1&pageSize=10
Authorization: Bearer {token}
```

**Paramètres optionnels** :
- `searchTerm` : recherche par numéro de facture ou nom de catégorie
- `sortBy` : `dateemission`, `datepaiementcomplet`, `montanttotal`, `numerofacture`
- `sortDescending` : `true` ou `false`

### 4. Arriérés d'un Client

```bash
GET /api/Client/{idClient}/arrieres
Authorization: Bearer {token}
```

**Résultat attendu** :
```json
{
  "idClient": 1,
  "nomClient": "John Doe",
  "numeroCompteur": "12345",
  "telephone": "+243900000000",
  "emailClient": "john@example.com",
  "nombreFacturesImpayees": 2,
  "totalArrieres": 75000.00,
  "montantTotalFactures": 100000.00,
  "montantTotalPaye": 25000.00,
  "facturesImpayees": [...]
}
```

### 5. Arriérés Globaux

```bash
GET /api/Client/arrieres/global
Authorization: Bearer {token}
Roles: Super-Admin, Admin, Financier
```

### 6. Clients avec Arriérés

```bash
GET /api/Client/arrieres/clients
Authorization: Bearer {token}
Roles: Super-Admin, Admin, Financier
```

## Scénarios de Test Détaillés

### Scénario 1 : Facture sans paiement

1. Créer une facture avec `montant = 100000`
2. Ne créer aucun paiement
3. Vérifier que la facture apparaît dans les impayées avec :
   - `MontantTotal` = 100000
   - `MontantPaye` = 0
   - `MontantDu` = 100000

### Scénario 2 : Facture partiellement payée

1. Créer une facture avec `montant = 100000`
2. Créer un paiement de `30000` avec `statut = "Validé"`
3. Vérifier que la facture apparaît dans les impayées avec :
   - `MontantTotal` = 100000
   - `MontantPaye` = 30000
   - `MontantDu` = 70000

### Scénario 3 : Facture entièrement payée

1. Créer une facture avec `montant = 100000`
2. Créer un paiement de `100000` avec `statut = "Validé"`
3. Vérifier que :
   - La facture **n'apparaît pas** dans les impayées
   - La facture **apparaît** dans les payées avec `MontantPaye >= MontantTotal`

### Scénario 4 : Facture avec paiement supérieur

1. Créer une facture avec `montant = 100000`
2. Créer un paiement de `120000` avec `statut = "Validé"`
3. Vérifier que la facture apparaît dans les payées (surpaiement accepté)

### Scénario 5 : Paiement avec statut "En attente"

1. Créer une facture avec `montant = 100000`
2. Créer un paiement de `50000` avec `statut = "En attente"`
3. Vérifier que :
   - `MontantPaye` = 0 (seuls les paiements avec `statut = "Validé"` sont comptabilisés)
   - `MontantDu` = 100000

### Scénario 6 : Plusieurs paiements pour une même facture

1. Créer une facture avec `montant = 100000`
2. Créer 3 paiements :
   - Paiement 1 : `30000` (Validé)
   - Paiement 2 : `40000` (Validé)
   - Paiement 3 : `30000` (Validé)
3. Vérifier que :
   - `MontantPaye` = 100000 (somme des 3 paiements)
   - La facture apparaît dans les payées

## Vérifications Importantes

### ✅ Vérifier que `Facture.Montant` n'est pas modifié

Après chaque création/modification/suppression de paiement, vérifier que le `montant` de la facture reste inchangé :

```bash
GET /api/Facture/1
Authorization: Bearer {token}
```

Le `montant` doit toujours être égal au montant initial de la facture, **pas** au montant payé.

### ✅ Vérifier le calcul du montant payé

Le montant payé doit être calculé dynamiquement depuis la table `Paiement` :

```sql
SELECT SUM(MontantPaye) 
FROM Paiements 
WHERE IdFacture = 1 
  AND IdClient = 1 
  AND Statut = 'Validé'
```

Ce montant doit correspondre à `MontantPaye` dans les DTOs retournés.

## Commandes cURL Complètes

### Test complet avec cURL

```bash
# 1. Authentification (remplacer avec vos credentials)
TOKEN=$(curl -X POST "http://localhost:5000/api/Auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"Admin123"}' \
  | jq -r '.token')

# 2. Créer une facture
FACTURE_ID=$(curl -X POST "http://localhost:5000/api/Facture" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "numeroFacture": "FACT-TEST-001",
    "montant": 50000.00,
    "dateEmission": "2025-01-15",
    "moisEmission": 1,
    "anneesEmission": 2025,
    "idCategorie": 1,
    "statut": true
  }' | jq -r '.idFacture')

echo "Facture créée : $FACTURE_ID"

# 3. Créer un paiement partiel
curl -X POST "http://localhost:5000/api/Paiement" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"idFacture\": $FACTURE_ID,
    \"idClient\": 1,
    \"montantPaye\": 20000.00,
    \"datePaiement\": \"2025-01-20\",
    \"methodePaiement\": \"Mobile Money\",
    \"referenceTransaction\": \"REF-TEST-001\",
    \"statut\": \"Validé\"
  }"

# 4. Vérifier les factures impayées
curl -X GET "http://localhost:5000/api/Client/1/factures-impayees" \
  -H "Authorization: Bearer $TOKEN" | jq

# 5. Créer un paiement complémentaire
curl -X POST "http://localhost:5000/api/Paiement" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"idFacture\": $FACTURE_ID,
    \"idClient\": 1,
    \"montantPaye\": 30000.00,
    \"datePaiement\": \"2025-01-25\",
    \"methodePaiement\": \"Espèces\",
    \"referenceTransaction\": \"REF-TEST-002\",
    \"statut\": \"Validé\"
  }"

# 6. Vérifier les factures payées
curl -X GET "http://localhost:5000/api/Client/1/factures-payees/paged?pageNumber=1&pageSize=10" \
  -H "Authorization: Bearer $TOKEN" | jq

# 7. Vérifier que la facture n'est plus dans les impayées
curl -X GET "http://localhost:5000/api/Client/1/factures-impayees" \
  -H "Authorization: Bearer $TOKEN" | jq
```

## Tests via Swagger UI

1. Ouvrir Swagger UI : `http://localhost:5000/swagger`
2. S'authentifier via `/api/Auth/login`
3. Copier le token
4. Cliquer sur "Authorize" en haut à droite
5. Entrer : `Bearer {votre_token}`
6. Tester les endpoints dans l'ordre des scénarios ci-dessus

## Points d'Attention

1. **`Facture.Montant` ne doit jamais être modifié** : Il représente le montant initial de la facture
2. **Seuls les paiements avec `Statut = "Validé"` sont comptabilisés**
3. **Le calcul se fait par client** : Chaque client a ses propres paiements pour une facture
4. **Les factures doivent avoir `Statut = true`** pour être prises en compte
5. **Le client doit avoir `IdCategorieClient` renseigné** pour que les factures de sa catégorie soient trouvées

## Résultats Attendus

### Facture Impayée
- `MontantDu > 0`
- `MontantTotal > MontantPaye`
- Apparaît dans `/factures-impayees`
- N'apparaît **pas** dans `/factures-payees`

### Facture Payée
- `MontantPaye >= MontantTotal`
- `MontantDu = 0` (ou non calculé)
- Apparaît dans `/factures-payees`
- N'apparaît **pas** dans `/factures-impayees`

