-- =====================================================
-- Script SQL pour Production : Table ArriereesCrashed
-- =====================================================
-- Ce script crée la table ArriereesCrashed pour stocker
-- les arriérées qui ont échoué lors de l'import Excel
-- =====================================================

SET FOREIGN_KEY_CHECKS = 0;

-- =====================================================
-- 1. CRÉER LA TABLE ArriereesCrashed
-- =====================================================

CREATE TABLE IF NOT EXISTS `ArriereesCrashed` (
    `IdArriereeCrashed` INT NOT NULL AUTO_INCREMENT,
    
    -- Informations de la ligne Excel
    `NumeroLigne` INT NOT NULL,
    
    -- Données brutes de l'arriérée
    `CodeCons` VARCHAR(100) NULL,
    `Montant` VARCHAR(50) NULL,
    `Mois` VARCHAR(10) NULL,
    `Annees` VARCHAR(10) NULL,
    
    -- Identifiant du client si trouvé
    `IdClient` INT NULL,
    
    -- Données brutes en JSON
    `DonneesBrutesJson` TEXT NULL,
    
    -- Informations d'erreur
    `MessageErreur` TEXT NOT NULL,
    `TypeErreur` VARCHAR(50) NULL,
    `ErreursJson` TEXT NULL,
    
    -- Statut de la ligne échouée
    `Statut` VARCHAR(20) NOT NULL DEFAULT 'EN_ATTENTE',
    
    -- Identifiant de la ClientFacture créée si la correction a réussi
    `IdClientFactureCree` INT NULL,
    
    -- Dates
    `DateCreation` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `DateCorrection` DATETIME(6) NULL,
    `DateModification` DATETIME(6) NULL,
    
    PRIMARY KEY (`IdArriereeCrashed`),
    
    -- Index pour optimiser les requêtes
    INDEX `IX_ArriereeCrashed_Statut` (`Statut`),
    INDEX `IX_ArriereeCrashed_CodeCons` (`CodeCons`),
    INDEX `IX_ArriereeCrashed_DateCreation` (`DateCreation`),
    INDEX `IX_ArriereeCrashed_IdClient` (`IdClient`),
    INDEX `IX_ArriereeCrashed_IdClientFactureCree` (`IdClientFactureCree`),
    
    -- Contraintes de clés étrangères
    CONSTRAINT `FK_ArriereesCrashed_Clients_IdClient` 
        FOREIGN KEY (`IdClient`) 
        REFERENCES `Clients` (`IdClient`) 
        ON DELETE SET NULL,
    
    CONSTRAINT `FK_ArriereesCrashed_ClientFactures_IdClientFactureCree` 
        FOREIGN KEY (`IdClientFactureCree`) 
        REFERENCES `ClientFactures` (`IdClientFacture`) 
        ON DELETE SET NULL
        
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =====================================================
-- 2. COMMENTAIRES SUR LES COLONNES
-- =====================================================

ALTER TABLE `ArriereesCrashed` 
    MODIFY COLUMN `IdArriereeCrashed` INT NOT NULL AUTO_INCREMENT COMMENT 'Identifiant unique de l''arriérée échouée',
    MODIFY COLUMN `NumeroLigne` INT NOT NULL COMMENT 'Numéro de ligne dans le fichier Excel',
    MODIFY COLUMN `CodeCons` VARCHAR(100) NULL COMMENT 'Code consommateur (données brutes)',
    MODIFY COLUMN `Montant` VARCHAR(50) NULL COMMENT 'Montant de l''arriérée (données brutes)',
    MODIFY COLUMN `Mois` VARCHAR(10) NULL COMMENT 'Mois de l''arriérée (données brutes)',
    MODIFY COLUMN `Annees` VARCHAR(10) NULL COMMENT 'Année de l''arriérée (données brutes)',
    MODIFY COLUMN `IdClient` INT NULL COMMENT 'Identifiant du client si trouvé',
    MODIFY COLUMN `DonneesBrutesJson` TEXT NULL COMMENT 'Toutes les données brutes en JSON',
    MODIFY COLUMN `MessageErreur` TEXT NOT NULL COMMENT 'Message d''erreur détaillé',
    MODIFY COLUMN `TypeErreur` VARCHAR(50) NULL COMMENT 'Type d''erreur (CODE_CONS_NOT_FOUND, VALIDATION, DUPLICATE, etc.)',
    MODIFY COLUMN `ErreursJson` TEXT NULL COMMENT 'Liste des erreurs en JSON',
    MODIFY COLUMN `Statut` VARCHAR(20) NOT NULL DEFAULT 'EN_ATTENTE' COMMENT 'Statut: EN_ATTENTE, CORRIGE, IGNORE',
    MODIFY COLUMN `IdClientFactureCree` INT NULL COMMENT 'Identifiant de la ClientFacture créée si la correction a réussi',
    MODIFY COLUMN `DateCreation` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) COMMENT 'Date de création de l''enregistrement',
    MODIFY COLUMN `DateCorrection` DATETIME(6) NULL COMMENT 'Date de correction (quand la ligne a été corrigée)',
    MODIFY COLUMN `DateModification` DATETIME(6) NULL COMMENT 'Date de dernière modification';

SET FOREIGN_KEY_CHECKS = 1;

-- =====================================================
-- 3. VÉRIFICATION
-- =====================================================

SELECT '✅ Table ArriereesCrashed créée avec succès' as Message;

-- Afficher la structure de la table
DESCRIBE `ArriereesCrashed`;

-- Afficher les index créés
SHOW INDEX FROM `ArriereesCrashed`;

-- =====================================================
-- 4. NOTES IMPORTANTES
-- =====================================================
-- 
-- 1. La table stocke les arriérées qui ont échoué lors de l'import Excel
--    via l'endpoint POST /api/ClientFacture/bulk-excel
--
-- 2. Types d'erreurs possibles :
--    - CODE_CONS_NOT_FOUND : CodeCons non trouvé dans la base
--    - VALIDATION : Erreurs de validation (montant, mois, année)
--    - DUPLICATE : Doublon détecté
--
-- 3. Statuts possibles :
--    - EN_ATTENTE : En attente de correction
--    - CORRIGE : Corrigé et prêt à être réessayé
--    - IGNORE : Ignoré/désactivé
--
-- 4. Le champ IdClientFactureCree est rempli si la correction a réussi
--    via l'endpoint POST /api/ArriereeCrashed/{id}/retry
--
-- 5. Les données brutes sont stockées en JSON pour référence complète
--
-- =====================================================
