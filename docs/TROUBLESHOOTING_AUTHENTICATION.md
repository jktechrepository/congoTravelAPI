# 🔧 Guide de dépannage - Authentification JWT

## Problème : 401 Unauthorized avec `Bearer error="invalid_token"`

### Causes possibles

#### 1. ⏰ Token expiré
**Symptôme :** Le token a dépassé sa durée de vie.

**Solution :**
- Vérifier la date d'expiration du token dans [jwt.io](https://jwt.io)
- Obtenir un nouveau token via `POST /api/Utilisateur/authentifier`

**Configuration actuelle :**
- `ValidateLifetime = true` : La validation de l'expiration est activée
- `ClockSkew = TimeSpan.Zero` : Aucune tolérance sur l'expiration (strict)

---

#### 2. 🔑 Clé secrète JWT incorrecte
**Symptôme :** Le token a été généré avec une clé différente de celle utilisée pour la validation.

**Vérification :**
1. Vérifier la clé dans `appsettings.json` :
   ```json
   {
     "Jwt": {
       "SecretKey": "votre-clé-secrète"
     }
   }
   ```
2. Vérifier que la même clé est utilisée pour :
   - Génération du token (`SimpleJwtService`)
   - Validation du token (`Program.cs`)

**Solution :**
- S'assurer que la clé secrète est identique partout
- Si la clé a changé, tous les tokens existants deviennent invalides

---

#### 3. 📝 Token malformé ou tronqué
**Symptôme :** Le token dans le header Authorization est incomplet.

**Vérification :**
- Format attendu : `Authorization: Bearer {token_complet}`
- Le token ne doit pas contenir de sauts de ligne ou d'espaces supplémentaires

**Exemple correct :**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMCIsImh0dHA6Ly9zY2h1bWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWVpZGVudGlmaWVyIjoiMTAiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiQWRtaW4iLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9yb2xlIjoiQWRtaW4iLCJleHAiOjE3MzQyODk4NDAsImlhdCI6MTczNDIwMzQ0MH0.signature
```

**Exemple incorrect :**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9... (tronqué)
Authorization: Bearer  (vide)
Authorization: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9... (manque "Bearer ")
```

---

#### 4. 🔄 Token non synchronisé
**Symptôme :** Le token a été généré sur un serveur et utilisé sur un autre.

**Solution :**
- S'assurer que tous les serveurs utilisent la même clé secrète JWT
- Vérifier la configuration dans `appsettings.json` de chaque environnement

---

## ✅ Solutions étape par étape

### Étape 1 : Vérifier le token dans Swagger

1. **Obtenir un nouveau token :**
   ```
   POST /api/Utilisateur/authentifier
   ```
   ```json
   {
     "emailOuTelephone": "admin@kenergie.cd",
     "motDePasse": "votre_mot_de_passe"
   }
   ```

2. **Copier le token complet** depuis la réponse :
   ```json
   {
     "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
   }
   ```

3. **Dans Swagger :**
   - Cliquer sur le bouton **"Authorize"** (cadenas) en haut à droite
   - Coller le token dans le champ (sans "Bearer ", Swagger l'ajoute automatiquement)
   - Cliquer sur **"Authorize"**
   - Cliquer sur **"Close"**

4. **Tester l'endpoint :**
   - L'endpoint devrait maintenant fonctionner avec le token valide

---

### Étape 2 : Vérifier le token avec jwt.io

1. Aller sur [https://jwt.io](https://jwt.io)
2. Coller le token dans le champ "Encoded"
3. Vérifier :
   - ✅ Le token est bien décodé (3 parties séparées par des points)
   - ✅ La date d'expiration (`exp`) est dans le futur
   - ✅ Les claims sont présents (`sub`, `name`, `role`, etc.)

---

### Étape 3 : Vérifier la configuration JWT

**Fichier :** `appsettings.json`

```json
{
  "Jwt": {
    "SecretKey": "CongoTravel-SecretKey-2025-V1-Ultra-Secure-Key-For-JWT-Token-Generation"
  }
}
```

**Important :** Cette clé doit être identique dans :
- `appsettings.json` (validation)
- `SimpleJwtService.cs` (génération)

---

### Étape 4 : Vérifier les logs serveur

Si le problème persiste, vérifier les logs pour plus de détails :

```bash
# Voir les logs en temps réel
tail -f logs/log-*.txt

# Chercher les erreurs JWT
grep -i "jwt\|token\|unauthorized" logs/log-*.txt
```

---

## 🔍 Vérification rapide

### Test avec curl

```bash
# 1. Obtenir un token
TOKEN=$(curl -X POST 'https://localhost:7110/api/Utilisateur/authentifier' \
  -H 'Content-Type: application/json' \
  -d '{
    "emailOuTelephone": "admin@kenergie.cd",
    "motDePasse": "Admin"
  }' | jq -r '.accessToken')

# 2. Tester l'endpoint avec le token
curl -X GET 'https://localhost:7110/api/PlainteClient' \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Accept: application/json'
```

---

## 📋 Checklist de dépannage

- [ ] Le token est complet (non tronqué)
- [ ] Le token n'est pas expiré (vérifier `exp` dans jwt.io)
- [ ] Le format du header est correct : `Authorization: Bearer {token}`
- [ ] La clé secrète JWT est identique partout
- [ ] Un nouveau token a été obtenu après les modifications
- [ ] Le token a été correctement ajouté dans Swagger (bouton Authorize)
- [ ] Les logs serveur ne montrent pas d'erreurs supplémentaires

---

## 🆘 Si le problème persiste

1. **Vérifier les permissions de l'utilisateur :**
   ```
   GET /api/Permission/user/{userId}
   ```

2. **Vérifier si l'utilisateur a la permission requise :**
   ```
   GET /api/Permission/check/PlainteClient.ReadAll
   ```

3. **Vérifier les rôles de l'utilisateur :**
   - L'utilisateur doit avoir un rôle avec les permissions appropriées
   - Voir `Data/PermissionSeeder.cs` pour les assignations par défaut

4. **Contacter l'équipe backend** avec :
   - Le token JWT (décodé sur jwt.io)
   - Les logs serveur
   - La date/heure de l'erreur

---

## 📝 Notes importantes

- **Durée de vie du token :** Par défaut, les tokens expirent après 24 heures
- **Refresh token :** Utiliser `/api/Utilisateur/refresh-token` pour obtenir un nouveau token sans se reconnecter
- **Environnements :** Chaque environnement (dev, prod) peut avoir une clé secrète différente

---

**Version :** 1.0  
**Dernière mise à jour :** 15 décembre 2025

