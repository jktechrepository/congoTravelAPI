-- ============================================================================
-- Script de diagnostic des permissions PlainteClient pour le rôle Technicien
-- Date : 2025-12-16
-- Description : Vérifie si les permissions PlainteClient sont présentes et assignées
--               au rôle Technicien
-- ============================================================================

USE `FactureNormaliseeRDC`;

-- ============================================================================
-- 1. VÉRIFICATION DES PERMISSIONS PlainteClient
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '📋 VÉRIFICATION DES PERMISSIONS PlainteClient' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Vérifier si les permissions existent
SELECT 
    CASE 
        WHEN COUNT(*) >= 2 THEN CONCAT('✅ Les permissions PlainteClient nécessaires existent (', COUNT(*), ' trouvée(s))')
        ELSE CONCAT('❌ Manque ', 2 - COUNT(*), ' permission(s) PlainteClient')
    END as Statut,
    COUNT(*) as Permissions_Trouvees,
    2 as Permissions_Attendues
FROM Permissions
WHERE Categorie = 'PlainteClient' 
  AND Nom IN ('PlainteClient.Read', 'PlainteClient.ReadAll');

-- Lister les permissions PlainteClient
SELECT 
    IdPermission,
    Nom,
    Categorie,
    Action,
    Description,
    Statut,
    DateCreation
FROM Permissions
WHERE Categorie = 'PlainteClient'
ORDER BY Action;

-- ============================================================================
-- 2. VÉRIFICATION DES ASSIGNATIONS AU RÔLE TECHNICIEN
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '👥 VÉRIFICATION DES ASSIGNATIONS AU RÔLE TECHNICIEN' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Vérifier si le rôle Technicien existe
SELECT 
    CASE 
        WHEN COUNT(*) > 0 THEN CONCAT('✅ Rôle Technicien trouvé (', COUNT(*), ' rôle(s))')
        ELSE '❌ ERREUR: Rôle Technicien non trouvé'
    END as Statut_Role
FROM Roles
WHERE Nom = 'Technicien';

-- Résumé des permissions assignées au rôle Technicien
SELECT 
    r.Nom as Role,
    COUNT(rp.IdPermission) as Permissions_PlainteClient_Assignees,
    CASE 
        WHEN COUNT(rp.IdPermission) >= 2 THEN '✅ Permissions de lecture assignées'
        WHEN COUNT(rp.IdPermission) > 0 THEN CONCAT('⚠️ ', COUNT(rp.IdPermission), ' permission(s) assignée(s) sur 2 minimum')
        ELSE '❌ Aucune permission assignée'
    END as Statut
FROM Roles r
LEFT JOIN RolePermissions rp ON r.IdRole = rp.IdRole
LEFT JOIN Permissions p ON rp.IdPermission = p.IdPermission 
    AND p.Categorie = 'PlainteClient' 
    AND p.Nom IN ('PlainteClient.Read', 'PlainteClient.ReadAll')
WHERE r.Nom = 'Technicien'
GROUP BY r.IdRole, r.Nom;

-- Détail des permissions assignées au rôle Technicien
SELECT 
    '📋 Détail des permissions assignées au rôle Technicien:' as Info;
SELECT 
    r.Nom as Role,
    p.Nom as Permission_PlainteClient,
    p.Action,
    rp.DateAttribution,
    CASE 
        WHEN p.Nom = 'PlainteClient.ReadAll' THEN '✅ Permission requise pour GET /api/PlainteClient'
        WHEN p.Nom = 'PlainteClient.Read' THEN '✅ Permission requise pour GET /api/PlainteClient/{id}'
        ELSE ''
    END as Usage
FROM Roles r
INNER JOIN RolePermissions rp ON r.IdRole = rp.IdRole
INNER JOIN Permissions p ON rp.IdPermission = p.IdPermission
WHERE r.Nom = 'Technicien'
  AND p.Categorie = 'PlainteClient'
  AND p.Nom IN ('PlainteClient.Read', 'PlainteClient.ReadAll')
ORDER BY p.Action;

-- Vérifier toutes les permissions PlainteClient du rôle Technicien (toutes actions)
SELECT 
    '📋 Toutes les permissions PlainteClient du rôle Technicien:' as Info;
SELECT 
    r.Nom as Role,
    p.Nom as Permission,
    p.Action,
    rp.DateAttribution,
    CASE 
        WHEN p.Nom IN ('PlainteClient.Read', 'PlainteClient.ReadAll') THEN '✅ Permission de lecture'
        WHEN p.Nom = 'PlainteClient.Create' THEN '📝 Permission de création'
        WHEN p.Nom = 'PlainteClient.Update' THEN '✏️ Permission de modification'
        WHEN p.Nom = 'PlainteClient.Delete' THEN '🗑️ Permission de suppression'
        ELSE ''
    END as Type_Permission
FROM Roles r
INNER JOIN RolePermissions rp ON r.IdRole = rp.IdRole
INNER JOIN Permissions p ON rp.IdPermission = p.IdPermission
WHERE r.Nom = 'Technicien'
  AND p.Categorie = 'PlainteClient'
ORDER BY p.Action;

-- ============================================================================
-- 3. COMPARAISON AVEC LES AUTRES RÔLES
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '🔍 COMPARAISON AVEC LES AUTRES RÔLES' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Comparer les permissions entre les rôles
SELECT 
    r.Nom as Role,
    COUNT(CASE WHEN p.Nom IN ('PlainteClient.Read', 'PlainteClient.ReadAll') THEN 1 END) as Permissions_Lecture,
    COUNT(CASE WHEN p.Nom = 'PlainteClient.Create' THEN 1 END) as Permission_Creation,
    COUNT(CASE WHEN p.Nom = 'PlainteClient.Update' THEN 1 END) as Permission_Update,
    COUNT(CASE WHEN p.Nom = 'PlainteClient.Delete' THEN 1 END) as Permission_Delete,
    COUNT(rp.IdPermission) as Total_Permissions_PlainteClient
FROM Roles r
LEFT JOIN RolePermissions rp ON r.IdRole = rp.IdRole
LEFT JOIN Permissions p ON rp.IdPermission = p.IdPermission AND p.Categorie = 'PlainteClient'
WHERE r.Nom IN ('Technicien', 'Admin', 'Super-Admin', 'Gerant', 'Agent')
GROUP BY r.IdRole, r.Nom
ORDER BY r.Nom;

-- ============================================================================
-- 4. VÉRIFICATION DES UTILISATEURS
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '👤 UTILISATEURS AVEC LE RÔLE TECHNICIEN' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Lister les utilisateurs avec le rôle Technicien
SELECT 
    u.IdUtilisateur,
    u.NomComplet as Nom,
    u.Email,
    u.Telephone,
    r.Nom as Role,
    ur.Statut as Role_Statut
FROM Utilisateurs u
INNER JOIN UserRoles ur ON u.IdUtilisateur = ur.IdUtilisateur
INNER JOIN Roles r ON ur.IdRole = r.IdRole
WHERE r.Nom = 'Technicien'
  AND ur.Statut = 1
ORDER BY u.NomComplet;

-- Vérifier si les utilisateurs Technicien ont les permissions
SELECT 
    '🔍 Vérification des permissions pour les utilisateurs Technicien:' as Info;
SELECT 
    u.IdUtilisateur,
    u.NomComplet as Nom,
    CASE 
        WHEN EXISTS (
            SELECT 1 
            FROM RolePermissions rp2
            INNER JOIN Permissions p2 ON rp2.IdPermission = p2.IdPermission
            WHERE rp2.IdRole = r.IdRole 
              AND p2.Nom = 'PlainteClient.ReadAll'
        ) THEN '✅ A accès à PlainteClient.ReadAll'
        ELSE '❌ N''a PAS accès à PlainteClient.ReadAll'
    END as Acces_ReadAll,
    CASE 
        WHEN EXISTS (
            SELECT 1 
            FROM RolePermissions rp2
            INNER JOIN Permissions p2 ON rp2.IdPermission = p2.IdPermission
            WHERE rp2.IdRole = r.IdRole 
              AND p2.Nom = 'PlainteClient.Read'
        ) THEN '✅ A accès à PlainteClient.Read'
        ELSE '❌ N''a PAS accès à PlainteClient.Read'
    END as Acces_Read
FROM Utilisateurs u
INNER JOIN UserRoles ur ON u.IdUtilisateur = ur.IdUtilisateur
INNER JOIN Roles r ON ur.IdRole = r.IdRole
WHERE r.Nom = 'Technicien'
  AND ur.Statut = 1
ORDER BY u.NomComplet;

-- ============================================================================
-- 5. STATISTIQUES GLOBALES
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '📊 STATISTIQUES GLOBALES' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Compter les permissions PlainteClient
SELECT 
    'Permissions PlainteClient' as Type,
    COUNT(*) as Nombre
FROM Permissions
WHERE Categorie = 'PlainteClient';

-- Compter les assignations PlainteClient aux rôles
SELECT 
    'Assignations PlainteClient aux rôles' as Type,
    COUNT(*) as Nombre
FROM RolePermissions rp
INNER JOIN Permissions p ON rp.IdPermission = p.IdPermission
WHERE p.Categorie = 'PlainteClient'
  AND p.Nom IN ('PlainteClient.Read', 'PlainteClient.ReadAll');

-- Compter les utilisateurs avec le rôle Technicien
SELECT 
    'Utilisateurs avec rôle Technicien' as Type,
    COUNT(DISTINCT u.IdUtilisateur) as Nombre
FROM Utilisateurs u
INNER JOIN UserRoles ur ON u.IdUtilisateur = ur.IdUtilisateur
INNER JOIN Roles r ON ur.IdRole = r.IdRole
WHERE r.Nom = 'Technicien'
  AND ur.Statut = 1;

-- ============================================================================
-- 6. RECOMMANDATIONS
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '💡 RECOMMANDATIONS' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Vérifier si des permissions manquent
SELECT 
    CASE 
        WHEN (SELECT COUNT(*) FROM Permissions WHERE Categorie = 'PlainteClient' AND Nom IN ('PlainteClient.Read', 'PlainteClient.ReadAll')) < 2
        THEN '⚠️ ACTION REQUISE: Exécutez d''abord le script add_permissions_new_entities.sql pour créer les permissions'
        ELSE '✅ Toutes les permissions PlainteClient existent'
    END as Recommendation_1;

-- Vérifier si des assignations manquent
SELECT 
    CASE 
        WHEN (
            SELECT COUNT(*) 
            FROM Roles r
            LEFT JOIN RolePermissions rp ON r.IdRole = rp.IdRole
            LEFT JOIN Permissions p ON rp.IdPermission = p.IdPermission 
                AND p.Categorie = 'PlainteClient' 
                AND p.Nom IN ('PlainteClient.Read', 'PlainteClient.ReadAll')
            WHERE r.Nom = 'Technicien'
            GROUP BY r.IdRole
            HAVING COUNT(rp.IdPermission) < 2
        ) > 0
        THEN '⚠️ ACTION REQUISE: Exécutez le script assign_permissions_plainte_technicien.sql pour assigner les permissions'
        ELSE '✅ Les permissions sont assignées au rôle Technicien'
    END as Recommendation_2;

-- ============================================================================
-- FIN DU SCRIPT DE DIAGNOSTIC
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '✅ DIAGNOSTIC TERMINÉ' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

SELECT 
    '📝 PROCHAINES ÉTAPES:' as Info;
SELECT 
    '1. Si des permissions manquent, exécutez add_permissions_new_entities.sql' as Etape;
SELECT 
    '2. Si des assignations manquent, exécutez assign_permissions_plainte_technicien.sql' as Etape;
SELECT 
    '3. Les utilisateurs Technicien doivent obtenir un nouveau token JWT après l''assignation' as Etape;
