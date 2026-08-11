-- =============================================================================
-- CongoTravel — Billetterie Site Touristique V1 (tables + ConfigSociete)
-- =============================================================================
-- Prérequis : Societes, Sites, ConfigSocietes
-- Idempotent : CREATE TABLE IF NOT EXISTS + garde-fou colonne.
-- =============================================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- Config : durée hold site touristique
SET @col_exists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'ConfigSocietes'
      AND COLUMN_NAME = 'DureeHoldSiteTouristiqueMinutes'
);
SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE `ConfigSocietes` ADD `DureeHoldSiteTouristiqueMinutes` int NOT NULL DEFAULT 15',
    'SELECT ''ConfigSocietes.DureeHoldSiteTouristiqueMinutes déjà présent'' AS Info'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

CREATE TABLE IF NOT EXISTS `SiteTouristiqueClasses` (
    `IdSiteTouristiqueClasse` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `Code` varchar(50) CHARACTER SET utf8mb4 NULL,
    `Libelle` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
    `Description` varchar(500) CHARACTER SET utf8mb4 NULL,
    `Actif` tinyint(1) NOT NULL DEFAULT TRUE,
    `DateCreation` datetime(6) NOT NULL,
    CONSTRAINT `PK_SiteTouristiqueClasses` PRIMARY KEY (`IdSiteTouristiqueClasse`),
    CONSTRAINT `FK_SiteTouristiqueClasses_Societes_IdSociete`
        FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `SiteTouristiques` (
    `IdSiteTouristique` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `IdSite` int NULL,
    `CodeLieu` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `Nom` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Description` varchar(2000) CHARACTER SET utf8mb4 NULL,
    `Status` enum('Draft','Published','Closed','Cancelled') CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Draft',
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_SiteTouristiques` PRIMARY KEY (`IdSiteTouristique`),
    CONSTRAINT `FK_SiteTouristiques_Societes_IdSociete`
        FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
    CONSTRAINT `FK_SiteTouristiques_Sites_IdSite`
        FOREIGN KEY (`IdSite`) REFERENCES `Sites` (`IdSite`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `SiteTouristiqueJournees` (
    `IdSiteTouristiqueJournee` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `IdSiteTouristique` int NOT NULL,
    `DateVisite` date NOT NULL,
    `InventoryMode` enum('ClassQuota','GlobalQuota') CHARACTER SET utf8mb4 NOT NULL,
    `Status` enum('Draft','Published','Closed','Cancelled') CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Draft',
    `CodeDevise` char(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF',
    `SalesOpenAtUtc` datetime(6) NULL,
    `SalesCloseAtUtc` datetime(6) NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_SiteTouristiqueJournees` PRIMARY KEY (`IdSiteTouristiqueJournee`),
    CONSTRAINT `CK_SiteTouristiqueJournees_SalesWindow`
        CHECK (`SalesCloseAtUtc` IS NULL OR `SalesOpenAtUtc` IS NULL OR `SalesCloseAtUtc` >= `SalesOpenAtUtc`),
    CONSTRAINT `FK_SiteTouristiqueJournees_Societes_IdSociete`
        FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
    CONSTRAINT `FK_SiteTouristiqueJournees_SiteTouristiques_IdSiteTouristique`
        FOREIGN KEY (`IdSiteTouristique`) REFERENCES `SiteTouristiques` (`IdSiteTouristique`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `SiteTouristiqueGlobalQuotas` (
    `IdSiteTouristiqueJournee` int NOT NULL,
    `CapaciteTotale` int NOT NULL,
    `QuantiteHold` int NOT NULL DEFAULT 0,
    `QuantiteVendue` int NOT NULL DEFAULT 0,
    `PrixUnitaire` decimal(18,2) NOT NULL,
    CONSTRAINT `PK_SiteTouristiqueGlobalQuotas` PRIMARY KEY (`IdSiteTouristiqueJournee`),
    CONSTRAINT `CK_SiteTouristiqueGlobalQuotas_Capacite` CHECK (`CapaciteTotale` >= 0),
    CONSTRAINT `CK_SiteTouristiqueGlobalQuotas_StockPositive` CHECK (`QuantiteHold` >= 0 AND `QuantiteVendue` >= 0),
    CONSTRAINT `CK_SiteTouristiqueGlobalQuotas_StockMax` CHECK (`QuantiteHold` + `QuantiteVendue` <= `CapaciteTotale`),
    CONSTRAINT `FK_SiteTouristiqueGlobalQuotas_Journees`
        FOREIGN KEY (`IdSiteTouristiqueJournee`) REFERENCES `SiteTouristiqueJournees` (`IdSiteTouristiqueJournee`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `SiteTouristiqueClassQuotas` (
    `IdSiteTouristiqueClassQuota` int NOT NULL AUTO_INCREMENT,
    `IdSiteTouristiqueJournee` int NOT NULL,
    `IdSiteTouristiqueClasse` int NOT NULL,
    `CapaciteTotale` int NOT NULL,
    `QuantiteHold` int NOT NULL DEFAULT 0,
    `QuantiteVendue` int NOT NULL DEFAULT 0,
    `PrixUnitaire` decimal(18,2) NOT NULL,
    CONSTRAINT `PK_SiteTouristiqueClassQuotas` PRIMARY KEY (`IdSiteTouristiqueClassQuota`),
    CONSTRAINT `CK_SiteTouristiqueClassQuotas_Capacite` CHECK (`CapaciteTotale` >= 0),
    CONSTRAINT `CK_SiteTouristiqueClassQuotas_StockPositive` CHECK (`QuantiteHold` >= 0 AND `QuantiteVendue` >= 0),
    CONSTRAINT `CK_SiteTouristiqueClassQuotas_StockMax` CHECK (`QuantiteHold` + `QuantiteVendue` <= `CapaciteTotale`),
    CONSTRAINT `FK_SiteTouristiqueClassQuotas_Journees`
        FOREIGN KEY (`IdSiteTouristiqueJournee`) REFERENCES `SiteTouristiqueJournees` (`IdSiteTouristiqueJournee`) ON DELETE CASCADE,
    CONSTRAINT `FK_SiteTouristiqueClassQuotas_Classes`
        FOREIGN KEY (`IdSiteTouristiqueClasse`) REFERENCES `SiteTouristiqueClasses` (`IdSiteTouristiqueClasse`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `SiteTouristiqueReservations` (
    `IdSiteTouristiqueReservation` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `IdSiteTouristiqueJournee` int NOT NULL,
    `IdSite` int NULL,
    `ReferenceReservation` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `CustomerRef` varchar(100) CHARACTER SET utf8mb4 NULL,
    `IdUtilisateur` int NULL,
    `Status` enum('HOLD','CONFIRMED','CANCELLED','EXPIRED') CHARACTER SET utf8mb4 NOT NULL,
    `ExpiresAtUtc` datetime(6) NULL,
    `MontantSousTotal` decimal(18,2) NOT NULL DEFAULT 0.00,
    `CodeDevise` char(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF',
    `IdempotencyKey` varchar(120) CHARACTER SET utf8mb4 NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_SiteTouristiqueReservations` PRIMARY KEY (`IdSiteTouristiqueReservation`),
    CONSTRAINT `FK_SiteTouristiqueReservations_Societes`
        FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
    CONSTRAINT `FK_SiteTouristiqueReservations_Sites`
        FOREIGN KEY (`IdSite`) REFERENCES `Sites` (`IdSite`) ON DELETE RESTRICT,
    CONSTRAINT `FK_SiteTouristiqueReservations_Journees`
        FOREIGN KEY (`IdSiteTouristiqueJournee`) REFERENCES `SiteTouristiqueJournees` (`IdSiteTouristiqueJournee`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `SiteTouristiqueReservationLines` (
    `IdSiteTouristiqueReservationLine` int NOT NULL AUTO_INCREMENT,
    `IdSiteTouristiqueReservation` int NOT NULL,
    `LineType` enum('ClassQuota','GlobalQuota') CHARACTER SET utf8mb4 NOT NULL,
    `Quantite` int NOT NULL,
    `PrixUnitaire` decimal(18,2) NOT NULL,
    `CodeDevise` char(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF',
    `IdSiteTouristiqueClassQuota` int NULL,
    CONSTRAINT `PK_SiteTouristiqueReservationLines` PRIMARY KEY (`IdSiteTouristiqueReservationLine`),
    CONSTRAINT `CK_SiteTouristiqueReservationLines_Quantite` CHECK (`Quantite` > 0),
    CONSTRAINT `FK_SiteTouristiqueReservationLines_Reservations`
        FOREIGN KEY (`IdSiteTouristiqueReservation`) REFERENCES `SiteTouristiqueReservations` (`IdSiteTouristiqueReservation`) ON DELETE CASCADE,
    CONSTRAINT `FK_SiteTouristiqueReservationLines_ClassQuotas`
        FOREIGN KEY (`IdSiteTouristiqueClassQuota`) REFERENCES `SiteTouristiqueClassQuotas` (`IdSiteTouristiqueClassQuota`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `SiteTouristiqueTickets` (
    `IdSiteTouristiqueTicket` int NOT NULL AUTO_INCREMENT,
    `IdSiteTouristiqueReservationLine` int NOT NULL,
    `TicketCode` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Status` enum('ISSUED','USED','VOID') CHARACTER SET utf8mb4 NOT NULL DEFAULT 'ISSUED',
    `IssuedAtUtc` datetime(6) NOT NULL,
    `UsedAtUtc` datetime(6) NULL,
    CONSTRAINT `PK_SiteTouristiqueTickets` PRIMARY KEY (`IdSiteTouristiqueTicket`),
    CONSTRAINT `FK_SiteTouristiqueTickets_Lines`
        FOREIGN KEY (`IdSiteTouristiqueReservationLine`) REFERENCES `SiteTouristiqueReservationLines` (`IdSiteTouristiqueReservationLine`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `SiteTouristiquePayments` (
    `IdSiteTouristiquePayment` int NOT NULL AUTO_INCREMENT,
    `IdSiteTouristiqueReservation` int NOT NULL,
    `IdSite` int NULL,
    `ReferencePaiement` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Provider` varchar(40) CHARACTER SET utf8mb4 NOT NULL,
    `ProviderTxRef` varchar(120) CHARACTER SET utf8mb4 NULL,
    `Status` enum('PENDING','SUCCEEDED','FAILED','REFUNDED') CHARACTER SET utf8mb4 NOT NULL,
    `Montant` decimal(18,2) NOT NULL,
    `CodeDevise` char(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF',
    `MontantTarif` decimal(18,2) NOT NULL,
    `CodeDeviseTarif` char(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF',
    `TauxVersDevisePaiement` decimal(18,8) NOT NULL DEFAULT 1.00000000,
    `IdempotencyKey` varchar(120) CHARACTER SET utf8mb4 NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_SiteTouristiquePayments` PRIMARY KEY (`IdSiteTouristiquePayment`),
    CONSTRAINT `FK_SiteTouristiquePayments_Reservations`
        FOREIGN KEY (`IdSiteTouristiqueReservation`) REFERENCES `SiteTouristiqueReservations` (`IdSiteTouristiqueReservation`) ON DELETE RESTRICT,
    CONSTRAINT `FK_SiteTouristiquePayments_Sites`
        FOREIGN KEY (`IdSite`) REFERENCES `Sites` (`IdSite`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

-- Indexes
CREATE UNIQUE INDEX `IX_SiteTouristiqueClasses_Societe_Code_UQ`
    ON `SiteTouristiqueClasses` (`IdSociete`, `Code`);
CREATE INDEX `IX_SiteTouristiqueClasses_IdSociete` ON `SiteTouristiqueClasses` (`IdSociete`);

CREATE UNIQUE INDEX `IX_SiteTouristiques_Societe_CodeLieu_UQ`
    ON `SiteTouristiques` (`IdSociete`, `CodeLieu`);
CREATE INDEX `IX_SiteTouristiques_IdSite` ON `SiteTouristiques` (`IdSite`);

CREATE UNIQUE INDEX `IX_SiteTouristiqueJournees_Lieu_DateVisite_UQ`
    ON `SiteTouristiqueJournees` (`IdSiteTouristique`, `DateVisite`);
CREATE INDEX `IX_SiteTouristiqueJournees_IdSociete_DateVisite`
    ON `SiteTouristiqueJournees` (`IdSociete`, `DateVisite`);

CREATE UNIQUE INDEX `IX_SiteTouristiqueClassQuotas_Journee_Classe_UQ`
    ON `SiteTouristiqueClassQuotas` (`IdSiteTouristiqueJournee`, `IdSiteTouristiqueClasse`);

CREATE UNIQUE INDEX `IX_SiteTouristiqueReservations_Societe_Reference_UQ`
    ON `SiteTouristiqueReservations` (`IdSociete`, `ReferenceReservation`);
CREATE UNIQUE INDEX `IX_SiteTouristiqueReservations_Societe_Idempotency_UQ`
    ON `SiteTouristiqueReservations` (`IdSociete`, `IdempotencyKey`);
CREATE INDEX `IX_SiteTouristiqueReservations_Status_ExpiresAtUtc`
    ON `SiteTouristiqueReservations` (`Status`, `ExpiresAtUtc`);
CREATE INDEX `IX_SiteTouristiqueReservations_Journee_Status`
    ON `SiteTouristiqueReservations` (`IdSiteTouristiqueJournee`, `Status`);

CREATE INDEX `IX_SiteTouristiqueReservationLines_IdReservation`
    ON `SiteTouristiqueReservationLines` (`IdSiteTouristiqueReservation`);

CREATE UNIQUE INDEX `IX_SiteTouristiqueTickets_TicketCode_UQ`
    ON `SiteTouristiqueTickets` (`TicketCode`);

CREATE UNIQUE INDEX `IX_SiteTouristiquePayments_ReferencePaiement_UQ`
    ON `SiteTouristiquePayments` (`ReferencePaiement`);
CREATE UNIQUE INDEX `IX_SiteTouristiquePayments_Idempotency_UQ`
    ON `SiteTouristiquePayments` (`IdempotencyKey`);

SET FOREIGN_KEY_CHECKS = 1;
