-- ============================================================================
-- Script de diagnostic des permissions en production
-- Date : 2025-12-15
-- Description : Vérifie si les permissions PlainteClient sont présentes et assignées
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
        WHEN COUNT(*) = 5 THEN '✅ Toutes les permissions PlainteClient existent'
        ELSE CONCAT('❌ Manque ', 5 - COUNT(*), ' permission(s) PlainteClient')
    END as Statut,
    COUNT(*) as Permissions_Trouvees,
    5 as Permissions_Attendues
FROM Permissions
WHERE Categorie = 'PlainteClient';

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
-- 2. VÉRIFICATION DES ASSIGNATIONS AUX RÔLES
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '🔗 VÉRIFICATION DES ASSIGNATIONS AUX RÔLES' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Vérifier les assignations par rôle
SELECT 
    r.Nom as Role,
    COUNT(rp.IdPermission) as Permissions_PlainteClient_Assignees,
    CASE 
        WHEN r.Nom = 'Super-Admin' THEN 5
        WHEN r.Nom = 'Admin' THEN 5
        WHEN r.Nom = 'Gerant' THEN 5
        WHEN r.Nom = 'Technicien' THEN 5
        WHEN r.Nom = 'Client' THEN 3
        ELSE 0
    END as Permissions_Attendues,
    CASE 
        WHEN r.Nom = 'Super-Admin' AND COUNT(rp.IdPermission) = 5 THEN '✅'
        WHEN r.Nom = 'Admin' AND COUNT(rp.IdPermission) = 5 THEN '✅'
        WHEN r.Nom = 'Gerant' AND COUNT(rp.IdPermission) = 5 THEN '✅'
        WHEN r.Nom = 'Technicien' AND COUNT(rp.IdPermission) = 5 THEN '✅'
        WHEN r.Nom = 'Client' AND COUNT(rp.IdPermission) = 3 THEN '✅'
        ELSE '❌'
    END as Statut
FROM Roles r
LEFT JOIN RolePermissions rp ON r.IdRole = rp.IdRole
LEFT JOIN Permissions p ON rp.IdPermission = p.IdPermission AND p.Categorie = 'PlainteClient'
WHERE r.Nom IN ('Super-Admin', 'Admin', 'Gerant', 'Technicien', 'Client')
GROUP BY r.IdRole, r.Nom
ORDER BY r.Nom;

-- Détail des permissions assignées par rôle
SELECT 
    r.Nom as Role,
    p.Nom as Permission,
    p.Action,
    rp.DateAttribution
FROM Roles r
INNER JOIN RolePermissions rp ON r.IdRole = rp.IdRole
INNER JOIN Permissions p ON rp.IdPermission = p.IdPermission
WHERE p.Categorie = 'PlainteClient'
ORDER BY r.Nom, p.Action;

-- ============================================================================
-- 3. VÉRIFICATION D'UN UTILISATEUR SPÉCIFIQUE
-- ============================================================================

-- Remplacez @user_id par l'ID de l'utilisateur qui a le problème
-- Ou utilisez l'email/téléphone pour trouver l'ID

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '👤 VÉRIFICATION D''UN UTILISATEUR' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Exemple : Vérifier l'utilisateur avec ID 4 (remplacez par l'ID réel)
SET @user_id = 4; -- ⚠️ MODIFIEZ CETTE VALEUR

SELECT 
    u.IdUtilisateur,
    u.Nom,
    u.Email,
    u.Telephone,
    r.Nom as Role,
    p.Nom as Permission_PlainteClient,
    CASE 
        WHEN p.Nom = 'PlainteClient.ReadAll' THEN '✅ Permission requise pour GET /api/PlainteClient'
        ELSE ''
    END as Note
FROM Utilisateurs u
INNER JOIN UserRoles ur ON u.IdUtilisateur = ur.IdUtilisateur AND ur.Statut = 1
INNER JOIN Roles r ON ur.IdRole = r.IdRole
INNER JOIN RolePermissions rp ON r.IdRole = rp.IdRole
INNER JOIN Permissions p ON rp.IdPermission = p.IdPermission
WHERE u.IdUtilisateur = @user_id
  AND p.Categorie = 'PlainteClient'
ORDER BY p.Action;

-- Vérifier si l'utilisateur a la permission PlainteClient.ReadAll
SELECT 
    u.IdUtilisateur,
    u.Nom,
    CASE 
        WHEN COUNT(CASE WHEN p.Nom = 'PlainteClient.ReadAll' THEN 1 END) > 0 
        THEN '✅ A la permission PlainteClient.ReadAll'
        ELSE '❌ N''a PAS la permission PlainteClient.ReadAll'
    END as Statut_Permission_ReadAll
FROM Utilisateurs u
INNER JOIN UserRoles ur ON u.IdUtilisateur = ur.IdUtilisateur AND ur.Statut = 1
INNER JOIN Roles r ON ur.IdRole = r.IdRole
INNER JOIN RolePermissions rp ON r.IdRole = rp.IdRole
INNER JOIN Permissions p ON rp.IdPermission = p.IdPermission
WHERE u.IdUtilisateur = @user_id
  AND p.Nom = 'PlainteClient.ReadAll'
GROUP BY u.IdUtilisateur, u.Nom;

-- ============================================================================
-- 4. RÉSUMÉ ET RECOMMANDATIONS
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '📊 RÉSUMÉ' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Compter les permissions PlainteClient
SELECT 
    'Permissions PlainteClient' as Type,
    COUNT(*) as Nombre
FROM Permissions
WHERE Categorie = 'PlainteClient';

-- Compter les assignations
SELECT 
    'Assignations PlainteClient aux rôles' as Type,
    COUNT(*) as Nombre
FROM RolePermissions rp
INNER JOIN Permissions p ON rp.IdPermission = p.IdPermission
WHERE p.Categorie = 'PlainteClient';

-- ============================================================================
-- 5. COMMANDES DE CORRECTION (À EXÉCUTER SI NÉCESSAIRE)
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '🔧 ACTIONS RECOMMANDÉES' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

SELECT 
    'Si des permissions manquent, exécutez:' as Action,
    'Scripts/add_permissions_new_entities.sql' as Script;

SELECT 
    'Si des assignations manquent, exécutez:' as Action,
    'La partie 4 du script add_permissions_new_entities.sql' as Script;

-- ============================================================================
-- FIN DU SCRIPT DE DIAGNOSTIC
-- ============================================================================

