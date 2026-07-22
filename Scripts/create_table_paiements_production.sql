-- ============================================================================
-- Script de création de la table Paiements pour la production
-- Base de données: MariaDB / MySQL
-- Version: 1.0
-- Date: Décembre 2025
-- ============================================================================
-- 
-- INSTRUCTIONS:
-- 1. Assurez-vous que les tables suivantes existent déjà:
--    - Factures
--    - Clients
--    - Utilisateurs
-- 2. Exécutez ce script dans votre base de données de production
-- 
-- ============================================================================

SET FOREIGN_KEY_CHECKS = 0;
SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET AUTOCOMMIT = 0;
START TRANSACTION;
SET time_zone = "+00:00";

-- ============================================================================
-- TABLE: Paiements
-- ============================================================================
CREATE TABLE IF NOT EXISTS `Paiements` (
  `IdPaiement` int NOT NULL AUTO_INCREMENT,
  `IdFacture` int NOT NULL,
  `IdClient` int DEFAULT NULL,
  `MontantPaye` decimal(18,2) NOT NULL,
  `MontantAPaye` decimal(18,2) DEFAULT NULL,
  `ResteAPaye` decimal(18,2) DEFAULT NULL,
  `DatePaiement` datetime(6) NOT NULL,
  `MethodePaiement` varchar(50) DEFAULT NULL,
  `ReferenceTransaction` varchar(100) DEFAULT NULL,
  `Commentaire` varchar(500) DEFAULT NULL,
  `Statut` varchar(20) NOT NULL DEFAULT 'Validé',
  `IdUtilisateurEnregistrement` int DEFAULT NULL,
  `DateCreation` datetime(6) NOT NULL,
  PRIMARY KEY (`IdPaiement`),
  KEY `IX_Paiements_IdFacture` (`IdFacture`),
  KEY `IX_Paiements_IdClient` (`IdClient`),
  KEY `IX_Paiements_DatePaiement` (`DatePaiement`),
  KEY `IX_Paiements_IdUtilisateurEnregistrement` (`IdUtilisateurEnregistrement`),
  CONSTRAINT `FK_Paiements_Factures_IdFacture` FOREIGN KEY (`IdFacture`) REFERENCES `Factures` (`IdFacture`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Paiements_Clients_IdClient` FOREIGN KEY (`IdClient`) REFERENCES `Clients` (`IdClient`) ON DELETE SET NULL,
  CONSTRAINT `FK_Paiements_Utilisateurs_IdUtilisateurEnregistrement` FOREIGN KEY (`IdUtilisateurEnregistrement`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================================
-- FIN DU SCRIPT
-- ============================================================================

SET FOREIGN_KEY_CHECKS = 1;
COMMIT;

-- ============================================================================
-- NOTES IMPORTANTES:
-- ============================================================================
-- 1. Cette table stocke tous les paiements effectués sur les factures
-- 2. Les colonnes MontantAPaye et ResteAPaye sont optionnelles (nullable)
-- 3. Le champ Statut a une valeur par défaut 'Validé'
-- 4. Les contraintes de clés étrangères assurent l'intégrité référentielle:
--    - IdFacture: référence Factures (RESTRICT sur suppression)
--    - IdClient: référence Clients (SET NULL sur suppression)
--    - IdUtilisateurEnregistrement: référence Utilisateurs (SET NULL sur suppression)
-- 5. Les index sont créés pour optimiser les requêtes fréquentes
-- ============================================================================

