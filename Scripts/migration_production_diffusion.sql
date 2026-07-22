-- ============================================================================
-- Script de migration pour la production : Diffusion de factures
-- Base de données: MariaDB / MySQL
-- Version: 1.0
-- Date: Décembre 2025
-- ============================================================================
-- 
-- INSTRUCTIONS:
-- 1. Faites une sauvegarde de votre base de données avant d'exécuter ce script
-- 2. Exécutez ce script dans votre base de données de production
-- 3. Ce script est idempotent (peut être exécuté plusieurs fois sans erreur)
-- 
-- ============================================================================

SET FOREIGN_KEY_CHECKS = 0;
SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET AUTOCOMMIT = 0;
START TRANSACTION;
SET time_zone = "+00:00";

-- ============================================================================
-- 1. AJOUT DES CHAMPS DANS LA TABLE Factures
-- ============================================================================

-- Ajouter le champ EstDiffusee (boolean, défaut: false)
-- Note: Si la colonne existe déjà, cette commande échouera silencieusement
SET @exist := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_SCHEMA = DATABASE() 
               AND TABLE_NAME = 'Factures' 
               AND COLUMN_NAME = 'EstDiffusee');
SET @sqlstmt := IF(@exist = 0, 
    'ALTER TABLE `Factures` ADD COLUMN `EstDiffusee` tinyint(1) NOT NULL DEFAULT 0 COMMENT ''Indique si la facture a déjà été diffusée aux clients''', 
    'SELECT ''Colonne EstDiffusee existe déjà'' AS message');
PREPARE stmt FROM @sqlstmt;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Ajouter le champ DateDiffusion (datetime nullable)
SET @exist := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_SCHEMA = DATABASE() 
               AND TABLE_NAME = 'Factures' 
               AND COLUMN_NAME = 'DateDiffusion');
SET @sqlstmt := IF(@exist = 0, 
    'ALTER TABLE `Factures` ADD COLUMN `DateDiffusion` datetime(6) NULL COMMENT ''Date de dernière diffusion de la facture''', 
    'SELECT ''Colonne DateDiffusion existe déjà'' AS message');
PREPARE stmt FROM @sqlstmt;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ============================================================================
-- 2. CRÉATION DE LA TABLE NotificationPreferences
-- ============================================================================

CREATE TABLE IF NOT EXISTS `NotificationPreferences` (
  `IdNotificationPreference` int NOT NULL AUTO_INCREMENT,
  `IdUtilisateur` int NOT NULL,
  `AllowPush` tinyint(1) NOT NULL DEFAULT 1,
  `AllowInApp` tinyint(1) NOT NULL DEFAULT 1,
  `AllowSms` tinyint(1) NOT NULL DEFAULT 1,
  `AllowEmail` tinyint(1) NOT NULL DEFAULT 1,
  `OptOutGlobal` tinyint(1) NOT NULL DEFAULT 0,
  `OptOutFactures` tinyint(1) NOT NULL DEFAULT 0,
  `DateCreation` datetime(6) NOT NULL,
  `DateModification` datetime(6) NOT NULL,
  PRIMARY KEY (`IdNotificationPreference`),
  UNIQUE KEY `IX_NotificationPreferences_IdUtilisateur` (`IdUtilisateur`),
  CONSTRAINT `FK_NotificationPreferences_Utilisateurs_IdUtilisateur` 
    FOREIGN KEY (`IdUtilisateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`) 
    ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================================
-- 3. CRÉATION DE LA TABLE DiffusionStatistiques
-- ============================================================================

CREATE TABLE IF NOT EXISTS `DiffusionStatistiques` (
  `IdDiffusionStatistique` int NOT NULL AUTO_INCREMENT,
  `IdFacture` int NOT NULL,
  `IdCategorie` int NOT NULL,
  `TotalClients` int NOT NULL DEFAULT 0,
  `ClientsNotifies` int NOT NULL DEFAULT 0,
  `ClientsEchecs` int NOT NULL DEFAULT 0,
  `StatistiquesCanaux` varchar(1000) DEFAULT NULL,
  `DateDebut` datetime(6) NOT NULL,
  `DateFin` datetime(6) DEFAULT NULL,
  `DureeSecondes` double DEFAULT NULL,
  `Statut` varchar(20) NOT NULL DEFAULT 'En cours',
  `IdUtilisateurLanceur` int DEFAULT NULL,
  PRIMARY KEY (`IdDiffusionStatistique`),
  KEY `IX_DiffusionStatistiques_IdFacture` (`IdFacture`),
  KEY `IX_DiffusionStatistiques_IdCategorie` (`IdCategorie`),
  CONSTRAINT `FK_DiffusionStatistiques_Factures_IdFacture` 
    FOREIGN KEY (`IdFacture`) REFERENCES `Factures` (`IdFacture`) 
    ON DELETE CASCADE,
  CONSTRAINT `FK_DiffusionStatistiques_CategorieClients_IdCategorie` 
    FOREIGN KEY (`IdCategorie`) REFERENCES `CategorieClients` (`IdCategorie`) 
    ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================================
-- 4. MISE À JOUR DES FACTURES EXISTANTES
-- ============================================================================

-- Initialiser EstDiffusee à false pour toutes les factures existantes
UPDATE `Factures` 
SET `EstDiffusee` = 0 
WHERE `EstDiffusee` IS NULL;

-- ============================================================================
-- FIN DU SCRIPT
-- ============================================================================

SET FOREIGN_KEY_CHECKS = 1;
COMMIT;

-- ============================================================================
-- NOTES IMPORTANTES:
-- ============================================================================
-- 1. Le champ EstDiffusee est initialisé à false pour toutes les factures existantes
-- 2. La table NotificationPreferences permet aux utilisateurs de gérer leurs préférences
-- 3. La table DiffusionStatistiques permet de suivre les statistiques de diffusion
-- 4. Toutes les contraintes de clés étrangères sont en CASCADE pour l'intégrité
-- 5. Les index sont créés pour optimiser les requêtes fréquentes
-- ============================================================================

