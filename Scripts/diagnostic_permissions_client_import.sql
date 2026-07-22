-- ============================================================================
-- Script de diagnostic des permissions Client pour l'import Excel
-- Date : 2025-12-16
-- Description : Vérifie si les permissions Client sont présentes et assignées
--               aux rôles Gerant, Financier et Caissier
-- ============================================================================

USE `FactureNormaliseeRDC`;

-- ============================================================================
-- 1. VÉRIFICATION DES PERMISSIONS Client
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '📋 VÉRIFICATION DES PERMISSIONS Client' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Vérifier si les permissions existent
SELECT 
    CASE 
        WHEN COUNT(*) >= 2 THEN CONCAT('✅ Les permissions Client nécessaires existent (', COUNT(*), ' trouvée(s))')
        ELSE CONCAT('❌ Manque ', 2 - COUNT(*), ' permission(s) Client')
    END as Statut,
    COUNT(*) as Permissions_Trouvees,
    2 as Permissions_Attendues
FROM Permissions
WHERE Categorie = 'Client' 
  AND Nom IN ('Client.Create', 'Client.ReadAll');

-- Lister les permissions Client
SELECT 
    IdPermission,
    Nom,
    Categorie,
    Action,
    Description,
    Statut,
    DateCreation
FROM Permissions
WHERE Categorie = 'Client'
ORDER BY Action;

-- ============================================================================
-- 2. VÉRIFICATION DES ASSIGNATIONS AUX RÔLES
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '👥 VÉRIFICATION DES ASSIGNATIONS AUX RÔLES' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Résumé des permissions assignées par rôle
SELECT 
    r.Nom as Role,
    COUNT(rp.IdPermission) as Permissions_Client_Assignees,
    CASE 
        WHEN COUNT(rp.IdPermission) >= 2 THEN '✅ Toutes les permissions assignées'
        WHEN COUNT(rp.IdPermission) > 0 THEN CONCAT('⚠️ ', COUNT(rp.IdPermission), ' permission(s) assignée(s) sur 2')
        ELSE '❌ Aucune permission assignée'
    END as Statut
FROM Roles r
LEFT JOIN RolePermissions rp ON r.IdRole = rp.IdRole
LEFT JOIN Permissions p ON rp.IdPermission = p.IdPermission 
    AND p.Categorie = 'Client' 
    AND p.Nom IN ('Client.Create', 'Client.ReadAll')
WHERE r.Nom IN ('Gerant', 'Financier', 'Caissier', 'Admin', 'Super-Admin')
GROUP BY r.IdRole, r.Nom
ORDER BY r.Nom;

-- Détail des permissions assignées par rôle
SELECT 
    '📋 Détail des permissions assignées par rôle:' as Info;
SELECT 
    r.Nom as Role,
    p.Nom as Permission_Client,
    p.Action,
    rp.DateAttribution,
    CASE 
        WHEN p.Nom = 'Client.Create' THEN '✅ Permission requise pour POST /api/Client/bulk-excel'
        WHEN p.Nom = 'Client.ReadAll' THEN '✅ Permission requise pour GET /api/Client/template-excel'
        ELSE ''
    END as Usage
FROM Roles r
INNER JOIN RolePermissions rp ON r.IdRole = rp.IdRole
INNER JOIN Permissions p ON rp.IdPermission = p.IdPermission
WHERE r.Nom IN ('Gerant', 'Financier', 'Caissier', 'Admin', 'Super-Admin')
  AND p.Categorie = 'Client'
  AND p.Nom IN ('Client.Create', 'Client.ReadAll')
ORDER BY r.Nom, p.Action;

-- ============================================================================
-- 3. VÉRIFICATION POUR UN UTILISATEUR SPÉCIFIQUE (Optionnel)
-- ============================================================================

-- Décommentez et remplacez @user_id par l'ID de l'utilisateur à vérifier
-- SET @user_id = 1; -- ID de l'utilisateur à vérifier

-- SELECT 
--     '════════════════════════════════════════════════════════════' as Separator;
-- SELECT 
--     '👤 VÉRIFICATION POUR L''UTILISATEUR' as Title;
-- SELECT 
--     '════════════════════════════════════════════════════════════' as Separator;

-- SELECT 
--     u.IdUtilisateur,
--     u.NomComplet as Nom,
--     u.Email,
--     r.Nom as Role,
--     ur.Statut as Role_Statut
-- FROM Utilisateurs u
-- INNER JOIN UserRoles ur ON u.IdUtilisateur = ur.IdUtilisateur
-- INNER JOIN Roles r ON ur.IdRole = r.IdRole
-- WHERE u.IdUtilisateur = @user_id
--   AND ur.Statut = 1;

-- -- Vérifier si l'utilisateur a la permission Client.Create
-- SELECT 
--     CASE 
--         WHEN COUNT(CASE WHEN p.Nom = 'Client.Create' THEN 1 END) > 0 
--         THEN '✅ A la permission Client.Create'
--         ELSE '❌ N''a PAS la permission Client.Create'
--     END as Statut_Client_Create,
--     CASE 
--         WHEN COUNT(CASE WHEN p.Nom = 'Client.ReadAll' THEN 1 END) > 0 
--         THEN '✅ A la permission Client.ReadAll'
--         ELSE '❌ N''a PAS la permission Client.ReadAll'
--     END as Statut_Client_ReadAll
-- FROM Utilisateurs u
-- INNER JOIN UserRoles ur ON u.IdUtilisateur = ur.IdUtilisateur
-- INNER JOIN Roles r ON ur.IdRole = r.IdRole
-- INNER JOIN RolePermissions rp ON r.IdRole = rp.IdRole
-- INNER JOIN Permissions p ON rp.IdPermission = p.IdPermission
-- WHERE u.IdUtilisateur = @user_id
--   AND ur.Statut = 1
--   AND p.Categorie = 'Client'
--   AND p.Nom IN ('Client.Create', 'Client.ReadAll');

-- ============================================================================
-- 4. STATISTIQUES GLOBALES
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '📊 STATISTIQUES GLOBALES' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Compter les permissions Client
SELECT 
    'Permissions Client' as Type,
    COUNT(*) as Nombre
FROM Permissions
WHERE Categorie = 'Client';

-- Compter les assignations Client aux rôles
SELECT 
    'Assignations Client aux rôles' as Type,
    COUNT(*) as Nombre
FROM RolePermissions rp
INNER JOIN Permissions p ON rp.IdPermission = p.IdPermission
WHERE p.Categorie = 'Client'
  AND p.Nom IN ('Client.Create', 'Client.ReadAll');

-- Compter les utilisateurs avec les rôles ciblés
SELECT 
    'Utilisateurs avec rôles ciblés' as Type,
    COUNT(DISTINCT u.IdUtilisateur) as Nombre
FROM Utilisateurs u
INNER JOIN UserRoles ur ON u.IdUtilisateur = ur.IdUtilisateur
INNER JOIN Roles r ON ur.IdRole = r.IdRole
WHERE r.Nom IN ('Gerant', 'Financier', 'Caissier')
  AND ur.Statut = 1;

-- ============================================================================
-- 5. RECOMMANDATIONS
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
        WHEN (SELECT COUNT(*) FROM Permissions WHERE Categorie = 'Client' AND Nom IN ('Client.Create', 'Client.ReadAll')) < 2
        THEN '⚠️ ACTION REQUISE: Exécutez d''abord le script initialize_permissions_prod.sql pour créer les permissions'
        ELSE '✅ Toutes les permissions Client existent'
    END as Recommendation_1;

-- Vérifier si des assignations manquent
SELECT 
    CASE 
        WHEN (
            SELECT COUNT(*) 
            FROM Roles r
            LEFT JOIN RolePermissions rp ON r.IdRole = rp.IdRole
            LEFT JOIN Permissions p ON rp.IdPermission = p.IdPermission 
                AND p.Categorie = 'Client' 
                AND p.Nom IN ('Client.Create', 'Client.ReadAll')
            WHERE r.Nom IN ('Gerant', 'Financier', 'Caissier')
            GROUP BY r.IdRole
            HAVING COUNT(rp.IdPermission) < 2
        ) > 0
        THEN '⚠️ ACTION REQUISE: Exécutez le script assign_permissions_client_import.sql pour assigner les permissions'
        ELSE '✅ Toutes les permissions sont assignées aux rôles ciblés'
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
    '1. Si des permissions manquent, exécutez initialize_permissions_prod.sql' as Etape;
SELECT 
    '2. Si des assignations manquent, exécutez assign_permissions_client_import.sql' as Etape;
SELECT 
    '3. Les utilisateurs doivent obtenir un nouveau token JWT après l''assignation' as Etape;
