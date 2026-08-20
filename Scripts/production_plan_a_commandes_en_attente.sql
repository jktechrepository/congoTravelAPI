-- Plan A — Commandes FlexPay en attente (parité Transport)
-- Evenement + Restaurant + SiteTouristique
-- Appliquer sur MySQL prod après backup. Idempotent partiel via IF NOT EXISTS où possible.

-- ========== EVENEMENT ==========
CREATE TABLE IF NOT EXISTS `EvenementCommandesEnAttente` (
  `IdEvenementCommandeEnAttente` char(36) NOT NULL,
  `IdSociete` int NOT NULL,
  `IdEvenementSession` int NOT NULL,
  `IdSite` int NULL,
  `IdUtilisateur` int NULL,
  `IdClient` int NULL,
  `MethodePaiement` varchar(50) NOT NULL,
  `MontantTarif` decimal(18,2) NOT NULL,
  `CodeDeviseTarif` char(3) NOT NULL DEFAULT 'CDF',
  `MontantFlexPay` decimal(18,2) NOT NULL,
  `CodeDevisePaiement` char(3) NOT NULL DEFAULT 'CDF',
  `TauxVersDevisePaiement` decimal(18,8) NOT NULL DEFAULT 1,
  `OrderNumberFlexPay` varchar(120) NULL,
  `ReferenceFlexPay` varchar(120) NULL,
  `IdempotencyKey` varchar(120) NULL,
  `PayloadMetierJson` longtext NOT NULL,
  `IdPaiementEnAttente` int NULL,
  `DateCreation` datetime(6) NOT NULL,
  `DateExpiration` datetime(6) NULL,
  PRIMARY KEY (`IdEvenementCommandeEnAttente`),
  UNIQUE KEY `IX_EvenementCommandesEnAttente_Idempotency_UQ` (`IdempotencyKey`),
  KEY `IX_EvenementCommandesEnAttente_DateExpiration` (`DateExpiration`),
  KEY `IX_EvenementCommandesEnAttente_OrderNumberFlexPay` (`OrderNumberFlexPay`),
  KEY `IX_EvenementCommandesEnAttente_Societe_Session` (`IdSociete`,`IdEvenementSession`),
  KEY `IX_EvenementCommandesEnAttente_IdEvenementSession` (`IdEvenementSession`),
  KEY `IX_EvenementCommandesEnAttente_IdSite` (`IdSite`),
  CONSTRAINT `FK_EvenementCommandesEnAttente_EvenementSessions_IdEvenementSession`
    FOREIGN KEY (`IdEvenementSession`) REFERENCES `EvenementSessions` (`IdEvenementSession`) ON DELETE RESTRICT,
  CONSTRAINT `FK_EvenementCommandesEnAttente_Sites_IdSite`
    FOREIGN KEY (`IdSite`) REFERENCES `Sites` (`IdSite`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- EvenementPayments.IdEvenementReservation nullable + FK commande
ALTER TABLE `EvenementPayments`
  MODIFY COLUMN `IdEvenementReservation` int NULL;

SET @col_exists := (
  SELECT COUNT(*) FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'EvenementPayments' AND COLUMN_NAME = 'IdEvenementCommandeEnAttente');
SET @sql := IF(@col_exists = 0,
  'ALTER TABLE `EvenementPayments` ADD COLUMN `IdEvenementCommandeEnAttente` char(36) NULL',
  'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_exists := (
  SELECT COUNT(*) FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'EvenementSessionSeats' AND COLUMN_NAME = 'IdEvenementCommandeEnAttenteCourante');
SET @sql := IF(@col_exists = 0,
  'ALTER TABLE `EvenementSessionSeats` ADD COLUMN `IdEvenementCommandeEnAttenteCourante` char(36) NULL',
  'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- ========== RESTAURANT ==========
CREATE TABLE IF NOT EXISTS `RestaurantCommandesEnAttente` (
  `IdRestaurantCommandeEnAttente` char(36) NOT NULL,
  `IdSociete` int NOT NULL,
  `IdRestaurant` int NOT NULL,
  `IdRestaurantCreneau` int NOT NULL,
  `IdSite` int NULL,
  `IdUtilisateur` int NULL,
  `IdClient` int NULL,
  `MethodePaiement` varchar(50) NOT NULL,
  `MontantTarif` decimal(18,2) NOT NULL,
  `CodeDeviseTarif` char(3) NOT NULL DEFAULT 'CDF',
  `MontantFlexPay` decimal(18,2) NOT NULL,
  `CodeDevisePaiement` char(3) NOT NULL DEFAULT 'CDF',
  `TauxVersDevisePaiement` decimal(18,8) NOT NULL DEFAULT 1,
  `OrderNumberFlexPay` varchar(120) NULL,
  `ReferenceFlexPay` varchar(120) NULL,
  `IdempotencyKey` varchar(120) NULL,
  `PayloadMetierJson` longtext NOT NULL,
  `IdPaiementEnAttente` int NULL,
  `DateCreation` datetime(6) NOT NULL,
  `DateExpiration` datetime(6) NULL,
  PRIMARY KEY (`IdRestaurantCommandeEnAttente`),
  UNIQUE KEY `IX_RestaurantCommandesEnAttente_Idempotency_UQ` (`IdempotencyKey`),
  KEY `IX_RestaurantCommandesEnAttente_DateExpiration` (`DateExpiration`),
  KEY `IX_RestaurantCommandesEnAttente_OrderNumberFlexPay` (`OrderNumberFlexPay`),
  KEY `IX_RestaurantCommandesEnAttente_Societe_Creneau` (`IdSociete`,`IdRestaurantCreneau`),
  KEY `IX_RestaurantCommandesEnAttente_IdRestaurantCreneau` (`IdRestaurantCreneau`),
  KEY `IX_RestaurantCommandesEnAttente_IdSite` (`IdSite`),
  CONSTRAINT `FK_RestaurantCommandesEnAttente_RestaurantCreneaux_IdRestaurantCreneau`
    FOREIGN KEY (`IdRestaurantCreneau`) REFERENCES `RestaurantCreneaux` (`IdRestaurantCreneau`) ON DELETE RESTRICT,
  CONSTRAINT `FK_RestaurantCommandesEnAttente_Sites_IdSite`
    FOREIGN KEY (`IdSite`) REFERENCES `Sites` (`IdSite`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

ALTER TABLE `RestaurantPayments`
  MODIFY COLUMN `IdRestaurantReservation` int NULL;

SET @col_exists := (
  SELECT COUNT(*) FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'RestaurantPayments' AND COLUMN_NAME = 'IdRestaurantCommandeEnAttente');
SET @sql := IF(@col_exists = 0,
  'ALTER TABLE `RestaurantPayments` ADD COLUMN `IdRestaurantCommandeEnAttente` char(36) NULL',
  'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- ========== SITE TOURISTIQUE ==========
CREATE TABLE IF NOT EXISTS `SiteTouristiqueCommandesEnAttente` (
  `IdSiteTouristiqueCommandeEnAttente` char(36) NOT NULL,
  `IdSociete` int NOT NULL,
  `IdSiteTouristiqueJournee` int NOT NULL,
  `IdSite` int NULL,
  `IdUtilisateur` int NULL,
  `IdClient` int NULL,
  `MethodePaiement` varchar(50) NOT NULL,
  `MontantTarif` decimal(18,2) NOT NULL,
  `CodeDeviseTarif` char(3) NOT NULL DEFAULT 'CDF',
  `MontantFlexPay` decimal(18,2) NOT NULL,
  `CodeDevisePaiement` char(3) NOT NULL DEFAULT 'CDF',
  `TauxVersDevisePaiement` decimal(18,8) NOT NULL DEFAULT 1,
  `OrderNumberFlexPay` varchar(120) NULL,
  `ReferenceFlexPay` varchar(120) NULL,
  `IdempotencyKey` varchar(120) NULL,
  `PayloadMetierJson` longtext NOT NULL,
  `IdPaiementEnAttente` int NULL,
  `DateCreation` datetime(6) NOT NULL,
  `DateExpiration` datetime(6) NULL,
  PRIMARY KEY (`IdSiteTouristiqueCommandeEnAttente`),
  UNIQUE KEY `IX_SiteTouristiqueCommandesEnAttente_Idempotency_UQ` (`IdempotencyKey`),
  KEY `IX_SiteTouristiqueCommandesEnAttente_DateExpiration` (`DateExpiration`),
  KEY `IX_SiteTouristiqueCommandesEnAttente_OrderNumberFlexPay` (`OrderNumberFlexPay`),
  KEY `IX_SiteTouristiqueCommandesEnAttente_Societe_Journee` (`IdSociete`,`IdSiteTouristiqueJournee`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

ALTER TABLE `SiteTouristiquePayments`
  MODIFY COLUMN `IdSiteTouristiqueReservation` int NULL;

SET @col_exists := (
  SELECT COUNT(*) FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'SiteTouristiquePayments' AND COLUMN_NAME = 'IdSiteTouristiqueCommandeEnAttente');
SET @sql := IF(@col_exists = 0,
  'ALTER TABLE `SiteTouristiquePayments` ADD COLUMN `IdSiteTouristiqueCommandeEnAttente` char(36) NULL',
  'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
