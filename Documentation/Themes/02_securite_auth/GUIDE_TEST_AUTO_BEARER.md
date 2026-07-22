# 🚀 Guide de Test - Middleware AutoBearer

## 🎯 Objectif

Le middleware **AutoBearer** ajoute automatiquement le préfixe "Bearer" aux tokens JWT si vous l'oubliez.

## 📋 Étapes de Test

### 1️⃣ **Démarrer le Serveur**

```bash
dotnet run --urls="https://localhost:7110"
```

### 2️⃣ **Obtenir un Token JWT**

```bash
curl -X POST https://localhost:7110/api/Auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@example.com",
    "motDePasse": "votre_mot_de_passe"
  }'
```

**Réponse attendue :**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiration": "2025-02-09T18:00:00Z",
  "user": { ... }
}
```

### 3️⃣ **Tester l'Authentification**

#### ✅ **Test 1 : Token SANS "Bearer" (NOUVEAU)**
```bash
curl -X GET https://localhost:7110/api/AuthTest/protected \
  -H "Authorization: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

#### ✅ **Test 2 : Token AVEC "Bearer" (CLASSIQUE)**
```bash
curl -X GET https://localhost:7110/api/AuthTest/protected \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

#### ✅ **Test 3 : Endpoint Public**
```bash
curl -X GET https://localhost:7110/api/AuthTest/public
```

### 4️⃣ **Tester l'Export des Clients**

#### 🎯 **Export SANS "Bearer"**
```bash
curl -X GET "https://localhost:7110/api/Client/societe/1/export?IncludeInactive=true" \
  -H "Authorization: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  --output "clients_export.xlsx"
```

#### 🎯 **Export AVEC "Bearer"**
```bash
curl -X GET "https://localhost:7110/api/Client/societe/1/export?IncludeInactive=true" \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  --output "clients_export.xlsx"
```

## 🧪 **Résultats Attendus**

### ✅ **Réussite (200 OK)**
- Les deux formats (avec/sans "Bearer") doivent fonctionner
- Le fichier Excel doit être téléchargé
- Les endpoints protégés doivent retourner les infos utilisateur

### ❌ **Échec (401 Unauthorized)**
- Token invalide ou expiré
- Problème de permissions

## 🔍 **Débogage**

### Vérifier le Middleware
Le middleware ajoute des logs dans la console. Cherchez :
```
AutoBearer: Token formaté automatiquement
```

### Logs Serveur
```bash
# Vérifiez les logs dans la console du serveur
# Cherchez "AutoBearer" ou "Authorization"
```

### Test avec Swagger
1. Allez sur : `https://localhost:7110/swagger`
2. Cliquez sur **"Authorize"**
3. Entrez **SANS** le préfixe "Bearer" :
   ```
   eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
   ```
4. Testez les endpoints

## 📊 **Endpoints de Test Disponibles**

### 🟢 **Endpoints Publics**
- `GET /api/AuthTest/public` - Test de connexion API

### 🔒 **Endpoints Protégés**
- `GET /api/AuthTest/protected` - Test authentification JWT
- `GET /api/AuthTest/permissions` - Vérifier permissions utilisateur
- `GET /api/Client/societe/{idSociete}/export` - Export Excel

## 🎯 **Scénarios de Test**

### Scénario 1 : Développeur Pressé
```bash
# Oublie le préfixe "Bearer" → ÇA MARCHE QUAND MÊME !
curl -H "Authorization: TOKEN_JWT_ICI" ...
```

### Scénario 2 : Utilisation Normale
```bash
# Utilise le format classique → ÇA MARCHE TOUJOURS !
curl -H "Authorization: Bearer TOKEN_JWT_ICI" ...
```

### Scénario 3 : Postman/Insomnia
1. **Headers** → `Authorization` → `TOKEN_JWT_ICI`
2. **OU** → `Authorization` → `Bearer TOKEN_JWT_ICI`
3. Les deux fonctionnent !

## 🔧 **Comment ça Marche**

### Le Middleware AutoBearer :
1. **Intercepte** chaque requête HTTP
2. **Vérifie** si le header `Authorization` existe
3. **Détecte** si le préfixe "Bearer " est manquant
4. **Ajoute** automatiquement le préfixe si nécessaire
5. **Passe** la requête au reste du pipeline

### Code du Middleware :
```csharp
if (!string.IsNullOrWhiteSpace(authHeader) && 
    !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
{
    // Ajout automatique du préfixe "Bearer"
    var newAuthHeader = $"Bearer {authHeader}";
    context.Request.Headers["Authorization"] = new StringValues(newAuthHeader);
}
```

## 🎉 **Avantages**

✅ **Flexibilité** : Plus besoin de se souvenir du préfixe "Bearer"  
✅ **Rétro-compatibilité** : Les anciens clients continuent de fonctionner  
✅ **Simplicité** : Moins d'erreurs de frappe  
✅ **Automatique** : Transparent pour l'utilisateur  

## 🚨 **Important**

- Le middleware **n'altère pas** les tokens valides
- Il **n'ajoute que** le préfixe manquant
- Les **logs** gardent trace des modifications
- **Sécurité** : Aucun impact sur la validation JWT

---

**🎯 Le middleware AutoBearer est maintenant actif et fonctionnel !**
