-- ============================================================================
-- Script de migration : Ajout de la table CommunicationCampaigns
-- Date : 2025-12-14
-- Description : Crée la table pour gérer les campagnes de communication
--               permettant à la société d'envoyer des communiqués à ses clients
-- ============================================================================
-- IMPORTANT : Ce script est IDEMPOTENT - peut être exécuté plusieurs fois sans erreur
-- ============================================================================

USE `FactureNormaliseeRDC`;

-- ============================================================================
-- PARTIE 1: Vérification et création de la table CommunicationCampaigns
-- ============================================================================

SET @table_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.TABLES 
    WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'CommunicationCampaigns'
);

SET @sql = IF(@table_exists = 0,
    'CREATE TABLE `CommunicationCampaigns` (
        `IdCampagne` INT NOT NULL AUTO_INCREMENT,
        `Titre` VARCHAR(200) NOT NULL,
        `Contenu` VARCHAR(2000) NOT NULL,
        `TypeCampagne` VARCHAR(50) NOT NULL DEFAULT ''INFO'',
        `IdSociete` INT NULL DEFAULT NULL,
        `IdUtilisateurCreateur` INT NOT NULL,
        `CriteresCiblage` TEXT NULL DEFAULT NULL,
        `ListeIdClients` TEXT NULL DEFAULT NULL,
        `ActiverPush` TINYINT(1) NOT NULL DEFAULT 1,
        `ActiverSms` TINYINT(1) NOT NULL DEFAULT 0,
        `ActiverEmail` TINYINT(1) NOT NULL DEFAULT 0,
        `ActiverInApp` TINYINT(1) NOT NULL DEFAULT 1,
        `DateEnvoi` DATETIME(6) NULL DEFAULT NULL,
        `EstProgrammee` TINYINT(1) NOT NULL DEFAULT 0,
        `EstEnCours` TINYINT(1) NOT NULL DEFAULT 0,
        `EstTerminee` TINYINT(1) NOT NULL DEFAULT 0,
        `NombreDestinataires` INT NOT NULL DEFAULT 0,
        `NombreEnvoyes` INT NOT NULL DEFAULT 0,
        `NombreSucces` INT NOT NULL DEFAULT 0,
        `NombreEchecs` INT NOT NULL DEFAULT 0,
        `DateCreation` DATETIME(6) NOT NULL,
        `DateDerniereModification` DATETIME(6) NOT NULL,
        `DateEnvoiEffectif` DATETIME(6) NULL DEFAULT NULL,
        PRIMARY KEY (`IdCampagne`),
        KEY `IX_CommunicationCampaigns_IdSociete` (`IdSociete`),
        KEY `IX_CommunicationCampaigns_IdUtilisateurCreateur` (`IdUtilisateurCreateur`),
        CONSTRAINT `FK_CommunicationCampaigns_Societes_IdSociete` 
            FOREIGN KEY (`IdSociete`) 
            REFERENCES `Societes` (`IdSociete`) 
            ON DELETE SET NULL,
        CONSTRAINT `FK_CommunicationCampaigns_Utilisateurs_IdUtilisateurCreateur` 
            FOREIGN KEY (`IdUtilisateurCreateur`) 
            REFERENCES `Utilisateurs` (`IdUtilisateur`) 
            ON DELETE RESTRICT
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;',
    'SELECT ''Table CommunicationCampaigns already exists, skipping creation'' AS message'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ============================================================================
-- PARTIE 2: Vérification et création de l'index sur IdSociete
-- ============================================================================

SET @index_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.STATISTICS 
    WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'CommunicationCampaigns'
    AND INDEX_NAME = 'IX_CommunicationCampaigns_IdSociete'
);

SET @sql = IF(@index_exists = 0 AND @table_exists = 0,
    'ALTER TABLE `CommunicationCampaigns` ADD INDEX `IX_CommunicationCampaigns_IdSociete` (`IdSociete`);',
    'SELECT ''Index IX_CommunicationCampaigns_IdSociete already exists or table does not exist, skipping'' AS message'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ============================================================================
-- PARTIE 3: Vérification et création de l'index sur IdUtilisateurCreateur
-- ============================================================================

SET @index_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.STATISTICS 
    WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'CommunicationCampaigns'
    AND INDEX_NAME = 'IX_CommunicationCampaigns_IdUtilisateurCreateur'
);

SET @sql = IF(@index_exists = 0 AND @table_exists = 0,
    'ALTER TABLE `CommunicationCampaigns` ADD INDEX `IX_CommunicationCampaigns_IdUtilisateurCreateur` (`IdUtilisateurCreateur`);',
    'SELECT ''Index IX_CommunicationCampaigns_IdUtilisateurCreateur already exists or table does not exist, skipping'' AS message'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ============================================================================
-- PARTIE 4: Vérification et création de la clé étrangère vers Societes
-- ============================================================================

SET @fk_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE 
    WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'CommunicationCampaigns'
    AND CONSTRAINT_NAME = 'FK_CommunicationCampaigns_Societes_IdSociete'
);

SET @sql = IF(@fk_exists = 0 AND @table_exists = 0,
    'ALTER TABLE `CommunicationCampaigns` 
     ADD CONSTRAINT `FK_CommunicationCampaigns_Societes_IdSociete` 
     FOREIGN KEY (`IdSociete`) 
     REFERENCES `Societes` (`IdSociete`) 
     ON DELETE SET NULL;',
    'SELECT ''Foreign key FK_CommunicationCampaigns_Societes_IdSociete already exists or table does not exist, skipping'' AS message'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ============================================================================
-- PARTIE 5: Vérification et création de la clé étrangère vers Utilisateurs
-- ============================================================================

SET @fk_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE 
    WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'CommunicationCampaigns'
    AND CONSTRAINT_NAME = 'FK_CommunicationCampaigns_Utilisateurs_IdUtilisateurCreateur'
);

SET @sql = IF(@fk_exists = 0 AND @table_exists = 0,
    'ALTER TABLE `CommunicationCampaigns` 
     ADD CONSTRAINT `FK_CommunicationCampaigns_Utilisateurs_IdUtilisateurCreateur` 
     FOREIGN KEY (`IdUtilisateurCreateur`) 
     REFERENCES `Utilisateurs` (`IdUtilisateur`) 
     ON DELETE RESTRICT;',
    'SELECT ''Foreign key FK_CommunicationCampaigns_Utilisateurs_IdUtilisateurCreateur already exists or table does not exist, skipping'' AS message'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ============================================================================
-- VÉRIFICATION FINALE
-- ============================================================================

SELECT 
    CASE 
        WHEN (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'CommunicationCampaigns') > 0 
        THEN '✅ Table CommunicationCampaigns créée avec succès'
        ELSE '❌ Erreur: Table CommunicationCampaigns non créée'
    END AS Status;

SELECT 
    COUNT(*) AS NombreIndex,
    'Index sur CommunicationCampaigns' AS Description
FROM INFORMATION_SCHEMA.STATISTICS 
WHERE TABLE_SCHEMA = DATABASE()
AND TABLE_NAME = 'CommunicationCampaigns';

SELECT 
    COUNT(*) AS NombreContraintes,
    'Contraintes de clés étrangères sur CommunicationCampaigns' AS Description
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE 
WHERE TABLE_SCHEMA = DATABASE()
AND TABLE_NAME = 'CommunicationCampaigns'
AND REFERENCED_TABLE_NAME IS NOT NULL;

-- ============================================================================
-- FIN DU SCRIPT
-- ============================================================================

