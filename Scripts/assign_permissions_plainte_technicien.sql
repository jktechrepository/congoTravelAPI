-- ============================================================================
-- Script d'assignation des permissions PlainteClient au rôle Technicien
-- Date : 2025-12-16
-- Description : Assigne les permissions PlainteClient (Read, ReadAll) au rôle 
--               Technicien pour permettre la lecture des plaintes clients
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

-- Vérifier que les permissions PlainteClient existent
SELECT 
    CASE 
        WHEN COUNT(*) >= 2 THEN CONCAT('✅ Les permissions PlainteClient nécessaires existent (', COUNT(*), ' trouvée(s))')
        ELSE CONCAT('❌ ATTENTION: Seulement ', COUNT(*), ' permission(s) PlainteClient trouvée(s) sur 2 minimum attendues')
    END as Statut_Permissions
FROM Permissions
WHERE Categorie = 'PlainteClient' 
  AND Nom IN ('PlainteClient.Read', 'PlainteClient.ReadAll');

-- Lister les permissions PlainteClient disponibles
SELECT 
    '📋 Permissions PlainteClient disponibles:' as Info;
SELECT 
    IdPermission,
    Nom,
    Action,
    Description
FROM Permissions
WHERE Categorie = 'PlainteClient'
ORDER BY Action;

-- Vérifier que le rôle Technicien existe
SELECT 
    '👥 Rôle ciblé:' as Info;
SELECT 
    IdRole,
    Nom,
    Statut
FROM Roles
WHERE Nom = 'Technicien';

-- ============================================================================
-- PARTIE 2: ASSIGNATION DES PERMISSIONS AU RÔLE TECHNICIEN
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '🟣 ASSIGNATION DES PERMISSIONS AU RÔLE TECHNICIEN' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Récupérer l'ID du rôle Technicien
SET @technicien_role_id = (SELECT IdRole FROM Roles WHERE Nom = 'Technicien' LIMIT 1);

-- Vérifier si le rôle existe
SELECT 
    CASE 
        WHEN @technicien_role_id IS NOT NULL THEN CONCAT('✅ Rôle Technicien trouvé (ID: ', @technicien_role_id, ')')
        ELSE '❌ ERREUR: Rôle Technicien non trouvé'
    END as Statut_Role;

-- Assigner les permissions PlainteClient.Read et PlainteClient.ReadAll au rôle Technicien
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT 
    @technicien_role_id,
    p.IdPermission,
    NOW()
FROM Permissions p
WHERE p.Categorie = 'PlainteClient' 
  AND p.Nom IN ('PlainteClient.Read', 'PlainteClient.ReadAll')
  AND @technicien_role_id IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 
      FROM RolePermissions rp 
      WHERE rp.IdRole = @technicien_role_id 
        AND rp.IdPermission = p.IdPermission
  );

-- Afficher le résultat
SELECT 
    CASE 
        WHEN ROW_COUNT() > 0 THEN CONCAT('✅ ', ROW_COUNT(), ' permission(s) assignée(s) au rôle Technicien')
        ELSE 'ℹ️ Les permissions PlainteClient sont déjà assignées au rôle Technicien'
    END as Resultat;

-- Vérifier les permissions assignées au rôle Technicien
SELECT 
    '📋 Permissions PlainteClient assignées au rôle Technicien:' as Info;
SELECT 
    r.Nom as Role,
    p.Nom as Permission,
    p.Action,
    rp.DateAttribution
FROM Roles r
INNER JOIN RolePermissions rp ON r.IdRole = rp.IdRole
INNER JOIN Permissions p ON rp.IdPermission = p.IdPermission
WHERE r.Nom = 'Technicien'
  AND p.Categorie = 'PlainteClient'
  AND p.Nom IN ('PlainteClient.Read', 'PlainteClient.ReadAll')
ORDER BY p.Action;

-- ============================================================================
-- PARTIE 3: VÉRIFICATION DES PERMISSIONS COMPLÈTES
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '🔍 VÉRIFICATION DES PERMISSIONS COMPLÈTES' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Vérifier toutes les permissions PlainteClient du rôle Technicien
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
-- PARTIE 4: RÉSUMÉ FINAL
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
WHERE r.Nom IN ('Technicien', 'Admin', 'Super-Admin', 'Gerant')
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
WHERE r.Nom IN ('Technicien', 'Admin', 'Super-Admin', 'Gerant')
  AND p.Categorie = 'PlainteClient'
  AND p.Nom IN ('PlainteClient.Read', 'PlainteClient.ReadAll')
ORDER BY r.Nom, p.Action;

-- ============================================================================
-- PARTIE 5: VÉRIFICATION DES UTILISATEURS
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
    ur.Statut as Role_Statut,
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
-- PARTIE 6: ACTIONS POST-EXÉCUTION
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
    '1. Les utilisateurs avec le rôle Technicien doivent obtenir un nouveau token JWT' as Action;
SELECT 
    '2. Les permissions sont vérifiées au moment de l''authentification' as Action;
SELECT 
    '3. Testez les endpoints PlainteClient avec les nouveaux tokens:' as Action;
SELECT 
    '   - GET /api/PlainteClient (nécessite PlainteClient.ReadAll)' as Endpoint_1;
SELECT 
    '   - GET /api/PlainteClient/{id} (nécessite PlainteClient.Read)' as Endpoint_2;
SELECT 
    '   - GET /api/PlainteClient/paged (nécessite PlainteClient.ReadAll)' as Endpoint_3;

-- ============================================================================
-- FIN DU SCRIPT
-- ============================================================================
