-- =====================================================
-- Script SQL : Création de la table clientsCrashed
-- =====================================================
-- Description : Crée la table clientsCrashed pour stocker
--               les lignes de clients qui ont échoué lors
--               de l'import Excel, permettant de corriger
--               et réessayer ultérieurement.
-- 
-- Date : 2025-01-05
-- Version : 1.0.0
-- =====================================================

-- =====================================================
-- ÉTAPE 1 : Création de la table clientsCrashed
-- =====================================================

CREATE TABLE IF NOT EXISTS `clientsCrashed` (
    `IdClientCrashed` INT NOT NULL AUTO_INCREMENT,
    `IdSociete` INT NOT NULL,
    `NumeroLigne` INT NOT NULL,
    `NomClient` VARCHAR(200) NULL,
    `AdresseClient` VARCHAR(500) NULL,
    `Telephone` VARCHAR(20) NULL,
    `EmailClient` VARCHAR(256) NULL,
    `GenreClient` VARCHAR(10) NULL,
    `CodeCons` VARCHAR(100) NULL,
    `LibelleUsage` TEXT NULL,
    `DonneesBrutesJson` TEXT NULL,
    `MessageErreur` TEXT NOT NULL,
    `TypeErreur` VARCHAR(50) NULL,
    `ErreursJson` TEXT NULL,
    `Statut` VARCHAR(20) NOT NULL DEFAULT 'EN_ATTENTE',
    `IdClientCree` INT NULL,
    `DateCreation` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `DateCorrection` DATETIME(6) NULL,
    `DateModification` DATETIME(6) NULL,
    PRIMARY KEY (`IdClientCrashed`),
    INDEX `IX_ClientCrashed_IdSociete` (`IdSociete`),
    INDEX `IX_ClientCrashed_Statut` (`Statut`),
    INDEX `IX_ClientCrashed_DateCreation` (`DateCreation`),
    CONSTRAINT `FK_ClientCrashed_Societe` 
        FOREIGN KEY (`IdSociete`) 
        REFERENCES `Societes` (`IdSociete`) 
        ON DELETE RESTRICT,
    CONSTRAINT `FK_ClientCrashed_Client` 
        FOREIGN KEY (`IdClientCree`) 
        REFERENCES `Clients` (`IdClient`) 
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =====================================================
-- ÉTAPE 2 : Vérification de la création
-- =====================================================

-- Vérifier que la table a bien été créée
SHOW TABLES LIKE 'clientsCrashed';

-- Afficher la structure de la table
DESCRIBE `clientsCrashed`;

-- =====================================================
-- ÉTAPE 3 : Vérification des index
-- =====================================================

SHOW INDEX FROM `clientsCrashed`;

-- =====================================================
-- NOTES IMPORTANTES
-- =====================================================
-- 1. La table stocke toutes les lignes qui échouent lors de l'import Excel
-- 2. Le champ Statut peut avoir les valeurs :
--    - EN_ATTENTE : En attente de correction
--    - CORRIGE : Corrigé et prêt à être réessayé
--    - IGNORE : Ignoré/désactivé
-- 3. Le champ IdClientCree est rempli si la correction a réussi
-- 4. Les données brutes sont stockées en JSON pour référence complète
-- 5. Les erreurs sont stockées dans MessageErreur et ErreursJson

-- =====================================================
-- ROLLBACK (si nécessaire)
-- =====================================================
-- Pour supprimer la table si nécessaire :
-- DROP TABLE IF EXISTS `clientsCrashed`;
