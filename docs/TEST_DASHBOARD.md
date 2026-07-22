# Guide de Test - Dashboard API

## Prérequis
- L'application doit être lancée sur `https://localhost:7110`
- Avoir les identifiants de connexion

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

**Réponse attendue:**
```json
{
  "success": true,
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "...",
  "utilisateur": {
    "idSociete": 1,
    ...
  }
}
```

**Important:** Copiez le `accessToken` de la réponse.

### Étape 2: Récupérer les statistiques du Dashboard

Remplacez `{TOKEN}` par le token obtenu à l'étape 1 et `{ID_SOCIETE}` par l'ID de la société (généralement 1).

```bash
curl -k -X GET "https://localhost:7110/api/Dashboard/{ID_SOCIETE}" \
  -H "Authorization: Bearer {TOKEN}" \
  -H "Content-Type: application/json"
```

**Exemple complet:**
```bash
curl -k -X GET "https://localhost:7110/api/Dashboard/1" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  -H "Content-Type: application/json"
```

### Réponse attendue

```json
{
  "totalAgents": 15,
  "totalClientsActifs": 250,
  "paiementsDuMois": 1500000.00,
  "repartitionClientsParCategorie": [
    {
      "idCategorie": 1,
      "nomCategorie": "Domestic",
      "nombreClients": 150,
      "pourcentage": 60.00
    },
    {
      "idCategorie": 2,
      "nomCategorie": "Commercial",
      "nombreClients": 100,
      "pourcentage": 40.00
    }
  ],
  "top5AgentsCollecteurs": [
    {
      "idAgent": 5,
      "matricule": "AGT001",
      "nomComplet": "Jean Dupont",
      "montantCollecte": 500000.00,
      "nombrePaiements": 45
    }
  ]
}
```

## Test Automatisé avec le Script

Un script de test automatisé est disponible :

```bash
./scripts/test_dashboard.sh [ID_SOCIETE]
```

**Exemple:**
```bash
./scripts/test_dashboard.sh 1
```

Le script va :
1. S'authentifier automatiquement
2. Extraire le token
3. Récupérer l'ID de la société depuis la réponse
4. Appeler le endpoint Dashboard
5. Afficher les résultats formatés

## Test avec Postman ou Insomnia

### Collection Postman

1. **Authentification:**
   - Method: `POST`
   - URL: `https://localhost:7110/api/Utilisateur/authentifier`
   - Body (JSON):
     ```json
     {
       "emailOuTelephone": "admin@kenergie.cd",
       "motDePasse": "Admin"
     }
     ```

2. **Dashboard:**
   - Method: `GET`
   - URL: `https://localhost:7110/api/Dashboard/{idSociete}`
   - Headers:
     - `Authorization: Bearer {token}`
     - `Content-Type: application/json`

## Codes de Réponse

- `200 OK`: Statistiques récupérées avec succès
- `401 Unauthorized`: Token invalide ou expiré
- `403 Forbidden`: Rôle insuffisant (nécessite Super-Admin, Admin, Financier ou Caissier)
- `500 Internal Server Error`: Erreur serveur

## Dépannage

### Erreur 401
- Vérifiez que le token est correctement copié
- Vérifiez que le token n'a pas expiré (durée de vie: 120 minutes par défaut)
- Ré-authentifiez-vous si nécessaire

### Erreur 403
- Vérifiez que votre utilisateur a l'un des rôles requis: Super-Admin, Admin, Financier ou Caissier

### Erreur 500
- Vérifiez les logs de l'application
- Vérifiez que la base de données est accessible
- Vérifiez que l'ID de société existe

