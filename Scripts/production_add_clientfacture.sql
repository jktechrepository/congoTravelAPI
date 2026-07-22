-- =====================================================
-- Script SQL de Production : Ajout de la table ClientFactures
-- Date : 2025-01-05
-- Description : Création de la table ClientFactures pour gérer les arriérés pré-existants
--               et optimiser les calculs d'arriérés
-- =====================================================

-- Désactiver temporairement les vérifications de clés étrangères pour éviter les erreurs
SET FOREIGN_KEY_CHECKS = 0;

-- =====================================================
-- 1. Création de la table ClientFactures
-- =====================================================

CREATE TABLE IF NOT EXISTS `ClientFactures` (
    `IdClientFacture` INT AUTO_INCREMENT PRIMARY KEY,
    `IdFacture` INT NULL COMMENT 'NULL pour arriérés pré-existants',
    `IdClient` INT NOT NULL COMMENT 'Obligatoire',
    `Montant` DECIMAL(18,2) NULL COMMENT 'Montant total (déjà multiplié par nombreBatiment)',
    `nombreBatiment` INT NULL COMMENT 'Snapshot du nombre de bâtiments au moment de la facture',
    `MontantPaye` DECIMAL(18,2) NULL DEFAULT 0 COMMENT 'Montant déjà payé (pré-calculé)',
    `MontantDu` DECIMAL(18,2) NULL COMMENT 'Montant restant dû (pré-calculé)',
    `Mois` VARCHAR(20) NULL COMMENT 'Mois d''émission (format: "01", "02", ..., "12" ou "Janvier", etc.)',
    `Annees` INT NULL COMMENT 'Année d''émission (2000-2100)',
    `DateEmission` DATETIME(6) NULL COMMENT 'Date d''émission (plus fiable que Mois/Annees)',
    `EstArrierePreExistant` TINYINT(1) NOT NULL DEFAULT FALSE COMMENT 'Flag pour arriérés pré-existants',
    `Description` VARCHAR(500) NULL COMMENT 'Description/libellé pour les arriérés pré-existants',
    `Statut` TINYINT(1) NOT NULL DEFAULT TRUE COMMENT 'Statut actif/inactif (soft delete)',
    `DateCreation` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) COMMENT 'Date de création',
    `DateModification` DATETIME(6) NULL COMMENT 'Date de dernière modification',
    
    -- Contraintes de clés étrangères
    CONSTRAINT `FK_ClientFactures_Clients_IdClient` 
        FOREIGN KEY (`IdClient`) 
        REFERENCES `Clients` (`IdClient`) 
        ON DELETE RESTRICT 
        ON UPDATE CASCADE,
    
    CONSTRAINT `FK_ClientFactures_Factures_IdFacture` 
        FOREIGN KEY (`IdFacture`) 
        REFERENCES `Factures` (`IdFacture`) 
        ON DELETE SET NULL 
        ON UPDATE CASCADE,
    
    -- Index pour optimiser les requêtes
    INDEX `IX_ClientFacture_IdClient` (`IdClient`),
    INDEX `IX_ClientFacture_IdFacture` (`IdFacture`),
    INDEX `IX_ClientFacture_Client_Mois_Annees` (`IdClient`, `Mois`, `Annees`),
    INDEX `IX_ClientFacture_MontantDu` (`MontantDu`),
    INDEX `IX_ClientFacture_DateEmission` (`DateEmission`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Table de liaison Client-Facture pour gérer les arriérés et optimiser les calculs';

-- =====================================================
-- 2. Commentaires sur les colonnes
-- =====================================================

ALTER TABLE `ClientFactures` 
    MODIFY COLUMN `IdFacture` INT NULL COMMENT 'NULL pour arriérés pré-existants (avant informatisation)',
    MODIFY COLUMN `IdClient` INT NOT NULL COMMENT 'Obligatoire - Client concerné',
    MODIFY COLUMN `Montant` DECIMAL(18,2) NULL COMMENT 'Montant total pour ce client (facture.Montant × nombreBatiment)',
    MODIFY COLUMN `nombreBatiment` INT NULL COMMENT 'Snapshot du nombreBatiment au moment de la facture',
    MODIFY COLUMN `MontantPaye` DECIMAL(18,2) NULL DEFAULT 0 COMMENT 'Somme des paiements validés (pré-calculé)',
    MODIFY COLUMN `MontantDu` DECIMAL(18,2) NULL COMMENT 'Montant restant dû = Montant - MontantPaye',
    MODIFY COLUMN `Mois` VARCHAR(20) NULL COMMENT 'Mois d''émission (format libre: "01", "Janvier", etc.)',
    MODIFY COLUMN `Annees` INT NULL COMMENT 'Année d''émission (2000-2100)',
    MODIFY COLUMN `DateEmission` DATETIME(6) NULL COMMENT 'Date d''émission (plus fiable pour tri et filtrage)',
    MODIFY COLUMN `EstArrierePreExistant` TINYINT(1) NOT NULL DEFAULT FALSE COMMENT 'TRUE si arriéré avant informatisation',
    MODIFY COLUMN `Description` VARCHAR(500) NULL COMMENT 'Description/libellé pour les arriérés pré-existants',
    MODIFY COLUMN `Statut` TINYINT(1) NOT NULL DEFAULT TRUE COMMENT 'TRUE = actif, FALSE = inactif (soft delete)';

-- =====================================================
-- 3. Vérification de la création
-- =====================================================

-- Vérifier que la table existe
SELECT 
    TABLE_NAME,
    TABLE_COMMENT,
    ENGINE,
    TABLE_COLLATION
FROM 
    INFORMATION_SCHEMA.TABLES
WHERE 
    TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'ClientFactures';

-- Vérifier les index créés
SELECT 
    INDEX_NAME,
    COLUMN_NAME,
    SEQ_IN_INDEX,
    NON_UNIQUE
FROM 
    INFORMATION_SCHEMA.STATISTICS
WHERE 
    TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'ClientFactures'
ORDER BY 
    INDEX_NAME, SEQ_IN_INDEX;

-- Réactiver les vérifications de clés étrangères
SET FOREIGN_KEY_CHECKS = 1;

-- =====================================================
-- 4. Notes importantes
-- =====================================================

-- IMPORTANT :
-- 1. Cette table permet de gérer les arriérés pré-existants (IdFacture = NULL)
-- 2. Les montants sont pré-calculés pour optimiser les performances
-- 3. Le nombreBatiment est un snapshot pour éviter les recalculs
-- 4. Les index sont optimisés pour les requêtes d'arriérés
-- 5. La migration des données existantes se fera via un script séparé

-- =====================================================
-- Fin du script
-- =====================================================
