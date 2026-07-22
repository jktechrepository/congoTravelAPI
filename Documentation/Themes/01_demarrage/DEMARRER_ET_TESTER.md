# 🚀 Guide : Démarrer l'Application et Tester les Usages

## 📋 Étapes pour Tester

### Étape 1 : Démarrer l'Application

```bash
cd /Users/mac/Documents/CongoTravelApi
dotnet run
```

**Attendre** que l'application démarre. Vous devriez voir :
```
Now listening on: https://localhost:7110
```

---

### Étape 2 : Lancer le Test

**Dans un nouveau terminal**, exécutez :

```bash
cd /Users/mac/Documents/CongoTravelApi
./Scripts/test_usages_auth_complete.sh
```

Le script va :
1. ✅ Vérifier que l'application est accessible
2. 📝 Vous demander les identifiants d'un utilisateur CLIENT
3. 🔐 Tester l'authentification
4. ✅ Vérifier que `client.usages[]` est présent dans la réponse
5. 📊 Afficher les usages trouvés

---

## 🧪 Test Manuel avec curl

Si vous préférez tester manuellement :

```bash
curl -k -X POST "https://localhost:7110/api/Utilisateur/authentifier" \
  -H "Content-Type: application/json" \
  -d '{
    "emailOuTelephone": "VOTRE_EMAIL_CLIENT",
    "motDePasse": "VOTRE_MOT_DE_PASSE"
  }' | jq '.client.usages'
```

---

## 🧪 Test avec Swagger

1. **Ouvrir** : `https://localhost:7110/swagger`
2. **Trouver** : `POST /api/Utilisateur/authentifier`
3. **Cliquer** : "Try it out"
4. **Remplir** avec les identifiants d'un utilisateur CLIENT
5. **Exécuter** : "Execute"
6. **Vérifier** : Dans la réponse, chercher `client.usages[]`

---

## ⚠️ Important

Pour que le test fonctionne, vous devez avoir :
- ✅ Un utilisateur qui est un **CLIENT** (avec `IdClient` associé)
- ✅ Ce client doit avoir des **usages assignés** dans la table `ClientUsages`
- ✅ Les usages doivent être **actifs** (`Statut = true`)

---

## 🔍 Vérifier les Données dans la Base

Si vous voulez vérifier qu'un client a des usages :

```sql
-- Trouver un client avec des usages
SELECT 
    c.IdClient,
    c.NomClient,
    c.EmailClient,
    COUNT(cu.IdClientUsage) as NombreUsages
FROM Clients c
INNER JOIN ClientUsages cu ON c.IdClient = cu.IdClient
WHERE cu.Statut = 1
GROUP BY c.IdClient, c.NomClient, c.EmailClient
HAVING NombreUsages > 0
LIMIT 5;

-- Voir les usages d'un client spécifique
SELECT 
    c.NomClient,
    u.Libelle as UsageLibelle,
    cu.nombreBatiment,
    cu.Statut
FROM Clients c
INNER JOIN ClientUsages cu ON c.IdClient = cu.IdClient
INNER JOIN Usages u ON cu.IdUsage = u.IdUsage
WHERE c.IdClient = VOTRE_ID_CLIENT
  AND cu.Statut = 1;
```

---

## ✅ Résultat Attendu

Si tout fonctionne, vous devriez voir :

```json
{
  "success": true,
  "accessToken": "...",
  "client": {
    "idClient": 1,
    "nomClient": "...",
    "usages": [
      {
        "idUsage": 1,
        "libelle": "DOMESTIQUE",
        "nombreBatiment": 2,
        "dateAttribution": "2025-01-15T10:00:00",
        "statut": true
      }
    ]
  }
}
```

---

**Prêt à tester !** 🚀
