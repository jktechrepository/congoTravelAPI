# 📋 Guide de Test : Facturation avec le modèle Usage

Ce guide vous permet de tester la facturation après la refactorisation vers le modèle Usage.

## 🎯 Prérequis

1. **Base de données** : Exécutez le script `test_facturation_usage.sql` pour créer les données de test
2. **Application** : L'application doit être démarrée et accessible
3. **Authentification** : Vous devez être authentifié avec un token JWT valide

## 📊 Données de Test Créées

Le script SQL crée :
- **1 Société** : "Société Test" (IdSociete = 1)
- **1 Catégorie** : "Catégorie Test" (IdCategorie = 1)
- **3 Usages** :
  - Résidentiel (IdUsage = 1)
  - Commercial (IdUsage = 2)
  - Industriel (IdUsage = 3)
- **1 Client** : "Client Test Facturation" (IdClient = 1)
- **2 Relations ClientUsage** :
  - Client 1 → Usage Résidentiel (1 bâtiment)
  - Client 1 → Usage Commercial (2 bâtiments)
- **2 Factures** :
  - Facture résidentielle : 50,000 FCFA
  - Facture commerciale : 100,000 FCFA

## 🧪 Tests à Effectuer

### 1. Vérifier les Usages Disponibles

**Endpoint** : `GET /api/Usage` (si disponible) ou via la base de données

**Vérification** :
- Les 3 usages doivent être présents
- Chaque usage doit être lié à la catégorie

### 2. Vérifier les Relations Client-Usage

**Endpoint** : `GET /api/Client/1/usages`

**Résultat attendu** :
```json
[
  {
    "idUsage": 1,
    "libelle": "Résidentiel",
    "description": "Usage résidentiel pour les particuliers",
    "idCategorieClient": 1
  },
  {
    "idUsage": 2,
    "libelle": "Commercial",
    "description": "Usage commercial pour les entreprises",
    "idCategorieClient": 1
  }
]
```

**Endpoint** : `GET /api/Client/1/usages/details`

**Résultat attendu** :
```json
[
  {
    "idClientUsage": 1,
    "idClient": 1,
    "idUsage": 1,
    "nombreBatiment": 1,
    "dateAttribution": "2024-12-22T..."
  },
  {
    "idClientUsage": 2,
    "idClient": 1,
    "idUsage": 2,
    "nombreBatiment": 2,
    "dateAttribution": "2024-12-22T..."
  }
]
```

### 3. Créer une Nouvelle Facture

**Endpoint** : `POST /api/Facture`

**Headers** :
```
Authorization: Bearer {votre_token}
Content-Type: application/json
```

**Body** :
```json
{
  "montant": 75000.00,
  "dateEmission": "2024-12-22",
  "moisEmission": 12,
  "anneesEmission": 2024,
  "idUsage": 1,
  "statut": true
}
```

**Résultat attendu** :
- La facture est créée avec succès
- Un numéro de facture est généré automatiquement (format : `FAC-XXX-MMYY-####`)
- Le numéro doit contenir les initiales de l'usage (ex: `FAC-RES-1224-0002`)

**Vérifications** :
- ✅ La facture est liée à l'usage (IdUsage = 1)
- ✅ Le numéro de facture est unique
- ✅ La facture peut être récupérée via `GET /api/Facture/{idFacture}`

### 4. Lister les Factures par Usage

**Endpoint** : `GET /api/Facture/usage/1`

**Résultat attendu** :
- Toutes les factures liées à l'usage Résidentiel (IdUsage = 1)
- Les factures doivent inclure les informations de l'usage

### 5. Calculer les Arriérés d'un Client

**Endpoint** : `GET /api/Arrieres/client/1`

**Résultat attendu** :
```json
{
  "totalArrieres": 250000.00,
  "facturesImpayees": [
    {
      "idFacture": 1,
      "numeroFacture": "FAC-RES-1224-0001",
      "montantTotal": 50000.00,
      "montantDu": 50000.00,
      "joursRetard": 21,
      "nomCategorie": "Résidentiel"
    },
    {
      "idFacture": 2,
      "numeroFacture": "FAC-COM-1224-0001",
      "montantTotal": 200000.00,  // 100000 * 2 bâtiments
      "montantDu": 200000.00,
      "joursRetard": 21,
      "nomCategorie": "Commercial"
    }
  ]
}
```

**Points importants** :
- ✅ Le montant de la facture commerciale est multiplié par `nombreBatiment` (2)
- ✅ Les arriérés sont calculés pour tous les usages du client
- ✅ Le champ `nomCategorie` affiche le libellé de l'usage

### 6. Vérifier la Génération du Numéro de Facture

**Test** : Créer plusieurs factures pour le même usage dans le même mois

**Résultat attendu** :
- Les numéros doivent être séquentiels : `FAC-RES-1224-0001`, `FAC-RES-1224-0002`, etc.
- Les initiales doivent correspondre au libellé de l'usage (3 premières lettres)

### 7. Tester la Diffusion des Factures

**Endpoint** : `POST /api/Facture/{idFacture}/societe/{idSociete}/diffusion`

**Vérifications** :
- ✅ La facture est diffusée uniquement aux clients ayant l'usage correspondant
- ✅ Le client de test (IdClient = 1) doit recevoir la notification

## 🔍 Vérifications SQL Directes

### Vérifier les Factures et leurs Usages

```sql
SELECT 
    f.IdFacture,
    f.NumeroFacture,
    f.Montant,
    u.Libelle AS Usage,
    cc.NomCategorie AS Categorie
FROM Factures f
INNER JOIN Usages u ON f.IdUsage = u.IdUsage
INNER JOIN CategorieClients cc ON u.IdCategorieClient = cc.IdCategorie
ORDER BY f.DateCreation DESC;
```

### Vérifier les Clients et leurs Usages

```sql
SELECT 
    c.IdClient,
    c.NomClient,
    u.Libelle AS Usage,
    cu.nombreBatiment
FROM Clients c
INNER JOIN ClientUsages cu ON c.IdClient = cu.IdClient
INNER JOIN Usages u ON cu.IdUsage = u.IdUsage
WHERE c.Statut = true;
```

### Vérifier le Calcul des Arriérés avec nombreBatiment

```sql
SELECT 
    c.NomClient,
    u.Libelle AS Usage,
    cu.nombreBatiment,
    f.NumeroFacture,
    f.Montant AS MontantBase,
    (f.Montant * cu.nombreBatiment) AS MontantTotal,
    COALESCE(SUM(p.MontantPaye), 0) AS MontantPaye,
    ((f.Montant * cu.nombreBatiment) - COALESCE(SUM(p.MontantPaye), 0)) AS MontantDu
FROM Clients c
INNER JOIN ClientUsages cu ON c.IdClient = cu.IdClient
INNER JOIN Usages u ON cu.IdUsage = u.IdUsage
INNER JOIN Factures f ON u.IdUsage = f.IdUsage
LEFT JOIN Paiements p ON f.IdFacture = p.IdFacture 
    AND (p.Statut = 'Validé' OR p.Statut = 'true')
WHERE c.IdClient = 1
  AND f.Statut = true
GROUP BY c.IdClient, c.NomClient, u.Libelle, cu.nombreBatiment, f.IdFacture, f.NumeroFacture, f.Montant;
```

## ⚠️ Points d'Attention

1. **nombreBatiment** : Le montant des factures est multiplié par `nombreBatiment` lors du calcul des arriérés
2. **IdUsage obligatoire** : Toute facture doit avoir un `IdUsage` valide
3. **Diffusion** : Les factures sont diffusées aux clients ayant l'usage correspondant
4. **Numéro de facture** : Le format inclut les initiales de l'usage (3 premières lettres)

## 🐛 Dépannage

### Erreur : "IdUsage invalide"
- Vérifiez que l'usage existe : `SELECT * FROM Usages WHERE IdUsage = {idUsage}`
- Vérifiez que l'usage est lié à une catégorie de la société

### Erreur : "Aucun client trouvé pour cet usage"
- Vérifiez les relations ClientUsage : `SELECT * FROM ClientUsages WHERE IdUsage = {idUsage}`

### Les arriérés ne sont pas calculés correctement
- Vérifiez que `nombreBatiment` est correct dans `ClientUsages`
- Vérifiez que les factures sont liées au bon usage
- Vérifiez que les paiements sont marqués comme "Validé"

## ✅ Checklist de Validation

- [ ] Les usages sont créés et liés aux catégories
- [ ] Les clients peuvent avoir plusieurs usages
- [ ] Les factures sont créées avec un IdUsage valide
- [ ] Le numéro de facture est généré avec les initiales de l'usage
- [ ] Les arriérés sont calculés en multipliant par nombreBatiment
- [ ] Les factures sont récupérables par usage
- [ ] La diffusion fonctionne pour les clients ayant l'usage
