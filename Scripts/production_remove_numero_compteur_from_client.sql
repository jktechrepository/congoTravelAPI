-- =====================================================
-- Script SQL de Production : Suppression du champ numero_compteur de la table Clients
-- =====================================================
-- Description : Ce script supprime la colonne numero_compteur et son index unique
--               de la table Clients
-- =====================================================
-- Date : 2026-01-03
-- Compatible : MySQL 5.7+, MariaDB 10.2+
-- =====================================================

-- ⚠️ IMPORTANT : 
-- 1. Sauvegardez votre base de données avant d'exécuter ce script
-- 2. Ce script est idempotent : il peut être exécuté plusieurs fois sans erreur

-- Désactiver temporairement les vérifications de clés étrangères
SET FOREIGN_KEY_CHECKS = 0;

-- =====================================================
-- 1. Supprimer l'index unique sur numero_compteur (si existe)
-- =====================================================

-- Vérifier si l'index existe
SET @index_exists = (
    SELECT COUNT(*) 
    FROM information_schema.statistics 
    WHERE table_schema = DATABASE() 
      AND table_name = 'Clients' 
      AND index_name = 'IX_Client_NumeroCompteur_Unique'
);

-- Supprimer l'index si il existe
SET @sql_drop_index = IF(
    @index_exists > 0,
    'DROP INDEX `IX_Client_NumeroCompteur_Unique` ON `Clients`;',
    'SELECT "Index IX_Client_NumeroCompteur_Unique n''existe pas, ignoré" AS Message;'
);

PREPARE stmt FROM @sql_drop_index;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- =====================================================
-- 2. Supprimer la colonne numero_compteur
-- =====================================================

-- Vérifier si la colonne existe
SET @column_exists = (
    SELECT COUNT(*) 
    FROM information_schema.columns 
    WHERE table_schema = DATABASE() 
      AND table_name = 'Clients' 
      AND column_name = 'numero_compteur'
);

-- Supprimer la colonne si elle existe
SET @sql_drop_column = IF(
    @column_exists > 0,
    'ALTER TABLE `Clients` DROP COLUMN `numero_compteur`;',
    'SELECT "Colonne numero_compteur n''existe pas, ignorée" AS Message;'
);

PREPARE stmt FROM @sql_drop_column;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- =====================================================
-- Réactiver les vérifications de clés étrangères
-- =====================================================

SET FOREIGN_KEY_CHECKS = 1;

-- =====================================================
-- VÉRIFICATIONS
-- =====================================================

-- Vérifier que l'index a été supprimé
SELECT 
    'Vérification Index supprimé' AS Verification,
    CASE 
        WHEN COUNT(*) = 0 THEN '✅ Index IX_Client_NumeroCompteur_Unique supprimé'
        ELSE '⚠️ Index IX_Client_NumeroCompteur_Unique existe encore'
    END AS Statut
FROM information_schema.statistics 
WHERE table_schema = DATABASE() 
  AND table_name = 'Clients' 
  AND index_name = 'IX_Client_NumeroCompteur_Unique';

-- Vérifier que la colonne a été supprimée
SELECT 
    'Vérification Colonne supprimée' AS Verification,
    CASE 
        WHEN COUNT(*) = 0 THEN '✅ Colonne numero_compteur supprimée'
        ELSE '❌ Colonne numero_compteur existe encore'
    END AS Statut
FROM information_schema.columns 
WHERE table_schema = DATABASE() 
  AND table_name = 'Clients' 
  AND column_name = 'numero_compteur';

-- =====================================================
-- RÉSUMÉ DES MODIFICATIONS
-- =====================================================
-- 
-- ✅ Index unique IX_Client_NumeroCompteur_Unique supprimé
-- ✅ Colonne numero_compteur supprimée de la table Clients
-- 
-- =====================================================
-- NOTES IMPORTANTES
-- =====================================================
-- 
-- 1. Toutes les données de la colonne numero_compteur seront perdues
-- 2. Si vous obtenez des erreurs "Unknown column" ou "Unknown key",
--    cela signifie que la colonne/index n'existe pas. C'est normal et
--    vous pouvez ignorer ces erreurs.
-- 
-- =====================================================

