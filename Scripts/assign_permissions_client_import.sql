-- ============================================================================
-- Script d'assignation des permissions Client pour l'import Excel
-- Date : 2025-12-16
-- Description : Assigne les permissions Client.Create et Client.ReadAll aux rôles 
--               Gerant, Financier et Caissier pour permettre l'import Excel
-- IMPORTANT : Ce script est IDEMPOTENT - peut être exécuté plusieurs fois sans erreur
-- ============================================================================

USE `FactureNormaliseeRDC`;

-- ============================================================================
-- PARTIE 1: VÉRIFICATION PRÉALABLE
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '🔍 VÉRIFICATION PRÉALABLE' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Vérifier que les permissions Client existent
SELECT 
    CASE 
        WHEN COUNT(*) >= 2 THEN CONCAT('✅ Les permissions Client nécessaires existent (', COUNT(*), ' trouvée(s))')
        ELSE CONCAT('❌ ATTENTION: Seulement ', COUNT(*), ' permission(s) Client trouvée(s) sur 2 minimum attendues')
    END as Statut_Permissions
FROM Permissions
WHERE Categorie = 'Client' 
  AND Nom IN ('Client.Create', 'Client.ReadAll');

-- Lister les permissions Client disponibles
SELECT 
    '📋 Permissions Client disponibles:' as Info;
SELECT 
    IdPermission,
    Nom,
    Action,
    Description
FROM Permissions
WHERE Categorie = 'Client'
ORDER BY Action;

-- Vérifier que les rôles existent
SELECT 
    '👥 Rôles ciblés:' as Info;
SELECT 
    IdRole,
    Nom,
    Statut
FROM Roles
WHERE Nom IN ('Gerant', 'Financier', 'Caissier')
ORDER BY Nom;

-- ============================================================================
-- PARTIE 2: ASSIGNATION DES PERMISSIONS AU RÔLE GERANT
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '🔵 ASSIGNATION DES PERMISSIONS AU RÔLE GERANT' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Récupérer l'ID du rôle Gerant
SET @gerant_role_id = (SELECT IdRole FROM Roles WHERE Nom = 'Gerant' LIMIT 1);

-- Vérifier si le rôle existe
SELECT 
    CASE 
        WHEN @gerant_role_id IS NOT NULL THEN CONCAT('✅ Rôle Gerant trouvé (ID: ', @gerant_role_id, ')')
        ELSE '❌ ERREUR: Rôle Gerant non trouvé'
    END as Statut_Role;

-- Assigner les permissions Client.Create et Client.ReadAll au rôle Gerant
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT 
    @gerant_role_id,
    p.IdPermission,
    NOW()
FROM Permissions p
WHERE p.Categorie = 'Client' 
  AND p.Nom IN ('Client.Create', 'Client.ReadAll')
  AND @gerant_role_id IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 
      FROM RolePermissions rp 
      WHERE rp.IdRole = @gerant_role_id 
        AND rp.IdPermission = p.IdPermission
  );

-- Afficher le résultat
SELECT 
    CASE 
        WHEN ROW_COUNT() > 0 THEN CONCAT('✅ ', ROW_COUNT(), ' permission(s) assignée(s) au rôle Gerant')
        ELSE 'ℹ️ Les permissions Client sont déjà assignées au rôle Gerant'
    END as Resultat;

-- Vérifier les permissions assignées au rôle Gerant
SELECT 
    '📋 Permissions Client assignées au rôle Gerant:' as Info;
SELECT 
    r.Nom as Role,
    p.Nom as Permission,
    p.Action,
    rp.DateAttribution
FROM Roles r
INNER JOIN RolePermissions rp ON r.IdRole = rp.IdRole
INNER JOIN Permissions p ON rp.IdPermission = p.IdPermission
WHERE r.Nom = 'Gerant'
  AND p.Categorie = 'Client'
  AND p.Nom IN ('Client.Create', 'Client.ReadAll')
ORDER BY p.Action;

-- ============================================================================
-- PARTIE 3: ASSIGNATION DES PERMISSIONS AU RÔLE FINANCIER
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '💰 ASSIGNATION DES PERMISSIONS AU RÔLE FINANCIER' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Récupérer l'ID du rôle Financier
SET @financier_role_id = (SELECT IdRole FROM Roles WHERE Nom = 'Financier' LIMIT 1);

-- Vérifier si le rôle existe
SELECT 
    CASE 
        WHEN @financier_role_id IS NOT NULL THEN CONCAT('✅ Rôle Financier trouvé (ID: ', @financier_role_id, ')')
        ELSE '❌ ERREUR: Rôle Financier non trouvé'
    END as Statut_Role;

-- Assigner les permissions Client.Create et Client.ReadAll au rôle Financier
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT 
    @financier_role_id,
    p.IdPermission,
    NOW()
FROM Permissions p
WHERE p.Categorie = 'Client' 
  AND p.Nom IN ('Client.Create', 'Client.ReadAll')
  AND @financier_role_id IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 
      FROM RolePermissions rp 
      WHERE rp.IdRole = @financier_role_id 
        AND rp.IdPermission = p.IdPermission
  );

-- Afficher le résultat
SELECT 
    CASE 
        WHEN ROW_COUNT() > 0 THEN CONCAT('✅ ', ROW_COUNT(), ' permission(s) assignée(s) au rôle Financier')
        ELSE 'ℹ️ Les permissions Client sont déjà assignées au rôle Financier'
    END as Resultat;

-- Vérifier les permissions assignées au rôle Financier
SELECT 
    '📋 Permissions Client assignées au rôle Financier:' as Info;
SELECT 
    r.Nom as Role,
    p.Nom as Permission,
    p.Action,
    rp.DateAttribution
FROM Roles r
INNER JOIN RolePermissions rp ON r.IdRole = rp.IdRole
INNER JOIN Permissions p ON rp.IdPermission = p.IdPermission
WHERE r.Nom = 'Financier'
  AND p.Categorie = 'Client'
  AND p.Nom IN ('Client.Create', 'Client.ReadAll')
ORDER BY p.Action;

-- ============================================================================
-- PARTIE 4: ASSIGNATION DES PERMISSIONS AU RÔLE CAISSIER
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '💵 ASSIGNATION DES PERMISSIONS AU RÔLE CAISSIER' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Récupérer l'ID du rôle Caissier
SET @caissier_role_id = (SELECT IdRole FROM Roles WHERE Nom = 'Caissier' LIMIT 1);

-- Vérifier si le rôle existe
SELECT 
    CASE 
        WHEN @caissier_role_id IS NOT NULL THEN CONCAT('✅ Rôle Caissier trouvé (ID: ', @caissier_role_id, ')')
        ELSE '❌ ERREUR: Rôle Caissier non trouvé'
    END as Statut_Role;

-- Assigner les permissions Client.Create et Client.ReadAll au rôle Caissier
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT 
    @caissier_role_id,
    p.IdPermission,
    NOW()
FROM Permissions p
WHERE p.Categorie = 'Client' 
  AND p.Nom IN ('Client.Create', 'Client.ReadAll')
  AND @caissier_role_id IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 
      FROM RolePermissions rp 
      WHERE rp.IdRole = @caissier_role_id 
        AND rp.IdPermission = p.IdPermission
  );

-- Afficher le résultat
SELECT 
    CASE 
        WHEN ROW_COUNT() > 0 THEN CONCAT('✅ ', ROW_COUNT(), ' permission(s) assignée(s) au rôle Caissier')
        ELSE 'ℹ️ Les permissions Client sont déjà assignées au rôle Caissier'
    END as Resultat;

-- Vérifier les permissions assignées au rôle Caissier
SELECT 
    '📋 Permissions Client assignées au rôle Caissier:' as Info;
SELECT 
    r.Nom as Role,
    p.Nom as Permission,
    p.Action,
    rp.DateAttribution
FROM Roles r
INNER JOIN RolePermissions rp ON r.IdRole = rp.IdRole
INNER JOIN Permissions p ON rp.IdPermission = p.IdPermission
WHERE r.Nom = 'Caissier'
  AND p.Categorie = 'Client'
  AND p.Nom IN ('Client.Create', 'Client.ReadAll')
ORDER BY p.Action;

-- ============================================================================
-- PARTIE 5: RÉSUMÉ FINAL
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '📊 RÉSUMÉ FINAL' as Title;
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

-- Détail complet des permissions par rôle
SELECT 
    '📋 Détail des permissions par rôle:' as Info;
SELECT 
    r.Nom as Role,
    p.Nom as Permission,
    p.Action,
    rp.DateAttribution
FROM Roles r
INNER JOIN RolePermissions rp ON r.IdRole = rp.IdRole
INNER JOIN Permissions p ON rp.IdPermission = p.IdPermission
WHERE r.Nom IN ('Gerant', 'Financier', 'Caissier', 'Admin', 'Super-Admin')
  AND p.Categorie = 'Client'
  AND p.Nom IN ('Client.Create', 'Client.ReadAll')
ORDER BY r.Nom, p.Action;

-- ============================================================================
-- PARTIE 6: VÉRIFICATION DES UTILISATEURS
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '👤 UTILISATEURS AVEC LES RÔLES GERANT, FINANCIER ET CAISSIER' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Lister les utilisateurs avec le rôle Gerant
SELECT 
    '🔵 Utilisateurs avec le rôle Gerant:' as Info;
SELECT 
    u.IdUtilisateur,
    u.NomComplet as Nom,
    u.Email,
    u.Telephone,
    r.Nom as Role,
    ur.Statut as Role_Statut,
    CASE 
        WHEN EXISTS (
            SELECT 1 
            FROM RolePermissions rp2
            INNER JOIN Permissions p2 ON rp2.IdPermission = p2.IdPermission
            WHERE rp2.IdRole = r.IdRole 
              AND p2.Nom = 'Client.Create'
        ) THEN '✅ A accès à Client.Create'
        ELSE '❌ N''a PAS accès à Client.Create'
    END as Acces_Create
FROM Utilisateurs u
INNER JOIN UserRoles ur ON u.IdUtilisateur = ur.IdUtilisateur
INNER JOIN Roles r ON ur.IdRole = r.IdRole
WHERE r.Nom = 'Gerant'
  AND ur.Statut = 1
ORDER BY u.NomComplet;

-- Lister les utilisateurs avec le rôle Financier
SELECT 
    '💰 Utilisateurs avec le rôle Financier:' as Info;
SELECT 
    u.IdUtilisateur,
    u.NomComplet as Nom,
    u.Email,
    u.Telephone,
    r.Nom as Role,
    ur.Statut as Role_Statut,
    CASE 
        WHEN EXISTS (
            SELECT 1 
            FROM RolePermissions rp2
            INNER JOIN Permissions p2 ON rp2.IdPermission = p2.IdPermission
            WHERE rp2.IdRole = r.IdRole 
              AND p2.Nom = 'Client.Create'
        ) THEN '✅ A accès à Client.Create'
        ELSE '❌ N''a PAS accès à Client.Create'
    END as Acces_Create
FROM Utilisateurs u
INNER JOIN UserRoles ur ON u.IdUtilisateur = ur.IdUtilisateur
INNER JOIN Roles r ON ur.IdRole = r.IdRole
WHERE r.Nom = 'Financier'
  AND ur.Statut = 1
ORDER BY u.NomComplet;

-- Lister les utilisateurs avec le rôle Caissier
SELECT 
    '💵 Utilisateurs avec le rôle Caissier:' as Info;
SELECT 
    u.IdUtilisateur,
    u.NomComplet as Nom,
    u.Email,
    u.Telephone,
    r.Nom as Role,
    ur.Statut as Role_Statut,
    CASE 
        WHEN EXISTS (
            SELECT 1 
            FROM RolePermissions rp2
            INNER JOIN Permissions p2 ON rp2.IdPermission = p2.IdPermission
            WHERE rp2.IdRole = r.IdRole 
              AND p2.Nom = 'Client.Create'
        ) THEN '✅ A accès à Client.Create'
        ELSE '❌ N''a PAS accès à Client.Create'
    END as Acces_Create
FROM Utilisateurs u
INNER JOIN UserRoles ur ON u.IdUtilisateur = ur.IdUtilisateur
INNER JOIN Roles r ON ur.IdRole = r.IdRole
WHERE r.Nom = 'Caissier'
  AND ur.Statut = 1
ORDER BY u.NomComplet;

-- ============================================================================
-- PARTIE 7: ACTIONS POST-EXÉCUTION
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '✅ SCRIPT TERMINÉ AVEC SUCCÈS' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

SELECT 
    '📝 ACTIONS REQUISES APRÈS L''EXÉCUTION:' as Info;
SELECT 
    '1. Les utilisateurs avec les rôles Gerant, Financier et Caissier doivent obtenir un nouveau token JWT' as Action;
SELECT 
    '2. Les permissions sont vérifiées au moment de l''authentification' as Action;
SELECT 
    '3. Testez les endpoints /api/Client/template-excel et /api/Client/bulk-excel avec les nouveaux tokens' as Action;

-- ============================================================================
-- FIN DU SCRIPT
-- ============================================================================
