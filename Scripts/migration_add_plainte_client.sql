-- ============================================================================
-- Script de migration : Ajout de la table PlainteClients
-- Date : 2025-12-14
-- Description : Crée la table pour gérer les plaintes déposées par les clients
--               pour l'équipe d'intervention sur le réseau électrique
-- ============================================================================
-- IMPORTANT : Ce script est IDEMPOTENT - peut être exécuté plusieurs fois sans erreur
-- ============================================================================

USE `FactureNormaliseeRDC`;

-- ============================================================================
-- PARTIE 1: Vérification et création de la table PlainteClients
-- ============================================================================

SET @table_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.TABLES 
    WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'PlainteClients'
);

SET @sql = IF(@table_exists = 0,
    'CREATE TABLE `PlainteClients` (
        `IdPlainte` INT NOT NULL AUTO_INCREMENT,
        `IdClient` INT NOT NULL,
        `IdPanneSignalement` INT NULL DEFAULT NULL,
        `Titre` VARCHAR(200) NOT NULL,
        `Description` VARCHAR(2000) NULL DEFAULT NULL,
        `TypePanne` VARCHAR(200) NULL DEFAULT NULL,
        `NiveauImportance` VARCHAR(50) NULL DEFAULT NULL,
        `RisquesPrincipaux` VARCHAR(500) NULL DEFAULT NULL,
        `StatutPlainte` VARCHAR(50) NOT NULL DEFAULT ''En attente'',
        `Priorite` VARCHAR(50) NULL DEFAULT NULL,
        `IdAgentAssigné` INT NULL DEFAULT NULL,
        `IdUtilisateurCreateur` INT NULL DEFAULT NULL,
        `CommentaireResolution` VARCHAR(1000) NULL DEFAULT NULL,
        `DateResolution` DATETIME(6) NULL DEFAULT NULL,
        `EstUrgente` TINYINT(1) NOT NULL DEFAULT 0,
        `DateCreation` DATETIME(6) NOT NULL,
        `DateDerniereModification` DATETIME(6) NOT NULL,
        PRIMARY KEY (`IdPlainte`),
        KEY `IX_PlainteClients_IdClient` (`IdClient`),
        KEY `IX_PlainteClients_IdPanneSignalement` (`IdPanneSignalement`),
        KEY `IX_PlainteClients_IdAgentAssigné` (`IdAgentAssigné`),
        KEY `IX_PlainteClients_IdUtilisateurCreateur` (`IdUtilisateurCreateur`),
        CONSTRAINT `FK_PlainteClients_Clients_IdClient` 
            FOREIGN KEY (`IdClient`) 
            REFERENCES `Clients` (`IdClient`) 
            ON DELETE RESTRICT,
        CONSTRAINT `FK_PlainteClients_PanneSignalements_IdPanneSignalement` 
            FOREIGN KEY (`IdPanneSignalement`) 
            REFERENCES `PanneSignalements` (`IdPanneSignalement`) 
            ON DELETE SET NULL,
        CONSTRAINT `FK_PlainteClients_Agents_IdAgentAssigné` 
            FOREIGN KEY (`IdAgentAssigné`) 
            REFERENCES `Agents` (`IdAgent`) 
            ON DELETE SET NULL,
        CONSTRAINT `FK_PlainteClients_Utilisateurs_IdUtilisateurCreateur` 
            FOREIGN KEY (`IdUtilisateurCreateur`) 
            REFERENCES `Utilisateurs` (`IdUtilisateur`) 
            ON DELETE SET NULL
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;',
    'SELECT ''Table PlainteClients already exists, skipping creation'' AS message'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ============================================================================
-- PARTIE 2: Vérification et création des index
-- ============================================================================

-- Réinitialiser @table_exists pour vérifier à nouveau
SET @table_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.TABLES 
    WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'PlainteClients'
);

-- Index sur IdClient
SET @index_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.STATISTICS 
    WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'PlainteClients'
    AND INDEX_NAME = 'IX_PlainteClients_IdClient'
);

SET @sql = IF(@index_exists = 0 AND @table_exists > 0,
    'ALTER TABLE `PlainteClients` ADD INDEX `IX_PlainteClients_IdClient` (`IdClient`);',
    'SELECT ''Index IX_PlainteClients_IdClient already exists or table does not exist, skipping'' AS message'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Index sur IdPanneSignalement
SET @index_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.STATISTICS 
    WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'PlainteClients'
    AND INDEX_NAME = 'IX_PlainteClients_IdPanneSignalement'
);

SET @sql = IF(@index_exists = 0 AND @table_exists > 0,
    'ALTER TABLE `PlainteClients` ADD INDEX `IX_PlainteClients_IdPanneSignalement` (`IdPanneSignalement`);',
    'SELECT ''Index IX_PlainteClients_IdPanneSignalement already exists or table does not exist, skipping'' AS message'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Index sur IdAgentAssigné
SET @index_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.STATISTICS 
    WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'PlainteClients'
    AND INDEX_NAME = 'IX_PlainteClients_IdAgentAssigné'
);

SET @sql = IF(@index_exists = 0 AND @table_exists > 0,
    'ALTER TABLE `PlainteClients` ADD INDEX `IX_PlainteClients_IdAgentAssigné` (`IdAgentAssigné`);',
    'SELECT ''Index IX_PlainteClients_IdAgentAssigné already exists or table does not exist, skipping'' AS message'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Index sur IdUtilisateurCreateur
SET @index_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.STATISTICS 
    WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'PlainteClients'
    AND INDEX_NAME = 'IX_PlainteClients_IdUtilisateurCreateur'
);

SET @sql = IF(@index_exists = 0 AND @table_exists > 0,
    'ALTER TABLE `PlainteClients` ADD INDEX `IX_PlainteClients_IdUtilisateurCreateur` (`IdUtilisateurCreateur`);',
    'SELECT ''Index IX_PlainteClients_IdUtilisateurCreateur already exists or table does not exist, skipping'' AS message'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ============================================================================
-- PARTIE 3: Vérification et création des clés étrangères
-- ============================================================================

-- Réinitialiser @table_exists pour vérifier à nouveau
SET @table_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.TABLES 
    WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'PlainteClients'
);

-- FK vers Clients
SET @fk_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE 
    WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'PlainteClients'
    AND CONSTRAINT_NAME = 'FK_PlainteClients_Clients_IdClient'
);

SET @sql = IF(@fk_exists = 0 AND @table_exists > 0,
    'ALTER TABLE `PlainteClients` 
     ADD CONSTRAINT `FK_PlainteClients_Clients_IdClient` 
     FOREIGN KEY (`IdClient`) 
     REFERENCES `Clients` (`IdClient`) 
     ON DELETE RESTRICT;',
    'SELECT ''Foreign key FK_PlainteClients_Clients_IdClient already exists or table does not exist, skipping'' AS message'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- FK vers PanneSignalements
SET @fk_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE 
    WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'PlainteClients'
    AND CONSTRAINT_NAME = 'FK_PlainteClients_PanneSignalements_IdPanneSignalement'
);

SET @sql = IF(@fk_exists = 0 AND @table_exists > 0,
    'ALTER TABLE `PlainteClients` 
     ADD CONSTRAINT `FK_PlainteClients_PanneSignalements_IdPanneSignalement` 
     FOREIGN KEY (`IdPanneSignalement`) 
     REFERENCES `PanneSignalements` (`IdPanneSignalement`) 
     ON DELETE SET NULL;',
    'SELECT ''Foreign key FK_PlainteClients_PanneSignalements_IdPanneSignalement already exists or table does not exist, skipping'' AS message'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- FK vers Agents
SET @fk_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE 
    WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'PlainteClients'
    AND CONSTRAINT_NAME = 'FK_PlainteClients_Agents_IdAgentAssigné'
);

SET @sql = IF(@fk_exists = 0 AND @table_exists > 0,
    'ALTER TABLE `PlainteClients` 
     ADD CONSTRAINT `FK_PlainteClients_Agents_IdAgentAssigné` 
     FOREIGN KEY (`IdAgentAssigné`) 
     REFERENCES `Agents` (`IdAgent`) 
     ON DELETE SET NULL;',
    'SELECT ''Foreign key FK_PlainteClients_Agents_IdAgentAssigné already exists or table does not exist, skipping'' AS message'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- FK vers Utilisateurs
SET @fk_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE 
    WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'PlainteClients'
    AND CONSTRAINT_NAME = 'FK_PlainteClients_Utilisateurs_IdUtilisateurCreateur'
);

SET @sql = IF(@fk_exists = 0 AND @table_exists > 0,
    'ALTER TABLE `PlainteClients` 
     ADD CONSTRAINT `FK_PlainteClients_Utilisateurs_IdUtilisateurCreateur` 
     FOREIGN KEY (`IdUtilisateurCreateur`) 
     REFERENCES `Utilisateurs` (`IdUtilisateur`) 
     ON DELETE SET NULL;',
    'SELECT ''Foreign key FK_PlainteClients_Utilisateurs_IdUtilisateurCreateur already exists or table does not exist, skipping'' AS message'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ============================================================================
-- VÉRIFICATION FINALE
-- ============================================================================

SELECT 
    CASE 
        WHEN (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'PlainteClients') > 0 
        THEN '✅ Table PlainteClients créée avec succès'
        ELSE '❌ Erreur: Table PlainteClients non créée'
    END AS Status;

SELECT 
    COUNT(*) AS NombreIndex,
    'Index sur PlainteClients' AS Description
FROM INFORMATION_SCHEMA.STATISTICS 
WHERE TABLE_SCHEMA = DATABASE()
AND TABLE_NAME = 'PlainteClients';

SELECT 
    COUNT(*) AS NombreContraintes,
    'Contraintes de clés étrangères sur PlainteClients' AS Description
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE 
WHERE TABLE_SCHEMA = DATABASE()
AND TABLE_NAME = 'PlainteClients'
AND REFERENCED_TABLE_NAME IS NOT NULL;

-- ============================================================================
-- FIN DU SCRIPT
-- ============================================================================

