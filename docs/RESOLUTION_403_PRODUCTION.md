# 🔧 Résolution 403 Forbidden en Production

## Problème

L'endpoint `GET /api/PlainteClient` retourne **403 Forbidden** en production mais fonctionne en local.

**Cause probable :** Les permissions `PlainteClient.ReadAll` n'ont pas été ajoutées ou assignées aux rôles en production.

---

## 🔍 Diagnostic

### Étape 1 : Exécuter le script de diagnostic

Exécutez le script SQL de diagnostic sur la base de données de production :

```sql
-- Fichier : Scripts/diagnostic_permissions_production.sql
```

Ce script vérifie :
- ✅ Si les permissions PlainteClient existent
- ✅ Si les permissions sont assignées aux rôles
- ✅ Si votre utilisateur a la permission requise

**Commandes :**
```bash
# Se connecter à la base de données de production
mysql -u root -p FactureNormaliseeRDC

# Exécuter le script
source Scripts/diagnostic_permissions_production.sql
```

---

### Étape 2 : Vérifier via l'API

#### 2.1 Vérifier vos permissions

```http
GET /api/Permission/user/{votre_userId}
Authorization: Bearer {votre_token}
```

**Réponse attendue :** Liste de toutes vos permissions, incluant `PlainteClient.ReadAll`

#### 2.2 Vérifier une permission spécifique

```http
GET /api/Permission/check/PlainteClient.ReadAll
Authorization: Bearer {votre_token}
```

**Réponse attendue :**
```json
{
  "permissionName": "PlainteClient.ReadAll",
  "hasPermission": true
}
```

Si `hasPermission` est `false`, c'est que la permission n'est pas assignée à votre rôle.

---

## ✅ Solution

### Solution 1 : Exécuter le script SQL (Recommandé)

Exécutez le script SQL qui ajoute les permissions et les assigne aux rôles :

```sql
-- Fichier : Scripts/add_permissions_new_entities.sql
```

**Ce script :**
1. ✅ Crée les 5 permissions PlainteClient (idempotent)
2. ✅ Crée les 5 permissions CommunicationCampaign (idempotent)
3. ✅ Crée les 5 permissions PanneSignalement (idempotent)
4. ✅ Assigne les permissions aux rôles appropriés :
   - **Super-Admin** : Toutes les permissions
   - **Admin** : Toutes les permissions
   - **Gerant** : Toutes les permissions
   - **Technicien** : Toutes les permissions PlainteClient et PanneSignalement
   - **Client** : Create, Read, ReadAll pour PlainteClient et PanneSignalement

**Commandes :**
```bash
# Se connecter à la base de données
mysql -u root -p FactureNormaliseeRDC

# Exécuter le script
source Scripts/add_permissions_new_entities.sql
```

---

### Solution 2 : Via l'API (Si vous êtes Super-Admin)

Si vous avez les permissions nécessaires, vous pouvez assigner les permissions via l'API :

#### 2.1 Vérifier que les permissions existent

```http
GET /api/Permission
Authorization: Bearer {votre_token}
```

Cherchez les permissions `PlainteClient.*` dans la réponse.

#### 2.2 Assigner les permissions à un rôle

```http
POST /api/Permission/role/{roleId}/assign-multiple
Authorization: Bearer {votre_token}
Content-Type: application/json

{
  "permissionIds": [id1, id2, id3, id4, id5]
}
```

**Note :** Vous devez d'abord récupérer les IDs des permissions PlainteClient.

---

### Solution 3 : Vérifier manuellement en base de données

#### 3.1 Vérifier si les permissions existent

```sql
SELECT * FROM Permissions 
WHERE Categorie = 'PlainteClient';
```

**Résultat attendu :** 5 permissions
- `PlainteClient.Create`
- `PlainteClient.Read`
- `PlainteClient.ReadAll` ← **Permission requise pour GET /api/PlainteClient**
- `PlainteClient.Update`
- `PlainteClient.Delete`

#### 3.2 Vérifier les assignations aux rôles

```sql
SELECT 
    r.Nom as Role,
    p.Nom as Permission
FROM Roles r
INNER JOIN RolePermissions rp ON r.IdRole = rp.IdRole
INNER JOIN Permissions p ON rp.IdPermission = p.IdPermission
WHERE p.Categorie = 'PlainteClient'
ORDER BY r.Nom, p.Action;
```

**Résultat attendu :** 
- Super-Admin : 5 permissions
- Admin : 5 permissions
- Gerant : 5 permissions
- Technicien : 5 permissions
- Client : 3 permissions (Create, Read, ReadAll)

#### 3.3 Vérifier votre utilisateur

```sql
-- Remplacez @user_id par votre ID utilisateur
SET @user_id = 4;

SELECT 
    u.Nom,
    r.Nom as Role,
    p.Nom as Permission
FROM Utilisateurs u
INNER JOIN UserRoles ur ON u.IdUtilisateur = ur.IdUtilisateur AND ur.Statut = 1
INNER JOIN Roles r ON ur.IdRole = r.IdRole
INNER JOIN RolePermissions rp ON r.IdRole = rp.IdRole
INNER JOIN Permissions p ON rp.IdPermission = p.IdPermission
WHERE u.IdUtilisateur = @user_id
  AND p.Nom = 'PlainteClient.ReadAll';
```

**Résultat attendu :** Au moins une ligne avec `PlainteClient.ReadAll`

---

## 🎯 Checklist de résolution

- [ ] J'ai exécuté le script `diagnostic_permissions_production.sql`
- [ ] J'ai vérifié que les 5 permissions PlainteClient existent
- [ ] J'ai vérifié que les permissions sont assignées aux rôles
- [ ] J'ai vérifié que mon utilisateur a le rôle approprié
- [ ] J'ai vérifié que mon utilisateur a la permission `PlainteClient.ReadAll`
- [ ] J'ai exécuté le script `add_permissions_new_entities.sql` si nécessaire
- [ ] J'ai obtenu un nouveau token JWT après les modifications
- [ ] J'ai testé l'endpoint `GET /api/PlainteClient` et obtenu 200 OK

---

## ⚠️ Notes importantes

### Pourquoi ça fonctionne en local mais pas en production ?

1. **En local :** Le `PermissionSeeder` s'exécute automatiquement au démarrage de l'application (voir `Program.cs`)
2. **En production :** Le seeder ne s'exécute peut-être pas automatiquement, ou les permissions ont été supprimées

### Différence entre 401 et 403

- **401 Unauthorized** : Token invalide ou expiré → Problème d'authentification
- **403 Forbidden** : Token valide mais permissions manquantes → Problème d'autorisation

### Après avoir ajouté les permissions

1. **Obtenez un nouveau token JWT** : Les permissions sont vérifiées au moment de l'authentification
2. **Testez l'endpoint** : Il devrait maintenant fonctionner

---

## 📞 Support

Si le problème persiste après avoir suivi ces étapes :

1. Vérifiez les logs serveur pour plus de détails
2. Vérifiez que la clé secrète JWT est identique entre local et production
3. Vérifiez que le système de permissions est bien activé en production

**Version :** 1.0  
**Dernière mise à jour :** 15 décembre 2025

