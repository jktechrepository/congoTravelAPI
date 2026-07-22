-- ============================================================================
-- Script de migration : Ajout du champ Usage au modèle Client
-- Base de données: MariaDB / MySQL
-- Version: 1.0
-- Date: Décembre 2025
-- ============================================================================
-- 
-- INSTRUCTIONS:
-- 1. ⚠️  FAITES UN BACKUP de votre base de données avant d'exécuter ce script
-- 2. Utilisez la base de données: USE CongoTravelDB; (ou votre nom de base)
-- 3. Exécutez ce script pour ajouter le nouveau champ
-- 
-- MODIFICATIONS:
-- - Ajoute la colonne Usage (VARCHAR(200), nullable) à la table Clients
-- 
-- ============================================================================

SET FOREIGN_KEY_CHECKS = 0;
SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET AUTOCOMMIT = 0;
START TRANSACTION;
SET time_zone = "+00:00";

-- ============================================================================
-- Ajout de la colonne Usage à la table Clients
-- ============================================================================

-- Vérifier si la colonne Usage existe déjà
SET @exists := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
        AND TABLE_NAME = 'Clients'
        AND COLUMN_NAME = 'Usage'
);

-- Ajouter la colonne si elle n'existe pas
SET @sql := IF(@exists = 0,
    'ALTER TABLE `Clients` ADD COLUMN `Usage` VARCHAR(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL AFTER `IsActif`;',
    'SELECT "Colonne Usage existe déjà, ignorée." AS message;'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ============================================================================
-- Vérification finale
-- ============================================================================

SELECT 
    'Migration terminée avec succès!' AS message,
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'Clients' 
     AND COLUMN_NAME = 'Usage') AS colonne_ajoutee;

COMMIT;
SET FOREIGN_KEY_CHECKS = 1;

-- ============================================================================
-- FIN DU SCRIPT
-- ============================================================================

