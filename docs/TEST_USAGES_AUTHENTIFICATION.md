# 🧪 Guide de Test - Usages dans la Réponse d'Authentification

## 📋 Objectif

Vérifier que l'endpoint d'authentification (`POST /api/Utilisateur/authentifier`) retourne maintenant la liste des usages du client dans l'objet `Client` de la réponse.

---

## ✅ Prérequis

1. L'application doit être lancée (généralement sur `https://localhost:7110`)
2. Avoir un utilisateur client avec des usages associés dans la base de données
3. Avoir les identifiants de connexion (email/téléphone et mot de passe)

---

## 🧪 Test 1 : Authentification avec un Client ayant des Usages

### Avec curl

```bash
curl -k -X POST "https://localhost:7110/api/Utilisateur/authentifier" \
  -H "Content-Type: application/json" \
  -d '{
    "emailOuTelephone": "client@example.com",
    "motDePasse": "MotDePasse123"
  }' | jq '.client.usages'
```

### Réponse Attendue

La réponse devrait contenir un objet `client` avec une propriété `usages` :

```json
{
  "success": true,
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "...",
  "utilisateur": { ... },
  "client": {
    "idClient": 1,
    "nomClient": "Kalambayi Jonathan",
    "codeCons": "B/b1/0001",
    "telephone": "+243 825 099 299",
    "emailClient": "kangudjaobed66@gmail.com",
    "usages": [
      {
        "idUsage": 1,
        "libelle": "DOMESTIQUE",
        "nombreBatiment": 2,
        "dateAttribution": "2025-01-15T10:00:00",
        "statut": true
      },
      {
        "idUsage": 2,
        "libelle": "COMMERCIAL",
        "nombreBatiment": 1,
        "dateAttribution": "2025-01-15T10:00:00",
        "statut": true
      }
    ]
  }
}
```

### Vérifications

✅ **Vérifier que `client.usages` existe** :
```bash
curl -k -X POST "https://localhost:7110/api/Utilisateur/authentifier" \
  -H "Content-Type: application/json" \
  -d '{"emailOuTelephone": "client@example.com", "motDePasse": "MotDePasse123"}' \
  | jq 'has("client") and (.client | has("usages"))'
```
**Résultat attendu :** `true`

✅ **Vérifier que `usages` est un tableau** :
```bash
curl -k -X POST "https://localhost:7110/api/Utilisateur/authentifier" \
  -H "Content-Type: application/json" \
  -d '{"emailOuTelephone": "client@example.com", "motDePasse": "MotDePasse123"}' \
  | jq '.client.usages | type'
```
**Résultat attendu :** `"array"`

✅ **Vérifier le nombre d'usages** :
```bash
curl -k -X POST "https://localhost:7110/api/Utilisateur/authentifier" \
  -H "Content-Type: application/json" \
  -d '{"emailOuTelephone": "client@example.com", "motDePasse": "MotDePasse123"}' \
  | jq '.client.usages | length'
```

✅ **Vérifier que chaque usage a les propriétés requises** :
```bash
curl -k -X POST "https://localhost:7110/api/Utilisateur/authentifier" \
  -H "Content-Type: application/json" \
  -d '{"emailOuTelephone": "client@example.com", "motDePasse": "MotDePasse123"}' \
  | jq '.client.usages[] | {idUsage, libelle, nombreBatiment, dateAttribution, statut}'
```

---

## 🧪 Test 2 : Client avec Usages Inactifs

### Objectif

Vérifier que seuls les usages actifs (`statut == true`) sont retournés.

### Préparation

1. Créer un client avec plusieurs usages
2. Désactiver un usage (mettre `Statut = false` dans la table `ClientUsages`)

### Test

```bash
curl -k -X POST "https://localhost:7110/api/Utilisateur/authentifier" \
  -H "Content-Type: application/json" \
  -d '{"emailOuTelephone": "client@example.com", "motDePasse": "MotDePasse123"}' \
  | jq '.client.usages[] | select(.statut == false)'
```

**Résultat attendu :** Aucun résultat (tableau vide), car seuls les usages actifs sont retournés.

---

## 🧪 Test 3 : Client sans Usages

### Objectif

Vérifier qu'un client sans usages retourne une liste vide `[]` et non `null`.

### Test

```bash
curl -k -X POST "https://localhost:7110/api/Utilisateur/authentifier" \
  -H "Content-Type: application/json" \
  -d '{"emailOuTelephone": "client_sans_usage@example.com", "motDePasse": "MotDePasse123"}' \
  | jq '.client.usages'
```

**Résultat attendu :** `[]` (tableau vide)

---

## 🧪 Test 4 : Utilisateur non-Client

### Objectif

Vérifier qu'un utilisateur qui n'est pas un client retourne `client: null`.

### Test

```bash
curl -k -X POST "https://localhost:7110/api/Utilisateur/authentifier" \
  -H "Content-Type: application/json" \
  -d '{"emailOuTelephone": "admin@kenergie.cd", "motDePasse": "Admin"}' \
  | jq '.client'
```

**Résultat attendu :** `null`

---

## 🧪 Test avec Postman

### Étape 1 : Créer une requête

1. **Method :** `POST`
2. **URL :** `https://localhost:7110/api/Utilisateur/authentifier`
3. **Headers :**
   - `Content-Type: application/json`
4. **Body (raw JSON) :**
   ```json
   {
     "emailOuTelephone": "client@example.com",
     "motDePasse": "MotDePasse123"
   }
   ```

### Étape 2 : Exécuter et Vérifier

1. Cliquez sur **Send**
2. Dans la réponse, vérifiez :
   - ✅ `success: true`
   - ✅ `client` existe
   - ✅ `client.usages` existe et est un tableau
   - ✅ Chaque usage contient : `idUsage`, `libelle`, `nombreBatiment`, `dateAttribution`, `statut`

### Étape 3 : Test Script (Postman)

Ajoutez ce script dans l'onglet **Tests** de Postman :

```javascript
pm.test("Client usages are returned", function () {
    var jsonData = pm.response.json();
    
    // Si l'utilisateur est un client
    if (jsonData.client) {
        pm.expect(jsonData.client).to.have.property('usages');
        pm.expect(jsonData.client.usages).to.be.an('array');
        
        // Vérifier que tous les usages ont les propriétés requises
        jsonData.client.usages.forEach(function(usage) {
            pm.expect(usage).to.have.property('idUsage');
            pm.expect(usage).to.have.property('libelle');
            pm.expect(usage).to.have.property('nombreBatiment');
            pm.expect(usage).to.have.property('dateAttribution');
            pm.expect(usage).to.have.property('statut');
            pm.expect(usage.statut).to.be.true; // Seuls les usages actifs
        });
    }
});
```

---

## 🧪 Test avec Swagger

1. **Ouvrir Swagger UI** : `https://localhost:7110/swagger`
2. **Trouver l'endpoint :** `POST /api/Utilisateur/authentifier`
3. **Cliquer sur "Try it out"**
4. **Remplir le body :**
   ```json
   {
     "emailOuTelephone": "client@example.com",
     "motDePasse": "MotDePasse123"
   }
   ```
5. **Cliquer sur "Execute"**
6. **Vérifier la réponse :** L'objet `client` doit contenir `usages[]`

---

## 📊 Script de Test Automatisé

Créez un fichier `test_usages_auth.sh` :

```bash
#!/bin/bash

BASE_URL="https://localhost:7110"
EMAIL="client@example.com"
PASSWORD="MotDePasse123"

echo "🔐 Authentification..."
RESPONSE=$(curl -k -s -X POST "${BASE_URL}/api/Utilisateur/authentifier" \
  -H "Content-Type: application/json" \
  -d "{\"emailOuTelephone\":\"${EMAIL}\",\"motDePasse\":\"${PASSWORD}\"}")

echo "📋 Réponse complète :"
echo "$RESPONSE" | jq '.'

echo ""
echo "✅ Vérifications :"

# Vérifier que client existe
if echo "$RESPONSE" | jq -e '.client' > /dev/null; then
    echo "✅ client existe"
    
    # Vérifier que usages existe
    if echo "$RESPONSE" | jq -e '.client.usages' > /dev/null; then
        echo "✅ client.usages existe"
        
        # Compter les usages
        COUNT=$(echo "$RESPONSE" | jq '.client.usages | length')
        echo "✅ Nombre d'usages : $COUNT"
        
        # Afficher les usages
        echo ""
        echo "📋 Liste des usages :"
        echo "$RESPONSE" | jq '.client.usages[] | {idUsage, libelle, nombreBatiment, statut}'
        
        # Vérifier que tous les usages sont actifs
        INACTIFS=$(echo "$RESPONSE" | jq '[.client.usages[] | select(.statut == false)] | length')
        if [ "$INACTIFS" -eq 0 ]; then
            echo "✅ Tous les usages sont actifs"
        else
            echo "⚠️  Attention : $INACTIFS usage(s) inactif(s) trouvé(s)"
        fi
    else
        echo "❌ client.usages n'existe pas"
    fi
else
    echo "⚠️  L'utilisateur n'est pas un client (client: null)"
fi
```

**Utilisation :**
```bash
chmod +x test_usages_auth.sh
./test_usages_auth.sh
```

---

## 🔍 Vérifications de Performance

### Vérifier le nombre de requêtes SQL

Avec les logs Entity Framework activés, vérifier qu'une seule requête SQL est exécutée pour charger les usages (grâce à `Include` et `ThenInclude`).

**Logs attendus :**
```
✅ Informations Client chargées pour l'utilisateur X - Client: Y (Z) - N usage(s)
```

---

## 📝 Checklist de Test

- [ ] Client avec plusieurs usages actifs → Tous les usages sont retournés
- [ ] Client avec usages inactifs → Seuls les usages actifs sont retournés
- [ ] Client sans usages → Liste vide `[]` (pas `null`)
- [ ] Utilisateur non-client → `client: null`
- [ ] Chaque usage contient : `idUsage`, `libelle`, `nombreBatiment`, `dateAttribution`, `statut`
- [ ] Tous les usages retournés ont `statut: true`
- [ ] Performance : Une seule requête SQL pour charger les usages

---

## 🐛 Dépannage

### Problème : `client.usages` est `null`

**Cause possible :** Le client n'a pas été chargé avec les relations.

**Solution :** Vérifier que le code utilise bien `.Include(c => c.ClientsUsages).ThenInclude(cu => cu.Usage)`

### Problème : Usages inactifs sont retournés

**Cause possible :** Le filtrage par `Statut == true` n'est pas appliqué.

**Solution :** Vérifier que le code utilise `.Where(cu => cu.Statut == true)`

### Problème : `Usage` est `null` dans `ClientUsage`

**Cause possible :** L'`Usage` n'a pas été chargé.

**Solution :** Vérifier que `.ThenInclude(cu => cu.Usage)` est présent

---

**Date de création :** 2025-01-XX  
**Auteur :** Auto (AI Assistant)  
**Statut :** ✅ Prêt pour les tests
