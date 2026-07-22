# Guide de Test - Factures Impayées par Société

## Endpoints Disponibles

1. **Liste complète** : `GET /api/Paiement/societe/{idSociete}/factureImpayee`
2. **Liste paginée** : `GET /api/Paiement/societe/{idSociete}/paged/factureImpayee`

## Prérequis

- L'application doit être lancée sur `https://localhost:7110`
- Avoir les identifiants de connexion
- Avoir un token d'authentification valide

## Test Automatisé avec le Script

Le script de test automatisé est disponible :

```bash
./scripts/test_factures_impayees_societe.sh [ID_SOCIETE]
```

**Exemple:**
```bash
./scripts/test_factures_impayees_societe.sh 1
```

Le script va :
1. S'authentifier automatiquement
2. Extraire le token
3. Tester l'endpoint liste complète
4. Tester l'endpoint paginé (sans paramètres)
5. Tester l'endpoint paginé avec recherche
6. Tester l'endpoint paginé avec tri
7. Afficher les résultats formatés

## Test Manuel avec curl

### Étape 1: Authentification

```bash
curl -k -X POST "https://localhost:7110/api/Utilisateur/authentifier" \
  -H "Content-Type: application/json" \
  -d '{
    "emailOuTelephone": "admin@kenergie.cd",
    "motDePasse": "Admin"
  }'
```

**Important:** Copiez le `accessToken` de la réponse.

### Étape 2: Liste Complète des Factures Impayées

```bash
curl -k -X GET "https://localhost:7110/api/Paiement/societe/1/factureImpayee" \
  -H "Authorization: Bearer {TOKEN}" \
  -H "Content-Type: application/json"
```

**Réponse attendue:**
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

### Étape 3: Liste Paginée (Sans Paramètres)

```bash
curl -k -X GET "https://localhost:7110/api/Paiement/societe/1/paged/factureImpayee" \
  -H "Authorization: Bearer {TOKEN}" \
  -H "Content-Type: application/json"
```

**Réponse attendue:**
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

### Étape 4: Liste Paginée avec Recherche

```bash
curl -k -X GET "https://localhost:7110/api/Paiement/societe/1/paged/factureImpayee?searchTerm=0228" \
  -H "Authorization: Bearer {TOKEN}" \
  -H "Content-Type: application/json"
```

La recherche fonctionne sur :
- `numeroFacture`
- `nomCategorie`

### Étape 5: Liste Paginée avec Tri

**Tri par montant dû (décroissant):**
```bash
curl -k -X GET "https://localhost:7110/api/Paiement/societe/1/paged/factureImpayee?sortBy=MontantDu&sortDescending=true" \
  -H "Authorization: Bearer {TOKEN}" \
  -H "Content-Type: application/json"
```

**Tri par date d'émission (croissant):**
```bash
curl -k -X GET "https://localhost:7110/api/Paiement/societe/1/paged/factureImpayee?sortBy=DateEmission&sortDescending=false" \
  -H "Authorization: Bearer {TOKEN}" \
  -H "Content-Type: application/json"
```

**Options de tri disponibles:**
- `MontantDu` : Montant restant à payer
- `MontantTotal` : Montant total de la facture
- `DateEmission` : Date d'émission (par défaut)
- `NumeroFacture` : Numéro de facture

### Étape 6: Liste Paginée avec Pagination

```bash
curl -k -X GET "https://localhost:7110/api/Paiement/societe/1/paged/factureImpayee?pageNumber=1&pageSize=10" \
  -H "Authorization: Bearer {TOKEN}" \
  -H "Content-Type: application/json"
```

## Paramètres de Pagination

| Paramètre | Type | Description | Défaut |
|-----------|------|-------------|--------|
| `pageNumber` | int | Numéro de la page (commence à 1) | 1 |
| `pageSize` | int | Nombre d'éléments par page (max 100) | 20 |
| `searchTerm` | string | Terme de recherche | - |
| `sortBy` | string | Propriété de tri | `DateEmission` |
| `sortDescending` | bool | Tri décroissant si true | `false` |

## Logique de Calcul

Une facture est considérée comme **impayée** si :
```
MontantTotal (Facture.Montant) > MontantPaye (Somme des paiements validés)
```

**MontantPaye** = Somme de tous les paiements où :
- `Statut == "Validé"` OU `Statut == "true"` (insensible à la casse)
- Pour la facture concernée
- Tous clients confondus (pas seulement un client spécifique)

**MontantDu** = `MontantTotal - MontantPaye`

## Codes de Réponse

- `200 OK`: Factures impayées récupérées avec succès
- `401 Unauthorized`: Token invalide ou expiré
- `403 Forbidden`: Rôle insuffisant (nécessite Super-Admin, Admin, Gerant, Financier, Caissier ou Technicien)
- `500 Internal Server Error`: Erreur serveur

## Dépannage

### Erreur 401
- Vérifiez que le token est correctement copié
- Vérifiez que le token n'a pas expiré (durée de vie: 120 minutes par défaut)
- Ré-authentifiez-vous si nécessaire

### Erreur 403
- Vérifiez que votre utilisateur a l'un des rôles requis

### Liste vide
- Vérifiez qu'il existe des factures pour la société
- Vérifiez que les factures ont un `Statut == true`
- Vérifiez que les factures appartiennent à des catégories de la société
- Vérifiez que `MontantTotal > MontantPaye` pour au moins une facture

### Erreur 500
- Vérifiez les logs de l'application
- Vérifiez que la base de données est accessible
- Vérifiez que l'ID de société existe

## Exemple de Test Complet

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

# 3. Liste paginée avec tri
curl -k -X GET "https://localhost:7110/api/Paiement/societe/1/paged/factureImpayee?sortBy=MontantDu&sortDescending=true" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" | python3 -m json.tool
```

