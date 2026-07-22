-- ============================================================================
-- Script de migration : Ajout de champs au modèle PleinteClient
-- Base de données: MariaDB / MySQL
-- Version: 1.0
-- Date: Décembre 2025
-- ============================================================================
-- 
-- INSTRUCTIONS:
-- 1. ⚠️  FAITES UN BACKUP de votre base de données avant d'exécuter ce script
-- 2. Utilisez la base de données: USE CongoTravelDB; (ou votre nom de base)
-- 3. Exécutez ce script pour ajouter les nouveaux champs
-- 
-- MODIFICATIONS:
-- - Ajoute 3 colonnes à la table PleinteClients : TypePanne, NiveauImportance, RisquesPrincipaux
-- - Renomme IdUtilisateurEnregistrement en IdUtilisateur dans la table Paiements
-- 
-- ============================================================================

SET FOREIGN_KEY_CHECKS = 0;
SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET AUTOCOMMIT = 0;
START TRANSACTION;
SET time_zone = "+00:00";

-- ============================================================================
-- PARTIE 1: Ajout des colonnes à la table PleinteClients
-- ============================================================================

-- Vérifier et ajouter la colonne TypePanne
SET @exists := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
        AND TABLE_NAME = 'PleinteClients'
        AND COLUMN_NAME = 'TypePanne'
);

SET @sql := IF(@exists = 0,
    'ALTER TABLE `PleinteClients` ADD COLUMN `TypePanne` VARCHAR(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL AFTER `Statut`;',
    'SELECT "Colonne TypePanne existe déjà, ignorée." AS message;'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Vérifier et ajouter la colonne NiveauImportance
SET @exists := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
        AND TABLE_NAME = 'PleinteClients'
        AND COLUMN_NAME = 'NiveauImportance'
);

SET @sql := IF(@exists = 0,
    'ALTER TABLE `PleinteClients` ADD COLUMN `NiveauImportance` VARCHAR(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL AFTER `TypePanne`;',
    'SELECT "Colonne NiveauImportance existe déjà, ignorée." AS message;'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Vérifier et ajouter la colonne RisquesPrincipaux
SET @exists := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
        AND TABLE_NAME = 'PleinteClients'
        AND COLUMN_NAME = 'RisquesPrincipaux'
);

SET @sql := IF(@exists = 0,
    'ALTER TABLE `PleinteClients` ADD COLUMN `RisquesPrincipaux` VARCHAR(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL AFTER `NiveauImportance`;',
    'SELECT "Colonne RisquesPrincipaux existe déjà, ignorée." AS message;'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ============================================================================
-- PARTIE 2: Renommage de IdUtilisateurEnregistrement en IdUtilisateur
-- ============================================================================
-- NOTE: Cette partie peut être ignorée si la colonne a déjà été renommée
-- ============================================================================

-- Vérifier si la colonne IdUtilisateurEnregistrement existe
SET @exists_old := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
        AND TABLE_NAME = 'Paiements'
        AND COLUMN_NAME = 'IdUtilisateurEnregistrement'
);

-- Vérifier si la colonne IdUtilisateur existe déjà
SET @exists_new := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
        AND TABLE_NAME = 'Paiements'
        AND COLUMN_NAME = 'IdUtilisateur'
);

-- Si l'ancienne colonne existe et la nouvelle n'existe pas, on renomme
SET @should_rename := @exists_old > 0 AND @exists_new = 0;

-- Supprimer l'ancienne clé étrangère si elle existe
SET @drop_fk := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
    WHERE TABLE_SCHEMA = DATABASE()
        AND TABLE_NAME = 'Paiements'
        AND CONSTRAINT_NAME = 'FK_Paiements_Utilisateurs_IdUtilisateurEnregistrement'
        AND CONSTRAINT_TYPE = 'FOREIGN KEY'
);

SET @sql_drop_fk := IF(@should_rename AND @drop_fk > 0,
    'ALTER TABLE `Paiements` DROP FOREIGN KEY `FK_Paiements_Utilisateurs_IdUtilisateurEnregistrement`;',
    'SELECT 1;'
);

PREPARE stmt_drop_fk FROM @sql_drop_fk;
EXECUTE stmt_drop_fk;
DEALLOCATE PREPARE stmt_drop_fk;

-- Supprimer l'ancien index si il existe
SET @drop_idx := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
        AND TABLE_NAME = 'Paiements'
        AND INDEX_NAME = 'IX_Paiements_IdUtilisateurEnregistrement'
);

SET @sql_drop_idx := IF(@should_rename AND @drop_idx > 0,
    'ALTER TABLE `Paiements` DROP INDEX `IX_Paiements_IdUtilisateurEnregistrement`;',
    'SELECT 1;'
);

PREPARE stmt_drop_idx FROM @sql_drop_idx;
EXECUTE stmt_drop_idx;
DEALLOCATE PREPARE stmt_drop_idx;

-- Renommer la colonne
SET @sql_rename := IF(@should_rename,
    'ALTER TABLE `Paiements` CHANGE COLUMN `IdUtilisateurEnregistrement` `IdUtilisateur` INT NULL DEFAULT NULL;',
    'SELECT 1;'
);

PREPARE stmt_rename FROM @sql_rename;
EXECUTE stmt_rename;
DEALLOCATE PREPARE stmt_rename;

-- Créer le nouvel index si il n'existe pas
SET @idx_new_exists := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
        AND TABLE_NAME = 'Paiements'
        AND INDEX_NAME = 'IX_Paiements_IdUtilisateur'
);

SET @sql_add_idx := IF(@should_rename AND @idx_new_exists = 0,
    'ALTER TABLE `Paiements` ADD INDEX `IX_Paiements_IdUtilisateur` (`IdUtilisateur`);',
    'SELECT 1;'
);

PREPARE stmt_add_idx FROM @sql_add_idx;
EXECUTE stmt_add_idx;
DEALLOCATE PREPARE stmt_add_idx;

-- Créer la nouvelle clé étrangère si elle n'existe pas
SET @fk_new_exists := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
    WHERE TABLE_SCHEMA = DATABASE()
        AND TABLE_NAME = 'Paiements'
        AND CONSTRAINT_NAME = 'FK_Paiements_Utilisateurs_IdUtilisateur'
        AND CONSTRAINT_TYPE = 'FOREIGN KEY'
);

SET @sql_add_fk := IF(@should_rename AND @fk_new_exists = 0,
    'ALTER TABLE `Paiements` ADD CONSTRAINT `FK_Paiements_Utilisateurs_IdUtilisateur` FOREIGN KEY (`IdUtilisateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE SET NULL;',
    'SELECT 1;'
);

PREPARE stmt_add_fk FROM @sql_add_fk;
EXECUTE stmt_add_fk;
DEALLOCATE PREPARE stmt_add_fk;

-- ============================================================================
-- Vérification finale
-- ============================================================================

SELECT 
    'Migration terminée avec succès!' AS message,
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'PleinteClients' 
     AND COLUMN_NAME IN ('TypePanne', 'NiveauImportance', 'RisquesPrincipaux')) AS colonnes_ajoutees,
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
     WHERE TABLE_SCHEMA = DATABASE() 
     AND TABLE_NAME = 'Paiements' 
     AND COLUMN_NAME = 'IdUtilisateur') AS colonne_renommee;

COMMIT;
SET FOREIGN_KEY_CHECKS = 1;

-- ============================================================================
-- FIN DU SCRIPT
-- ============================================================================
