# Scripts de déploiement en production - CongoTravel API

## 📋 Vue d'ensemble

Ce dossier contient les scripts SQL nécessaires pour créer la base de données CongoTravel en production.

## 📁 Fichiers

- **`create_database_production.sql`** : Script principal de création de toutes les tables
- **`README_PRODUCTION.md`** : Ce fichier (documentation)

## 🚀 Instructions de déploiement

### Prérequis

1. **MariaDB ou MySQL** installé et configuré
2. Accès administrateur à la base de données
3. Client MySQL/MariaDB (mysql, HeidiSQL, phpMyAdmin, etc.)

### Étape 1 : Créer la base de données

```sql
-- Se connecter à MySQL/MariaDB en tant qu'administrateur
mysql -u root -p

-- Créer la base de données
CREATE DATABASE CongoTravelDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- Utiliser la base de données
USE CongoTravelDB;
```

### Étape 2 : Exécuter le script de création des tables

#### Option A : Via la ligne de commande

```bash
mysql -u root -p CongoTravelDB < scripts/create_database_production.sql
```

#### Option B : Via un client graphique

1. Ouvrez le fichier `scripts/create_database_production.sql`
2. Copiez tout le contenu
3. Collez-le dans votre client SQL (HeidiSQL, phpMyAdmin, MySQL Workbench, etc.)
4. Exécutez le script

#### Option C : Via MySQL Workbench

1. Ouvrez MySQL Workbench
2. Connectez-vous à votre serveur
3. File → Open SQL Script → Sélectionnez `create_database_production.sql`
4. Cliquez sur l'icône "Execute" (⚡)

### Étape 3 : Vérifier la création des tables

```sql
USE CongoTravelDB;

-- Lister toutes les tables créées
SHOW TABLES;

-- Vérifier le nombre de tables (devrait être 17)
SELECT COUNT(*) as NombreDeTables 
FROM information_schema.tables 
WHERE table_schema = 'CongoTravelDB';
```

Vous devriez voir les tables suivantes :
- `Agents`
- `AuditLogs`
- `CategorieClients`
- `Clients`
- `Factures`
- `Notifications`
- `PasswordResetTokens`
- `Permissions`
- `RefreshTokens`
- `RolePermissions`
- `Roles`
- `SmsLogs`
- `Societes`
- `UserDevices`
- `UserPermissions`
- `UserRoles`
- `Utilisateurs`

### Étape 4 : Initialiser les données par défaut

Après la création des tables, vous devez initialiser les données par défaut :

#### Option A : Via le script SQL (Recommandé pour la production)

1. Exécutez le script d'initialisation des données :

```bash
mysql -u root -p CongoTravelDB < scripts/initialize_default_data.sql
```

Ou via un client graphique :
1. Ouvrez le fichier `scripts/initialize_default_data.sql`
2. Copiez tout le contenu
3. Collez-le dans votre client SQL
4. Exécutez le script

Ce script créera :
- ✅ Tous les rôles (Super-Admin, Admin, Gerant, Financier, Caissier, Technicien)
- ✅ La société par défaut (CongoTravel)
- ✅ L'agent Manager Général
- ✅ L'utilisateur Super-Admin par défaut
- ✅ L'association UserRole (multi-rôles)

**⚠️ IMPORTANT** : Ce script ne crée PAS les permissions. Vous devez les initialiser via le script SQL (voir Option B) ou via l'API (voir Option C).

#### Option B : Via le script SQL (Recommandé pour la production)

Après avoir exécuté `initialize_default_data.sql`, initialisez les permissions via le script SQL :

```bash
mysql -u root -p CongoTravelDB < scripts/initialize_permissions.sql
```

Ou via un client graphique :
1. Ouvrez le fichier `scripts/initialize_permissions.sql`
2. Copiez tout le contenu
3. Collez-le dans votre client SQL
4. Exécutez le script

Ce script créera :
- ✅ Toutes les permissions (67 permissions)
- ✅ Les associations RolePermissions (permissions assignées aux rôles)

#### Option C : Via l'API (Alternative)

Après avoir exécuté le script SQL, vous pouvez aussi initialiser les permissions via l'API :

1. Démarrez l'application CongoTravel API
2. Appelez l'endpoint d'initialisation :

```bash
curl -X POST https://votre-serveur/api/Init/initialize \
  -H "Content-Type: application/json"
```

Cela créera :
- ✅ Toutes les permissions (67 permissions)
- ✅ Les associations RolePermissions (permissions assignées aux rôles)

**Note** : L'endpoint `/api/Init/initialize` initialise maintenant automatiquement les permissions si elles n'existent pas encore.

#### Option D : Via le code d'initialisation

Si vous avez accès au code source, vous pouvez exécuter directement :

```csharp
// Dans Program.cs ou via une commande
await context.InitializeDefaultDataAsync();
await PermissionSeeder.SeedPermissionsAsync(context);
```

### Étape 5 : Vérifier les données initialisées

```sql
USE CongoTravelDB;

-- Vérifier les rôles créés (devrait être 6)
SELECT * FROM Roles ORDER BY Niveau;

-- Vérifier la société par défaut
SELECT * FROM Societes WHERE Nom = 'CongoTravel';

-- Vérifier l'agent Manager Général
SELECT * FROM Agents WHERE Fonction = 'Manager Général';

-- Vérifier l'utilisateur Super-Admin
SELECT 
    IdUtilisateur, 
    NomComplet, 
    Email, 
    DefaultUsername, 
    IdRole, 
    IdSociete,
    DoitChangerMotDePasse
FROM Utilisateurs 
WHERE Email = 'superadmin@kenergie.cd';

-- Vérifier l'association UserRole
SELECT 
    ur.IdUserRole,
    u.NomComplet as Utilisateur,
    r.Nom as Role,
    ur.IsPrimary,
    ur.Statut
FROM UserRoles ur
INNER JOIN Utilisateurs u ON ur.IdUtilisateur = u.IdUtilisateur
INNER JOIN Roles r ON ur.IdRole = r.IdRole
WHERE u.Email = 'superadmin@kenergie.cd';

-- Vérifier les permissions (après initialisation)
SELECT COUNT(*) as NombrePermissions FROM Permissions;

-- Vérifier les permissions par catégorie
SELECT Categorie, COUNT(*) as Nombre 
FROM Permissions 
GROUP BY Categorie 
ORDER BY Categorie;

-- Vérifier les permissions assignées aux rôles
SELECT 
    r.Nom as Role,
    COUNT(rp.IdRolePermission) as NombrePermissions
FROM Roles r
LEFT JOIN RolePermissions rp ON r.IdRole = rp.IdRole
GROUP BY r.IdRole, r.Nom
ORDER BY r.Niveau;
```

### Étape 6 : Vérifier et corriger les assignations de permissions

Si vous constatez que les permissions ne sont pas retournées lors de l'authentification, vous pouvez utiliser le script de vérification et correction :

```bash
mysql -u root -p CongoTravelDB < scripts/verify_and_fix_permissions.sql
```

Ce script :
- ✅ Vérifie l'état actuel des permissions et assignations
- ✅ Identifie les assignations manquantes
- ✅ Corrige automatiquement les assignations manquantes pour tous les rôles
- ✅ Crée les associations UserRoles manquantes
- ✅ Génère un rapport détaillé

**⚠️ IMPORTANT** : Ce script peut être exécuté plusieurs fois sans erreur. Il utilise `INSERT IGNORE` pour éviter les doublons.

### Étape 7 : Permissions personnalisées (Optionnel)

Si vous souhaitez initialiser des permissions personnalisées pour certains utilisateurs, vous pouvez utiliser le script :

```bash
mysql -u root -p CongoTravelDB < scripts/initialize_user_permissions.sql
```

**⚠️ IMPORTANT** : Ce script contient des **exemples commentés**. Vous devez :
1. Ouvrir le fichier `scripts/initialize_user_permissions.sql`
2. Décommenter et modifier les exemples selon vos besoins
3. Exécuter le script

Les permissions personnalisées permettent de :
- ✅ **Ajouter** des permissions à un utilisateur (en plus de son rôle)
- 🚫 **Retirer** des permissions à un utilisateur (même si son rôle les a)

**Exemples d'utilisation** :
- Un Gerant qui a besoin temporairement de `Facture.Update`
- Un Admin qui ne doit pas pouvoir supprimer des clients
- Un Caissier expérimenté qui peut valider des factures

## 🔐 Informations de connexion par défaut

Après l'initialisation, les identifiants par défaut sont :

- **Email/Username** : `superadmin@kenergie.cd` ou `SuperAdmin`
- **Mot de passe** : `Super-Admin`
- ⚠️ **IMPORTANT** : Le système forcera le changement de mot de passe à la première connexion

## 📊 Structure de la base de données

### Tables principales

1. **Gestion des utilisateurs et rôles**
   - `Roles` : Rôles du système (Super-Admin, Admin, Gerant, etc.)
   - `Permissions` : Permissions disponibles
   - `RolePermissions` : Association rôles-permissions
   - `Utilisateurs` : Utilisateurs du système
   - `UserRoles` : Association utilisateurs-rôles (multi-rôles)
   - `UserPermissions` : Permissions personnalisées par utilisateur

2. **Gestion des entités métier**
   - `Societes` : Sociétés/Entreprises
   - `Agents` : Agents/Employés
   - `CategorieClients` : Catégories de clients
   - `Clients` : Clients
   - `Factures` : Factures

3. **Système de notifications**
   - `Notifications` : Notifications du système
   - `UserDevices` : Appareils utilisateurs (FCM tokens)
   - `SmsLogs` : Historique des SMS envoyés

4. **Sécurité et authentification**
   - `RefreshTokens` : Tokens de rafraîchissement JWT
   - `PasswordResetTokens` : Tokens de réinitialisation de mot de passe
   - `AuditLogs` : Logs d'audit de toutes les actions

## 🔧 Configuration de la chaîne de connexion

Dans votre fichier `appsettings.json` ou `appsettings.Production.json`, configurez :

```json
{
  "ConnectionStrings": {
    "KelasiConnection": "Server=votre-serveur;Database=CongoTravelDB;User=root;Password=votre-mot-de-passe;CharSet=utf8mb4;"
  }
}
```

Pour MariaDB/MySQL :
- **Server** : Adresse du serveur (ex: `localhost`, `192.168.1.100`, `mysql.example.com`)
- **Database** : `CongoTravelDB`
- **User** : Nom d'utilisateur MySQL/MariaDB
- **Password** : Mot de passe MySQL/MariaDB
- **CharSet** : `utf8mb4` (obligatoire pour supporter les emojis et Unicode)

## ⚠️ Notes importantes

### Sécurité

1. **Changez le mot de passe par défaut** du Super-Admin immédiatement après la première connexion
2. **Créez un utilisateur de base de données dédié** avec des permissions limitées (pas root)
3. **Activez les backups automatiques** de la base de données
4. **Configurez le firewall** pour limiter l'accès à la base de données

### Performance

1. **Index** : Les index sont déjà créés dans le script, mais vous pouvez en ajouter selon vos besoins
2. **Partitionnement** : Pour les grandes tables (`AuditLogs`, `SmsLogs`), considérez le partitionnement par date
3. **Optimisation** : Ajustez les paramètres MySQL/MariaDB selon votre charge de travail

### Maintenance

1. **Backups réguliers** : Configurez des backups quotidiens
2. **Nettoyage des logs** : Planifiez le nettoyage des anciens logs (`AuditLogs`, `SmsLogs`, `RefreshTokens` expirés)
3. **Monitoring** : Surveillez l'espace disque et les performances

## 🐛 Dépannage

### Erreur : "Table already exists"

Si vous obtenez cette erreur, cela signifie que les tables existent déjà. Vous avez deux options :

1. **Supprimer et recréer** (⚠️ ATTENTION : Perte de données) :
```sql
DROP DATABASE CongoTravelDB;
CREATE DATABASE CongoTravelDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE CongoTravelDB;
-- Puis réexécutez le script
```

2. **Utiliser IF NOT EXISTS** : Le script utilise déjà `CREATE TABLE IF NOT EXISTS`, donc cette erreur ne devrait pas se produire.

### Erreur : "Foreign key constraint fails"

Vérifiez que toutes les tables sont créées dans le bon ordre. Le script gère automatiquement l'ordre de création.

### Erreur : "Access denied"

Assurez-vous d'avoir les permissions nécessaires :
```sql
GRANT ALL PRIVILEGES ON CongoTravelDB.* TO 'votre-utilisateur'@'localhost';
FLUSH PRIVILEGES;
```

## 📞 Support

Pour toute question ou problème :
1. Vérifiez les logs de l'application
2. Vérifiez les logs MySQL/MariaDB
3. Consultez la documentation de l'API
4. Contactez l'équipe de développement

---

**Dernière mise à jour** : Décembre 2025  
**Version** : 2.0

