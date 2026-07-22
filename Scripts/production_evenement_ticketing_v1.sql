-- =============================================================================
-- Migration production : Evenement Ticketing V1 (3 modes d'inventaire)
-- OBSOLETE — utiliser generated_evenement_migrations.sql (idempotent EF)
-- ou deploy_evenement_ticketing_production.sh
-- Ce fichier manque : ConfigSocietes.DureeHoldEvenementMinutes,
-- EvenementSessionGlobalQuotas.PrixUnitaire/CodeDevise, stamp __EFMigrationsHistory
-- =============================================================================

START TRANSACTION;

CREATE TABLE `EvenementSessions` (
    `IdEvenementSession` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `CodeSession` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `Libelle` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `StartAtUtc` datetime(6) NOT NULL,
    `EndAtUtc` datetime(6) NULL,
    `InventoryMode` enum('SeatNumbered','ClassQuota','GlobalQuota') CHARACTER SET utf8mb4 NOT NULL,
    `Status` enum('Draft','Published','Closed','Cancelled') CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Draft',
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_EvenementSessions` PRIMARY KEY (`IdEvenementSession`),
    CONSTRAINT `FK_EvenementSessions_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
    CONSTRAINT `CK_EvenementSessions_StartEnd` CHECK (`EndAtUtc` IS NULL OR `EndAtUtc` >= `StartAtUtc`)
) CHARACTER SET=utf8mb4;

CREATE UNIQUE INDEX `IX_EvenementSessions_Societe_CodeSession_UQ`
    ON `EvenementSessions` (`IdSociete`, `CodeSession`);
CREATE INDEX `IX_EvenementSessions_IdSociete_StartAtUtc`
    ON `EvenementSessions` (`IdSociete`, `StartAtUtc`);

CREATE TABLE `EvenementClasses` (
    `IdEvenementClasse` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `CodeClasse` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Libelle` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
    `Description` varchar(500) CHARACTER SET utf8mb4 NULL,
    `Statut` tinyint(1) NOT NULL DEFAULT TRUE,
    `DateCreation` datetime(6) NOT NULL,
    CONSTRAINT `PK_EvenementClasses` PRIMARY KEY (`IdEvenementClasse`),
    CONSTRAINT `FK_EvenementClasses_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE UNIQUE INDEX `IX_EvenementClasses_Societe_CodeClasse_UQ`
    ON `EvenementClasses` (`IdSociete`, `CodeClasse`);
CREATE INDEX `IX_EvenementClasses_IdSociete`
    ON `EvenementClasses` (`IdSociete`);

CREATE TABLE `EvenementSessionSections` (
    `IdEvenementSessionSection` int NOT NULL AUTO_INCREMENT,
    `IdEvenementSession` int NOT NULL,
    `CodeSection` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Libelle` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_EvenementSessionSections` PRIMARY KEY (`IdEvenementSessionSection`),
    CONSTRAINT `FK_EvenementSessionSections_EvenementSessions_IdEvenementSession`
        FOREIGN KEY (`IdEvenementSession`) REFERENCES `EvenementSessions` (`IdEvenementSession`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE UNIQUE INDEX `IX_EvenementSessionSections_Session_CodeSection_UQ`
    ON `EvenementSessionSections` (`IdEvenementSession`, `CodeSection`);
CREATE INDEX `IX_EvenementSessionSections_IdEvenementSession`
    ON `EvenementSessionSections` (`IdEvenementSession`);

CREATE TABLE `EvenementSessionGlobalQuotas` (
    `IdEvenementSession` int NOT NULL,
    `CapaciteTotale` int NOT NULL,
    `QuantiteHold` int NOT NULL DEFAULT 0,
    `QuantiteVendue` int NOT NULL DEFAULT 0,
    CONSTRAINT `PK_EvenementSessionGlobalQuotas` PRIMARY KEY (`IdEvenementSession`),
    CONSTRAINT `FK_EvenementSessionGlobalQuotas_EvenementSessions_IdEvenementSession`
        FOREIGN KEY (`IdEvenementSession`) REFERENCES `EvenementSessions` (`IdEvenementSession`) ON DELETE CASCADE,
    CONSTRAINT `CK_EvenementSessionGlobalQuotas_Capacite` CHECK (`CapaciteTotale` >= 0),
    CONSTRAINT `CK_EvenementSessionGlobalQuotas_StockPositive` CHECK (`QuantiteHold` >= 0 AND `QuantiteVendue` >= 0),
    CONSTRAINT `CK_EvenementSessionGlobalQuotas_StockMax` CHECK (`QuantiteHold` + `QuantiteVendue` <= `CapaciteTotale`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `EvenementSessionClassQuotas` (
    `IdEvenementSessionClassQuota` int NOT NULL AUTO_INCREMENT,
    `IdEvenementSession` int NOT NULL,
    `IdEvenementClasse` int NOT NULL,
    `CapaciteTotale` int NOT NULL,
    `QuantiteHold` int NOT NULL DEFAULT 0,
    `QuantiteVendue` int NOT NULL DEFAULT 0,
    `PrixUnitaire` decimal(18,2) NOT NULL,
    `CodeDevise` char(3) CHARACTER SET ascii NOT NULL DEFAULT 'CDF',
    CONSTRAINT `PK_EvenementSessionClassQuotas` PRIMARY KEY (`IdEvenementSessionClassQuota`),
    CONSTRAINT `FK_EvenementSessionClassQuotas_EvenementSessions_IdEvenementSession`
        FOREIGN KEY (`IdEvenementSession`) REFERENCES `EvenementSessions` (`IdEvenementSession`) ON DELETE CASCADE,
    CONSTRAINT `FK_EvenementSessionClassQuotas_EvenementClasses_IdEvenementClasse`
        FOREIGN KEY (`IdEvenementClasse`) REFERENCES `EvenementClasses` (`IdEvenementClasse`) ON DELETE RESTRICT,
    CONSTRAINT `CK_EvenementSessionClassQuotas_Capacite` CHECK (`CapaciteTotale` >= 0),
    CONSTRAINT `CK_EvenementSessionClassQuotas_StockPositive` CHECK (`QuantiteHold` >= 0 AND `QuantiteVendue` >= 0),
    CONSTRAINT `CK_EvenementSessionClassQuotas_StockMax` CHECK (`QuantiteHold` + `QuantiteVendue` <= `CapaciteTotale`)
) CHARACTER SET=utf8mb4;

CREATE UNIQUE INDEX `IX_EvenementSessionClassQuotas_Session_Classe_UQ`
    ON `EvenementSessionClassQuotas` (`IdEvenementSession`, `IdEvenementClasse`);
CREATE INDEX `IX_EvenementSessionClassQuotas_IdEvenementSession`
    ON `EvenementSessionClassQuotas` (`IdEvenementSession`);

CREATE TABLE `EvenementReservations` (
    `IdEvenementReservation` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `IdEvenementSession` int NOT NULL,
    `ReferenceReservation` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `CustomerRef` varchar(100) CHARACTER SET utf8mb4 NULL,
    `Status` enum('HOLD','CONFIRMED','CANCELLED','EXPIRED') CHARACTER SET utf8mb4 NOT NULL,
    `ExpiresAtUtc` datetime(6) NULL,
    `MontantSousTotal` decimal(18,2) NOT NULL DEFAULT 0,
    `CodeDevise` char(3) CHARACTER SET ascii NOT NULL DEFAULT 'CDF',
    `IdempotencyKey` varchar(120) CHARACTER SET utf8mb4 NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_EvenementReservations` PRIMARY KEY (`IdEvenementReservation`),
    CONSTRAINT `FK_EvenementReservations_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
    CONSTRAINT `FK_EvenementReservations_EvenementSessions_IdEvenementSession`
        FOREIGN KEY (`IdEvenementSession`) REFERENCES `EvenementSessions` (`IdEvenementSession`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE UNIQUE INDEX `IX_EvenementReservations_Societe_Reference_UQ`
    ON `EvenementReservations` (`IdSociete`, `ReferenceReservation`);
CREATE UNIQUE INDEX `IX_EvenementReservations_Societe_Idempotency_UQ`
    ON `EvenementReservations` (`IdSociete`, `IdempotencyKey`);
CREATE INDEX `IX_EvenementReservations_Status_ExpiresAtUtc`
    ON `EvenementReservations` (`Status`, `ExpiresAtUtc`);
CREATE INDEX `IX_EvenementReservations_Session_Status`
    ON `EvenementReservations` (`IdEvenementSession`, `Status`);

CREATE TABLE `EvenementSessionSeats` (
    `IdEvenementSessionSeat` int NOT NULL AUTO_INCREMENT,
    `IdEvenementSession` int NOT NULL,
    `SeatCode` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `IdEvenementSessionSection` int NULL,
    `IdEvenementClasse` int NULL,
    `SeatStatus` enum('Available','Held','Sold','Blocked') CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Available',
    `IdEvenementReservationCourante` int NULL,
    `HoldExpireAtUtc` datetime(6) NULL,
    `PrixUnitaire` decimal(18,2) NOT NULL,
    `CodeDevise` char(3) CHARACTER SET ascii NOT NULL DEFAULT 'CDF',
    CONSTRAINT `PK_EvenementSessionSeats` PRIMARY KEY (`IdEvenementSessionSeat`),
    CONSTRAINT `FK_EvenementSessionSeats_EvenementSessions_IdEvenementSession`
        FOREIGN KEY (`IdEvenementSession`) REFERENCES `EvenementSessions` (`IdEvenementSession`) ON DELETE CASCADE,
    CONSTRAINT `FK_EvenementSessionSeats_EvenementSessionSections_IdEvenementSessionSection`
        FOREIGN KEY (`IdEvenementSessionSection`) REFERENCES `EvenementSessionSections` (`IdEvenementSessionSection`) ON DELETE SET NULL,
    CONSTRAINT `FK_EvenementSessionSeats_EvenementClasses_IdEvenementClasse`
        FOREIGN KEY (`IdEvenementClasse`) REFERENCES `EvenementClasses` (`IdEvenementClasse`) ON DELETE SET NULL,
    CONSTRAINT `FK_EvenementSessionSeats_EvenementReservations_IdEvenementReservationCourante`
        FOREIGN KEY (`IdEvenementReservationCourante`) REFERENCES `EvenementReservations` (`IdEvenementReservation`) ON DELETE SET NULL
) CHARACTER SET=utf8mb4;

CREATE UNIQUE INDEX `IX_EvenementSessionSeats_Session_SeatCode_UQ`
    ON `EvenementSessionSeats` (`IdEvenementSession`, `SeatCode`);
CREATE INDEX `IX_EvenementSessionSeats_Session_SeatStatus`
    ON `EvenementSessionSeats` (`IdEvenementSession`, `SeatStatus`);
CREATE INDEX `IX_EvenementSessionSeats_HoldExpireAtUtc`
    ON `EvenementSessionSeats` (`HoldExpireAtUtc`);

CREATE TABLE `EvenementReservationLines` (
    `IdEvenementReservationLine` int NOT NULL AUTO_INCREMENT,
    `IdEvenementReservation` int NOT NULL,
    `LineType` enum('Seat','ClassQuota','GlobalQuota') CHARACTER SET utf8mb4 NOT NULL,
    `Quantite` int NOT NULL,
    `PrixUnitaire` decimal(18,2) NOT NULL,
    `CodeDevise` char(3) CHARACTER SET ascii NOT NULL DEFAULT 'CDF',
    `IdEvenementSessionSeat` int NULL,
    `IdEvenementSessionClassQuota` int NULL,
    CONSTRAINT `PK_EvenementReservationLines` PRIMARY KEY (`IdEvenementReservationLine`),
    CONSTRAINT `FK_EvenementReservationLines_EvenementReservations_IdEvenementReservation`
        FOREIGN KEY (`IdEvenementReservation`) REFERENCES `EvenementReservations` (`IdEvenementReservation`) ON DELETE CASCADE,
    CONSTRAINT `FK_EvenementReservationLines_EvenementSessionSeats_IdEvenementSessionSeat`
        FOREIGN KEY (`IdEvenementSessionSeat`) REFERENCES `EvenementSessionSeats` (`IdEvenementSessionSeat`) ON DELETE RESTRICT,
    CONSTRAINT `FK_EvenementReservationLines_EvenementSessionClassQuotas_IdEvenementSessionClassQuota`
        FOREIGN KEY (`IdEvenementSessionClassQuota`) REFERENCES `EvenementSessionClassQuotas` (`IdEvenementSessionClassQuota`) ON DELETE RESTRICT,
    CONSTRAINT `CK_EvenementReservationLines_Quantite` CHECK (`Quantite` > 0)
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_EvenementReservationLines_IdEvenementReservation`
    ON `EvenementReservationLines` (`IdEvenementReservation`);
CREATE UNIQUE INDEX `IX_EvenementReservationLines_Reservation_Seat_UQ`
    ON `EvenementReservationLines` (`IdEvenementReservation`, `IdEvenementSessionSeat`);

CREATE TABLE `EvenementTickets` (
    `IdEvenementTicket` int NOT NULL AUTO_INCREMENT,
    `IdEvenementReservationLine` int NOT NULL,
    `TicketCode` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Status` enum('ISSUED','USED','VOID') CHARACTER SET utf8mb4 NOT NULL DEFAULT 'ISSUED',
    `IssuedAtUtc` datetime(6) NOT NULL,
    `UsedAtUtc` datetime(6) NULL,
    CONSTRAINT `PK_EvenementTickets` PRIMARY KEY (`IdEvenementTicket`),
    CONSTRAINT `FK_EvenementTickets_EvenementReservationLines_IdEvenementReservationLine`
        FOREIGN KEY (`IdEvenementReservationLine`) REFERENCES `EvenementReservationLines` (`IdEvenementReservationLine`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE UNIQUE INDEX `IX_EvenementTickets_TicketCode_UQ`
    ON `EvenementTickets` (`TicketCode`);
CREATE INDEX `IX_EvenementTickets_Status`
    ON `EvenementTickets` (`Status`);

CREATE TABLE `EvenementPayments` (
    `IdEvenementPayment` int NOT NULL AUTO_INCREMENT,
    `IdEvenementReservation` int NOT NULL,
    `ReferencePaiement` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Provider` varchar(40) CHARACTER SET utf8mb4 NOT NULL,
    `ProviderTxRef` varchar(120) CHARACTER SET utf8mb4 NULL,
    `Status` enum('PENDING','SUCCEEDED','FAILED','REFUNDED') CHARACTER SET utf8mb4 NOT NULL,
    `Montant` decimal(18,2) NOT NULL,
    `CodeDevise` char(3) CHARACTER SET ascii NOT NULL DEFAULT 'CDF',
    `IdempotencyKey` varchar(120) CHARACTER SET utf8mb4 NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_EvenementPayments` PRIMARY KEY (`IdEvenementPayment`),
    CONSTRAINT `FK_EvenementPayments_EvenementReservations_IdEvenementReservation`
        FOREIGN KEY (`IdEvenementReservation`) REFERENCES `EvenementReservations` (`IdEvenementReservation`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE UNIQUE INDEX `IX_EvenementPayments_ReferencePaiement_UQ`
    ON `EvenementPayments` (`ReferencePaiement`);
CREATE UNIQUE INDEX `IX_EvenementPayments_Idempotency_UQ`
    ON `EvenementPayments` (`IdempotencyKey`);
CREATE INDEX `IX_EvenementPayments_Reservation_Status`
    ON `EvenementPayments` (`IdEvenementReservation`, `Status`);

-- -----------------------------------------------------------------------------
-- Triggers de coherence EvenementReservationLines (renforce les CHECK par type)
-- -----------------------------------------------------------------------------
DROP TRIGGER IF EXISTS `TRG_EvenementReservationLines_BI`;
CREATE TRIGGER `TRG_EvenementReservationLines_BI`
BEFORE INSERT ON `EvenementReservationLines`
FOR EACH ROW
BEGIN
    IF NEW.`LineType` = 'Seat' THEN
        IF NEW.`Quantite` <> 1 OR NEW.`IdEvenementSessionSeat` IS NULL OR NEW.`IdEvenementSessionClassQuota` IS NOT NULL THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'LineType Seat invalide: Quantite=1, Seat obligatoire, ClassQuota null.';
        END IF;
    ELSEIF NEW.`LineType` = 'ClassQuota' THEN
        IF NEW.`IdEvenementSessionClassQuota` IS NULL OR NEW.`IdEvenementSessionSeat` IS NOT NULL THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'LineType ClassQuota invalide: ClassQuota obligatoire, Seat null.';
        END IF;
    ELSEIF NEW.`LineType` = 'GlobalQuota' THEN
        IF NEW.`IdEvenementSessionSeat` IS NOT NULL OR NEW.`IdEvenementSessionClassQuota` IS NOT NULL THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'LineType GlobalQuota invalide: Seat et ClassQuota doivent etre null.';
        END IF;
    END IF;
END;

DROP TRIGGER IF EXISTS `TRG_EvenementReservationLines_BU`;
CREATE TRIGGER `TRG_EvenementReservationLines_BU`
BEFORE UPDATE ON `EvenementReservationLines`
FOR EACH ROW
BEGIN
    IF NEW.`LineType` = 'Seat' THEN
        IF NEW.`Quantite` <> 1 OR NEW.`IdEvenementSessionSeat` IS NULL OR NEW.`IdEvenementSessionClassQuota` IS NOT NULL THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'LineType Seat invalide: Quantite=1, Seat obligatoire, ClassQuota null.';
        END IF;
    ELSEIF NEW.`LineType` = 'ClassQuota' THEN
        IF NEW.`IdEvenementSessionClassQuota` IS NULL OR NEW.`IdEvenementSessionSeat` IS NOT NULL THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'LineType ClassQuota invalide: ClassQuota obligatoire, Seat null.';
        END IF;
    ELSEIF NEW.`LineType` = 'GlobalQuota' THEN
        IF NEW.`IdEvenementSessionSeat` IS NOT NULL OR NEW.`IdEvenementSessionClassQuota` IS NOT NULL THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'LineType GlobalQuota invalide: Seat et ClassQuota doivent etre null.';
        END IF;
    END IF;
END;

COMMIT;

