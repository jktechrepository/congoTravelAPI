# 📋 Guide d'Assignation des Permissions PlainteClient

## 🎯 Objectif

Assigner les permissions PlainteClient aux rôles **Gerant** et **Technicien** en production pour leur donner accès aux endpoints PlainteClient.

---

## 📊 Permissions concernées

Les 5 permissions PlainteClient qui seront assignées :

1. ✅ `PlainteClient.Create` - Créer une plainte
2. ✅ `PlainteClient.Read` - Voir une plainte
3. ✅ `PlainteClient.ReadAll` - **Voir toutes les plaintes** (requis pour `GET /api/PlainteClient`)
4. ✅ `PlainteClient.Update` - Modifier une plainte
5. ✅ `PlainteClient.Delete` - Supprimer une plainte

---

## 🚀 Solution : Script SQL

### Étape 1 : Exécuter le script SQL

Exécutez le script SQL sur votre base de données de production :

```bash
# Se connecter à la base de données
mysql -u root -p FactureNormaliseeRDC

# Exécuter le script
source Scripts/assign_permissions_gerant_technicien.sql
```

**Ou directement :**
```bash
mysql -u root -p FactureNormaliseeRDC < Scripts/assign_permissions_gerant_technicien.sql
```

### Étape 2 : Vérifier les résultats

Le script affiche automatiquement :
- ✅ Les permissions assignées
- ✅ Les utilisateurs concernés
- ✅ Le statut d'accès pour chaque utilisateur

---

## 📝 Ce que fait le script

1. **Vérifie** que les permissions PlainteClient existent (5 permissions)
2. **Vérifie** que les rôles Gerant et Technicien existent
3. **Assigne** toutes les permissions PlainteClient au rôle Gerant
4. **Assigne** toutes les permissions PlainteClient au rôle Technicien
5. **Affiche** un résumé des assignations
6. **Liste** les utilisateurs concernés avec leur statut d'accès

**Important :** Le script est **idempotent** - il peut être exécuté plusieurs fois sans créer de doublons.

---

## ✅ Vérification après exécution

### Vérification 1 : Via SQL

```sql
-- Vérifier les permissions du rôle Gerant
SELECT 
    r.Nom as Role,
    p.Nom as Permission
FROM Roles r
INNER JOIN RolePermissions rp ON r.IdRole = rp.IdRole
INNER JOIN Permissions p ON rp.IdPermission = p.IdPermission
WHERE r.Nom = 'Gerant'
  AND p.Categorie = 'PlainteClient'
ORDER BY p.Action;

-- Vérifier les permissions du rôle Technicien
SELECT 
    r.Nom as Role,
    p.Nom as Permission
FROM Roles r
INNER JOIN RolePermissions rp ON r.IdRole = rp.IdRole
INNER JOIN Permissions p ON rp.IdPermission = p.IdPermission
WHERE r.Nom = 'Technicien'
  AND p.Categorie = 'PlainteClient'
ORDER BY p.Action;
```

**Résultat attendu :** 5 permissions pour chaque rôle.

---

### Vérification 2 : Via l'API

#### Pour un utilisateur avec le rôle Gerant ou Technicien :

```http
GET /api/Permission/check/PlainteClient.ReadAll
Authorization: Bearer {token_utilisateur_gerant_ou_technicien}
```

**Réponse attendue :**
```json
{
  "permissionName": "PlainteClient.ReadAll",
  "hasPermission": true
}
```

#### Vérifier toutes les permissions d'un utilisateur :

```http
GET /api/Permission/user/{userId}
Authorization: Bearer {token}
```

**Réponse attendue :** Liste incluant toutes les permissions PlainteClient.

---

## 🔄 Actions post-exécution

### 1. Obtenir un nouveau token JWT

**Important :** Les utilisateurs avec les rôles Gerant et Technicien doivent obtenir un **nouveau token JWT** après l'exécution du script.

Les permissions sont vérifiées au moment de l'authentification, donc un nouveau token est nécessaire.

```http
POST /api/Utilisateur/authentifier
Content-Type: application/json

{
  "emailOuTelephone": "gerant@kenergie.cd",
  "motDePasse": "mot_de_passe"
}
```

### 2. Tester les endpoints

Avec le nouveau token, testez les endpoints PlainteClient :

```http
GET /api/PlainteClient
Authorization: Bearer {nouveau_token}
```

**Résultat attendu :** `200 OK` avec la liste des plaintes.

---

## 📊 Comparaison des rôles

| Rôle | Permissions PlainteClient | Endpoints accessibles |
|------|---------------------------|----------------------|
| **Admin** | ✅ Toutes (5) | Tous les endpoints |
| **Gerant** | ✅ Toutes (5) - **Après script** | Tous les endpoints |
| **Technicien** | ✅ Toutes (5) - **Après script** | Tous les endpoints |
| **Client** | ⚠️ Create, Read, ReadAll (3) | Création et consultation de ses propres plaintes |

---

## ⚠️ Notes importantes

### Pourquoi un nouveau token est nécessaire ?

Les permissions sont vérifiées au moment de l'authentification et stockées dans le token JWT. Un token existant ne contient pas les nouvelles permissions.

### Différence entre rôles

- **Admin** : A déjà toutes les permissions (pas besoin du script)
- **Gerant** : Aura toutes les permissions après le script
- **Technicien** : Aura toutes les permissions après le script
- **Client** : A seulement Create, Read, ReadAll (pas Update ni Delete)

### Endpoints concernés

Après l'exécution du script, les utilisateurs Gerant et Technicien pourront accéder à :

- ✅ `GET /api/PlainteClient` - Liste toutes les plaintes
- ✅ `GET /api/PlainteClient/paged` - Liste paginée
- ✅ `GET /api/PlainteClient/en-attente` - Plaintes en attente
- ✅ `GET /api/PlainteClient/assignees/{idAgent}` - Plaintes assignées
- ✅ `GET /api/PlainteClient/{id}` - Détails d'une plainte
- ✅ `POST /api/PlainteClient` - Créer une plainte
- ✅ `PUT /api/PlainteClient/{id}` - Modifier une plainte
- ✅ `PATCH /api/PlainteClient/{id}/assigner` - Assigner un agent
- ✅ `PATCH /api/PlainteClient/{id}/statut` - Changer le statut
- ✅ `PATCH /api/PlainteClient/{id}/resoudre` - Résoudre une plainte
- ✅ `DELETE /api/PlainteClient/{id}` - Supprimer une plainte

---

## 🐛 Dépannage

### Problème : Le script ne trouve pas les permissions

**Solution :** Exécutez d'abord le script `add_permissions_new_entities.sql` pour créer les permissions.

### Problème : Les utilisateurs n'ont toujours pas accès

**Vérifications :**
1. ✅ Le script a été exécuté avec succès
2. ✅ Les permissions sont assignées aux rôles (vérification SQL)
3. ✅ Les utilisateurs ont obtenu un **nouveau token JWT**
4. ✅ Les utilisateurs ont bien le rôle Gerant ou Technicien

### Problème : Erreur 403 persiste

**Vérifications :**
1. Vérifiez que l'utilisateur a le bon rôle :
   ```sql
   SELECT u.Nom, r.Nom as Role
   FROM Utilisateurs u
   INNER JOIN UserRoles ur ON u.IdUtilisateur = ur.IdUtilisateur
   INNER JOIN Roles r ON ur.IdRole = r.IdRole
   WHERE u.IdUtilisateur = {userId};
   ```

2. Vérifiez que le rôle a les permissions :
   ```sql
   SELECT r.Nom, p.Nom
   FROM Roles r
   INNER JOIN RolePermissions rp ON r.IdRole = rp.IdRole
   INNER JOIN Permissions p ON rp.IdPermission = p.IdPermission
   WHERE r.Nom IN ('Gerant', 'Technicien')
     AND p.Categorie = 'PlainteClient';
   ```

---

## 📞 Support

Si le problème persiste après avoir suivi ces étapes :

1. Vérifiez les logs serveur
2. Vérifiez que le système de permissions est bien activé
3. Contactez l'équipe backend

---

**Version :** 1.0  
**Dernière mise à jour :** 15 décembre 2025

