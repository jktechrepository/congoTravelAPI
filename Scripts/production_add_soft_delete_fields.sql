-- ============================================================================
-- Script SQL pour ajouter les champs de Soft Delete en production
-- ============================================================================
-- Description : Ajoute les colonnes Statut et IsDeleted pour implémenter
--               le soft delete sur les entités suivantes :
--               - Axe (Statut)
--               - Cabine (Statut)
--               - CommunicationCampaign (Statut)
--               - PlainteClient (Statut)
--               - Paiement (IsDeleted)
--
-- Date : 2025-01-05
-- Version : 1.0.0
-- ============================================================================

-- ═══════════════════════════════════════════════════════════════════════════
-- 1. AJOUT DE LA COLONNE Statut À LA TABLE Axes
-- ═══════════════════════════════════════════════════════════════════════════

-- Vérifier si la colonne existe déjà avant de l'ajouter
SET @column_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = DATABASE() 
    AND TABLE_NAME = 'Axes' 
    AND COLUMN_NAME = 'Statut'
);

SET @sql = IF(@column_exists = 0,
    'ALTER TABLE `Axes` ADD COLUMN `Statut` TINYINT(1) NOT NULL DEFAULT 1 COMMENT ''Statut de l''axe (actif/inactif) pour soft delete'' AFTER `IdCabine`;',
    'SELECT ''La colonne Statut existe déjà dans la table Axes'' AS message;'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Mettre à jour toutes les lignes existantes pour avoir Statut = 1 (actif)
UPDATE `Axes` SET `Statut` = 1 WHERE `Statut` IS NULL OR `Statut` = 0;

-- ═══════════════════════════════════════════════════════════════════════════
-- 2. AJOUT DE LA COLONNE Statut À LA TABLE Cabines
-- ═══════════════════════════════════════════════════════════════════════════

SET @column_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = DATABASE() 
    AND TABLE_NAME = 'Cabines' 
    AND COLUMN_NAME = 'Statut'
);

SET @sql = IF(@column_exists = 0,
    'ALTER TABLE `Cabines` ADD COLUMN `Statut` TINYINT(1) NOT NULL DEFAULT 1 COMMENT ''Statut de la cabine (actif/inactif) pour soft delete'' AFTER `IdSociete`;',
    'SELECT ''La colonne Statut existe déjà dans la table Cabines'' AS message;'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Mettre à jour toutes les lignes existantes pour avoir Statut = 1 (actif)
UPDATE `Cabines` SET `Statut` = 1 WHERE `Statut` IS NULL OR `Statut` = 0;

-- ═══════════════════════════════════════════════════════════════════════════
-- 3. AJOUT DE LA COLONNE Statut À LA TABLE CommunicationCampaigns
-- ═══════════════════════════════════════════════════════════════════════════

SET @column_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = DATABASE() 
    AND TABLE_NAME = 'CommunicationCampaigns' 
    AND COLUMN_NAME = 'Statut'
);

SET @sql = IF(@column_exists = 0,
    'ALTER TABLE `CommunicationCampaigns` ADD COLUMN `Statut` TINYINT(1) NOT NULL DEFAULT 1 COMMENT ''Statut de la campagne (actif/inactif) pour soft delete'' AFTER `DateEnvoiEffectif`;',
    'SELECT ''La colonne Statut existe déjà dans la table CommunicationCampaigns'' AS message;'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Mettre à jour toutes les lignes existantes pour avoir Statut = 1 (actif)
UPDATE `CommunicationCampaigns` SET `Statut` = 1 WHERE `Statut` IS NULL OR `Statut` = 0;

-- ═══════════════════════════════════════════════════════════════════════════
-- 4. AJOUT DE LA COLONNE Statut À LA TABLE PlainteClients
-- ═══════════════════════════════════════════════════════════════════════════

SET @column_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = DATABASE() 
    AND TABLE_NAME = 'PlainteClients' 
    AND COLUMN_NAME = 'Statut'
);

SET @sql = IF(@column_exists = 0,
    'ALTER TABLE `PlainteClients` ADD COLUMN `Statut` TINYINT(1) NOT NULL DEFAULT 1 COMMENT ''Statut de la plainte (actif/inactif) pour soft delete'' AFTER `DateDerniereModification`;',
    'SELECT ''La colonne Statut existe déjà dans la table PlainteClients'' AS message;'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Mettre à jour toutes les lignes existantes pour avoir Statut = 1 (actif)
UPDATE `PlainteClients` SET `Statut` = 1 WHERE `Statut` IS NULL OR `Statut` = 0;

-- ═══════════════════════════════════════════════════════════════════════════
-- 5. AJOUT DE LA COLONNE IsDeleted À LA TABLE Paiements
-- ═══════════════════════════════════════════════════════════════════════════

SET @column_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = DATABASE() 
    AND TABLE_NAME = 'Paiements' 
    AND COLUMN_NAME = 'IsDeleted'
);

SET @sql = IF(@column_exists = 0,
    'ALTER TABLE `Paiements` ADD COLUMN `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0 COMMENT ''Indique si le paiement est supprimé (soft delete)'' AFTER `Statut`;',
    'SELECT ''La colonne IsDeleted existe déjà dans la table Paiements'' AS message;'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Mettre à jour toutes les lignes existantes pour avoir IsDeleted = 0 (non supprimé)
UPDATE `Paiements` SET `IsDeleted` = 0 WHERE `IsDeleted` IS NULL;

-- ═══════════════════════════════════════════════════════════════════════════
-- 6. CRÉATION D'INDEX POUR OPTIMISER LES REQUÊTES
-- ═══════════════════════════════════════════════════════════════════════════

-- Index sur Statut pour Axes (si n'existe pas déjà)
SET @index_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.STATISTICS 
    WHERE TABLE_SCHEMA = DATABASE() 
    AND TABLE_NAME = 'Axes' 
    AND INDEX_NAME = 'IX_Axes_Statut'
);

SET @sql = IF(@index_exists = 0,
    'CREATE INDEX `IX_Axes_Statut` ON `Axes` (`Statut`);',
    'SELECT ''L''index IX_Axes_Statut existe déjà'' AS message;'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Index sur Statut pour Cabines (si n'existe pas déjà)
SET @index_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.STATISTICS 
    WHERE TABLE_SCHEMA = DATABASE() 
    AND TABLE_NAME = 'Cabines' 
    AND INDEX_NAME = 'IX_Cabines_Statut'
);

SET @sql = IF(@index_exists = 0,
    'CREATE INDEX `IX_Cabines_Statut` ON `Cabines` (`Statut`);',
    'SELECT ''L''index IX_Cabines_Statut existe déjà'' AS message;'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Index sur Statut pour CommunicationCampaigns (si n'existe pas déjà)
SET @index_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.STATISTICS 
    WHERE TABLE_SCHEMA = DATABASE() 
    AND TABLE_NAME = 'CommunicationCampaigns' 
    AND INDEX_NAME = 'IX_CommunicationCampaigns_Statut'
);

SET @sql = IF(@index_exists = 0,
    'CREATE INDEX `IX_CommunicationCampaigns_Statut` ON `CommunicationCampaigns` (`Statut`);',
    'SELECT ''L''index IX_CommunicationCampaigns_Statut existe déjà'' AS message;'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Index sur Statut pour PlainteClients (si n'existe pas déjà)
SET @index_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.STATISTICS 
    WHERE TABLE_SCHEMA = DATABASE() 
    AND TABLE_NAME = 'PlainteClients' 
    AND INDEX_NAME = 'IX_PlainteClients_Statut'
);

SET @sql = IF(@index_exists = 0,
    'CREATE INDEX `IX_PlainteClients_Statut` ON `PlainteClients` (`Statut`);',
    'SELECT ''L''index IX_PlainteClients_Statut existe déjà'' AS message;'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Index sur IsDeleted pour Paiements (si n'existe pas déjà)
SET @index_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.STATISTICS 
    WHERE TABLE_SCHEMA = DATABASE() 
    AND TABLE_NAME = 'Paiements' 
    AND INDEX_NAME = 'IX_Paiements_IsDeleted'
);

SET @sql = IF(@index_exists = 0,
    'CREATE INDEX `IX_Paiements_IsDeleted` ON `Paiements` (`IsDeleted`);',
    'SELECT ''L''index IX_Paiements_IsDeleted existe déjà'' AS message;'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ═══════════════════════════════════════════════════════════════════════════
-- 7. VALIDATION DES MODIFICATIONS
-- ═══════════════════════════════════════════════════════════════════════════

-- Afficher un résumé des colonnes ajoutées
SELECT 
    'Résumé des colonnes ajoutées' AS titre,
    TABLE_NAME AS table_name,
    COLUMN_NAME AS column_name,
    COLUMN_TYPE AS column_type,
    COLUMN_DEFAULT AS default_value,
    IS_NULLABLE AS nullable
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
AND (
    (TABLE_NAME = 'Axes' AND COLUMN_NAME = 'Statut') OR
    (TABLE_NAME = 'Cabines' AND COLUMN_NAME = 'Statut') OR
    (TABLE_NAME = 'CommunicationCampaigns' AND COLUMN_NAME = 'Statut') OR
    (TABLE_NAME = 'PlainteClients' AND COLUMN_NAME = 'Statut') OR
    (TABLE_NAME = 'Paiements' AND COLUMN_NAME = 'IsDeleted')
)
ORDER BY TABLE_NAME, COLUMN_NAME;

-- Afficher un résumé des index créés
SELECT 
    'Résumé des index créés' AS titre,
    TABLE_NAME AS table_name,
    INDEX_NAME AS index_name,
    COLUMN_NAME AS column_name
FROM INFORMATION_SCHEMA.STATISTICS
WHERE TABLE_SCHEMA = DATABASE()
AND (
    INDEX_NAME = 'IX_Axes_Statut' OR
    INDEX_NAME = 'IX_Cabines_Statut' OR
    INDEX_NAME = 'IX_CommunicationCampaigns_Statut' OR
    INDEX_NAME = 'IX_PlainteClients_Statut' OR
    INDEX_NAME = 'IX_Paiements_IsDeleted'
)
ORDER BY TABLE_NAME, INDEX_NAME;

-- ═══════════════════════════════════════════════════════════════════════════
-- FIN DU SCRIPT
-- ═══════════════════════════════════════════════════════════════════════════

SELECT '✅ Script d''ajout des champs de soft delete exécuté avec succès !' AS message;
