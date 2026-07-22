# Guide de Test - Factures Impayées par Société

## ⚠️ IMPORTANT : Redémarrage Requis

**L'application doit être redémarrée** pour que les nouveaux endpoints soient disponibles.

## Endpoints Disponibles

1. **Liste complète** : `GET /api/Paiement/societe/{idSociete}/factureImpayee`
2. **Liste paginée** : `GET /api/Paiement/societe/{idSociete}/paged/factureImpayee`

## Test Rapide (après redémarrage)

### Option 1 : Script Automatisé

```bash
./scripts/test_factures_impayees_societe.sh 1
```

### Option 2 : Commandes curl Manuelles

```bash
# 1. Authentification
TOKEN=$(curl -k -s -X POST "https://localhost:7110/api/Utilisateur/authentifier" \
  -H "Content-Type: application/json" \
  -d '{"emailOuTelephone":"admin@kenergie.cd","motDePasse":"Admin"}' \
  | grep -o '"accessToken":"[^"]*' | cut -d'"' -f4)

# 2. Liste complète
curl -k -X GET "https://localhost:7110/api/Paiement/societe/1/factureImpayee" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" | python3 -m json.tool

# 3. Liste paginée
curl -k -X GET "https://localhost:7110/api/Paiement/societe/1/paged/factureImpayee" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" | python3 -m json.tool
```

## Format de Réponse

### Liste Complète

```json
[
  {
    "idFacture": 1,
    "numeroFacture": "0228",
    "dateEmission": "2025-12-07T00:00:00",
    "moisEmission": 12,
    "anneesEmission": 2025,
    "montantTotal": 8000.00,
    "montantPaye": 0.00,
    "montantDu": 8000.00,
    "joursRetard": 2,
    "nomCategorie": "Domestic"
  }
]
```

### Liste Paginée

```json
{
  "data": [
    {
      "idFacture": 1,
      "numeroFacture": "0228",
      "dateEmission": "2025-12-07T00:00:00",
      "moisEmission": 12,
      "anneesEmission": 2025,
      "montantTotal": 8000.00,
      "montantPaye": 0.00,
      "montantDu": 8000.00,
      "joursRetard": 2,
      "nomCategorie": "Domestic"
    }
  ],
  "totalCount": 1,
  "pageNumber": 1,
  "pageSize": 20,
  "totalPages": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

## Paramètres de Pagination

- `pageNumber` : Numéro de page (défaut: 1)
- `pageSize` : Taille de page (défaut: 20, max: 100)
- `searchTerm` : Recherche sur `numeroFacture` ou `nomCategorie`
- `sortBy` : `MontantDu`, `MontantTotal`, `DateEmission`, `NumeroFacture`
- `sortDescending` : `true` ou `false` (défaut: `false`)

## Exemples d'Utilisation

### Recherche
```bash
curl -k -X GET "https://localhost:7110/api/Paiement/societe/1/paged/factureImpayee?searchTerm=0228" \
  -H "Authorization: Bearer $TOKEN"
```

### Tri par montant dû (décroissant)
```bash
curl -k -X GET "https://localhost:7110/api/Paiement/societe/1/paged/factureImpayee?sortBy=MontantDu&sortDescending=true" \
  -H "Authorization: Bearer $TOKEN"
```

### Pagination
```bash
curl -k -X GET "https://localhost:7110/api/Paiement/societe/1/paged/factureImpayee?pageNumber=1&pageSize=10" \
  -H "Authorization: Bearer $TOKEN"
```

## Codes de Réponse

- `200 OK` : Succès
- `401 Unauthorized` : Token invalide
- `403 Forbidden` : Rôle insuffisant
- `404 Not Found` : **Route non trouvée (redémarrer l'application)**
- `500 Internal Server Error` : Erreur serveur

