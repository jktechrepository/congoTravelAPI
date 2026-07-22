# 🚀 Guide Rapide - Utiliser Swagger avec JWT

## ⚠️ Erreur 401 Unauthorized : Token invalide

Si vous voyez `401 Unauthorized` avec `Bearer error="invalid_token"`, suivez ces étapes :

---

## ✅ Solution en 3 étapes

### Étape 1 : Obtenir un nouveau token JWT

1. **Dans Swagger**, trouvez l'endpoint :
   ```
   POST /api/Utilisateur/authentifier
   ```

2. **Cliquez sur "Try it out"**

3. **Remplissez le body** :
   ```json
   {
     "emailOuTelephone": "admin@kenergie.cd",
     "motDePasse": "votre_mot_de_passe"
   }
   ```

4. **Cliquez sur "Execute"**

5. **Copiez le token** depuis la réponse :
   ```json
   {
     "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
   }
   ```
   ⚠️ **Important** : Copiez TOUT le token (il est très long, généralement 3 parties séparées par des points)

---

### Étape 2 : Ajouter le token dans Swagger

1. **Cliquez sur le bouton "Authorize"** (cadenas 🔒) en haut à droite de Swagger

2. **Dans la popup qui s'ouvre** :
   - Vous verrez un champ "Value"
   - **Collez le token** que vous avez copié
   - ⚠️ **N'ajoutez PAS "Bearer "** - Swagger l'ajoute automatiquement
   - Le format doit être : `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...` (sans "Bearer ")

3. **Cliquez sur "Authorize"**

4. **Cliquez sur "Close"**

5. **Vérifiez** : Le cadenas devrait maintenant être déverrouillé 🔓

---

### Étape 3 : Tester votre endpoint

1. **Retournez à votre endpoint** (ex: `POST /api/PlainteClient`)

2. **Cliquez sur "Try it out"**

3. **Remplissez le body** :
   ```json
   {
     "idClient": 2,
     "idPanneSignalement": 1,
     "titre": "Test Titre",
     "description": "Test Description",
     "typePanne": "test",
     "niveauImportance": "test",
     "risquesPrincipaux": "test",
     "priorite": "test",
     "estUrgente": true
   }
   ```

4. **Cliquez sur "Execute"**

5. **Résultat attendu** : `201 Created` avec la plainte créée ✅

---

## 🔍 Vérifications

### Vérifier que le token est valide

1. **Allez sur [jwt.io](https://jwt.io)**

2. **Collez votre token** dans le champ "Encoded"

3. **Vérifiez** :
   - ✅ Le token se décode correctement (3 parties)
   - ✅ La date `exp` (expiration) est dans le futur
   - ✅ Les claims sont présents (`sub`, `name`, `role`)

### Vérifier les permissions

Si vous obtenez un `403 Forbidden` après avoir résolu le 401 :

1. **Vérifiez vos permissions** :
   ```
   GET /api/Permission/user/{votre_userId}
   ```

2. **Vérifiez une permission spécifique** :
   ```
   GET /api/Permission/check/PlainteClient.Create
   ```

---

## ⚠️ Erreurs courantes

### ❌ Erreur : Token tronqué
**Symptôme** : Le token semble coupé dans le header  
**Solution** : Copiez le token complet depuis la réponse JSON

### ❌ Erreur : "Bearer " ajouté deux fois
**Symptôme** : Le header contient `Bearer Bearer ...`  
**Solution** : N'ajoutez PAS "Bearer " dans Swagger, il est ajouté automatiquement

### ❌ Erreur : Token expiré
**Symptôme** : Le token était valide mais a expiré  
**Solution** : Obtenez un nouveau token (les tokens expirent après 24h)

### ❌ Erreur : Mauvais format
**Symptôme** : Le token contient des sauts de ligne ou espaces  
**Solution** : Le token doit être une seule ligne sans espaces

---

## 📝 Exemple complet

### 1. Authentification
```http
POST /api/Utilisateur/authentifier
Content-Type: application/json

{
  "emailOuTelephone": "admin@kenergie.cd",
  "motDePasse": "Admin"
}
```

**Réponse :**
```json
{
  "success": true,
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMCIsIm5hbWUiOiJBZG1pbiIsInJvbGUiOiJBZG1pbiIsImV4cCI6MTczNDI4OTg0MCwiaWF0IjoxNzM0MjAzNDQwfQ.signature",
  "expiresIn": 86400
}
```

### 2. Utiliser le token
```http
POST /api/PlainteClient
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "idClient": 2,
  "titre": "Test Titre",
  "description": "Test Description"
}
```

---

## 🎯 Checklist rapide

- [ ] J'ai obtenu un nouveau token via `/api/Utilisateur/authentifier`
- [ ] J'ai copié le token COMPLET (3 parties séparées par des points)
- [ ] J'ai cliqué sur "Authorize" dans Swagger
- [ ] J'ai collé le token SANS "Bearer "
- [ ] J'ai cliqué sur "Authorize" puis "Close"
- [ ] Le cadenas est déverrouillé 🔓
- [ ] J'ai testé l'endpoint et obtenu une réponse 200/201

---

**💡 Astuce** : Si le problème persiste, vérifiez les logs serveur pour plus de détails.

**Version :** 1.0  
**Dernière mise à jour :** 15 décembre 2025

