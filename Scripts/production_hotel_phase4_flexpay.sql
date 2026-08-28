-- CongoTravel — Hôtel Phase 4 : FlexPay Plan A (idempotent)
-- Prérequis : production_hotel_phase3_reservations.sql
SET NAMES utf8mb4;

ALTER TABLE `HotelPayments`
  MODIFY COLUMN `IdHotelReservation` int NULL;

SET @col_exists := (
  SELECT COUNT(*) FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'HotelPayments'
    AND COLUMN_NAME = 'IdHotelCommandeEnAttente');
SET @sql := IF(
  @col_exists = 0,
  'ALTER TABLE `HotelPayments` ADD COLUMN `IdHotelCommandeEnAttente` char(36) NULL AFTER `IdHotelReservation`',
  'SELECT ''HotelPayments.IdHotelCommandeEnAttente déjà présent'' AS Info');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

CREATE TABLE IF NOT EXISTS `HotelCommandesEnAttente` (
  `IdHotelCommandeEnAttente` char(36) NOT NULL,
  `IdSociete` int NOT NULL,
  `IdHotel` int NOT NULL,
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
  PRIMARY KEY (`IdHotelCommandeEnAttente`),
  UNIQUE KEY `IX_HotelCommandesEnAttente_Idempotency_UQ` (`IdempotencyKey`),
  KEY `IX_HotelCommandesEnAttente_DateExpiration` (`DateExpiration`),
  KEY `IX_HotelCommandesEnAttente_OrderNumberFlexPay` (`OrderNumberFlexPay`),
  KEY `IX_HotelCommandesEnAttente_Societe_Hotel` (`IdSociete`,`IdHotel`),
  KEY `IX_HotelCommandesEnAttente_IdHotel` (`IdHotel`),
  KEY `IX_HotelCommandesEnAttente_IdSite` (`IdSite`),
  KEY `IX_HotelCommandesEnAttente_IdPaiement` (`IdPaiementEnAttente`),
  CONSTRAINT `FK_HotelCommandesEnAttente_Hotels`
    FOREIGN KEY (`IdHotel`) REFERENCES `Hotels` (`IdHotel`) ON DELETE RESTRICT,
  CONSTRAINT `FK_HotelCommandesEnAttente_Sites`
    FOREIGN KEY (`IdSite`) REFERENCES `Sites` (`IdSite`) ON DELETE RESTRICT,
  CONSTRAINT `FK_HotelCommandesEnAttente_Payment`
    FOREIGN KEY (`IdPaiementEnAttente`) REFERENCES `HotelPayments` (`IdHotelPayment`) ON DELETE SET NULL
) CHARACTER SET=utf8mb4;

DROP PROCEDURE IF EXISTS `sp_InstallHotelPhase4FlexPay`;
DELIMITER $$
CREATE PROCEDURE `sp_InstallHotelPhase4FlexPay`()
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.TABLE_CONSTRAINTS
    WHERE CONSTRAINT_SCHEMA = DATABASE()
      AND TABLE_NAME = 'HotelPayments'
      AND CONSTRAINT_NAME = 'FK_HotelPayments_CommandeEnAttente'
  ) THEN
    ALTER TABLE `HotelPayments`
      ADD CONSTRAINT `FK_HotelPayments_CommandeEnAttente`
      FOREIGN KEY (`IdHotelCommandeEnAttente`)
      REFERENCES `HotelCommandesEnAttente` (`IdHotelCommandeEnAttente`) ON DELETE RESTRICT;
  END IF;
END$$
DELIMITER ;
CALL `sp_InstallHotelPhase4FlexPay`();
DROP PROCEDURE `sp_InstallHotelPhase4FlexPay`;

SELECT 'production_hotel_phase4_flexpay.sql appliqué' AS Info;

-- Configuration API :
-- FlexPay:HotelEnabled=true
-- FlexPay:HotelCallbackRelativePath=/api/hotels/flexpay/callback
