-- ============================================================================
-- SCRIPT DE ROLLBACK : Suppression de la relation Many-to-Many Client-CategorieClient
-- Date : 2025-12-20
-- Description : 
--   - Supprime la table ClientCategorieClients
--   - ATTENTION : Ce script supprime TOUTES les relations many-to-many
--   - Les clients conservent leur IdCategorieClient (catégorie principale)
-- 
-- ⚠️ AVERTISSEMENT : 
--   - Ce script est DESTRUCTIF
--   - Faire une SAUVEGARDE avant d'exécuter
--   - Utiliser uniquement en cas de problème nécessitant un rollback complet
-- ============================================================================

SELECT 
    '═══════════════════════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '⚠️ ROLLBACK : Suppression de la relation Many-to-Many' as Title;
SELECT 
    '═══════════════════════════════════════════════════════════════════════════════' as Separator;

-- ============================================================================
-- PARTIE 1: VÉRIFICATIONS PRÉALABLES
-- ============================================================================

SELECT 
    '🔍 VÉRIFICATIONS PRÉALABLES' as Title;

-- Vérifier l'existence de la table
SELECT 
    CASE 
        WHEN COUNT(*) > 0 THEN '✅ Table ClientCategorieClients existe - Rollback possible'
        ELSE 'ℹ️ Table ClientCategorieClients n''existe pas - Aucun rollback nécessaire'
    END as Statut_Table
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'ClientCategorieClients';

-- Compter les relations qui seront supprimées
SELECT 
    '📊 Relations qui seront supprimées:' as Info;
SELECT 
    COUNT(*) as Nombre_Relations_A_Supprimer,
    COUNT(DISTINCT IdClient) as Clients_Concernes,
    COUNT(DISTINCT IdCategorie) as Categories_Concernees
FROM ClientCategorieClients;

-- Afficher un échantillon des relations qui seront supprimées
SELECT 
    '📋 Échantillon des relations qui seront supprimées:' as Info;
SELECT 
    ccc.IdClient,
    c.NomClient,
    ccc.IdCategorie,
    cat.NomCategorie,
    ccc.DateAttribution
FROM ClientCategorieClients ccc
INNER JOIN Clients c ON ccc.IdClient = c.IdClient
INNER JOIN CategorieClients cat ON ccc.IdCategorie = cat.IdCategorie
ORDER BY ccc.DateAttribution DESC
LIMIT 20;

-- ============================================================================
-- PARTIE 2: CONFIRMATION (À DÉCOMMENTER POUR EXÉCUTER)
-- ============================================================================

-- ⚠️ DÉCOMMENTER LES LIGNES CI-DESSOUS POUR EFFECTUER LE ROLLBACK
-- ⚠️ ATTENTION : Cette opération est IRRÉVERSIBLE

/*
-- ============================================================================
-- PARTIE 3: SUPPRESSION DE LA TABLE
-- ============================================================================

SELECT 
    '═══════════════════════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '🗑️ SUPPRESSION DE LA TABLE ClientCategorieClients' as Title;
SELECT 
    '═══════════════════════════════════════════════════════════════════════════════' as Separator;

-- Supprimer les contraintes de clés étrangères d'abord (si nécessaire)
-- MySQL les supprime automatiquement avec DROP TABLE

-- Supprimer la table
DROP TABLE IF EXISTS `ClientCategorieClients`;

-- Vérifier la suppression
SELECT 
    CASE 
        WHEN COUNT(*) = 0 THEN '✅ Table ClientCategorieClients supprimée avec succès'
        ELSE '❌ ERREUR : La table existe encore'
    END as Statut_Suppression
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'ClientCategorieClients';

-- ============================================================================
-- PARTIE 4: VÉRIFICATION POST-ROLLBACK
-- ============================================================================

SELECT 
    '═══════════════════════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '✅ VÉRIFICATION POST-ROLLBACK' as Title;
SELECT 
    '═══════════════════════════════════════════════════════════════════════════════' as Separator;

-- Vérifier que les clients conservent toujours leur catégorie principale
SELECT 
    '📊 Clients avec catégorie principale (conservée):' as Info;
SELECT 
    COUNT(*) as Total_Clients,
    COUNT(IdCategorieClient) as Clients_Avec_Categorie_Principale
FROM Clients;

-- Afficher un échantillon
SELECT 
    '📋 Échantillon de clients avec leur catégorie principale:' as Info;
SELECT 
    c.IdClient,
    c.NomClient,
    c.IdCategorieClient,
    cat.NomCategorie
FROM Clients c
LEFT JOIN CategorieClients cat ON c.IdCategorieClient = cat.IdCategorie
WHERE c.IdCategorieClient IS NOT NULL
ORDER BY c.IdClient
LIMIT 20;

SELECT 
    '═══════════════════════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '✅ ROLLBACK TERMINÉ' as Title;
SELECT 
    '═══════════════════════════════════════════════════════════════════════════════' as Separator;

SELECT 
    '📝 NOTES IMPORTANTES:' as Info;
SELECT 
    '1. La table ClientCategorieClients a été supprimée' as Note;
SELECT 
    '2. Les clients conservent leur IdCategorieClient (catégorie principale)' as Note;
SELECT 
    '3. Toutes les relations many-to-many ont été perdues' as Note;
SELECT 
    '4. Pour réactiver la fonctionnalité, réexécuter le script de migration' as Note;
*/

-- ============================================================================
-- FIN DU SCRIPT
-- ============================================================================

SELECT 
    'ℹ️ Ce script est en mode SÉCURISÉ (rollback désactivé)' as Info;
SELECT 
    'Pour effectuer le rollback, décommenter la section PARTIE 3 et PARTIE 4' as Instruction;
