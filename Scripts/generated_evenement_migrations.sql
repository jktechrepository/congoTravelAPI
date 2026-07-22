START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    ALTER TABLE `ConfigSocietes` ADD `DureeHoldEvenementMinutes` int NOT NULL DEFAULT 15;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

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

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

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
        CONSTRAINT `CK_EvenementSessions_StartEnd` CHECK (`EndAtUtc` IS NULL OR `EndAtUtc` >= `StartAtUtc`),
        CONSTRAINT `FK_EvenementSessions_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE TABLE `EvenementReservations` (
        `IdEvenementReservation` int NOT NULL AUTO_INCREMENT,
        `IdSociete` int NOT NULL,
        `IdEvenementSession` int NOT NULL,
        `ReferenceReservation` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
        `CustomerRef` varchar(100) CHARACTER SET utf8mb4 NULL,
        `Status` enum('HOLD','CONFIRMED','CANCELLED','EXPIRED') CHARACTER SET utf8mb4 NOT NULL,
        `ExpiresAtUtc` datetime(6) NULL,
        `MontantSousTotal` decimal(18,2) NOT NULL DEFAULT 0.0,
        `CodeDevise` char(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF',
        `IdempotencyKey` varchar(120) CHARACTER SET utf8mb4 NULL,
        `DateCreation` datetime(6) NOT NULL,
        `DateModification` datetime(6) NULL,
        CONSTRAINT `PK_EvenementReservations` PRIMARY KEY (`IdEvenementReservation`),
        CONSTRAINT `FK_EvenementReservations_EvenementSessions_IdEvenementSession` FOREIGN KEY (`IdEvenementSession`) REFERENCES `EvenementSessions` (`IdEvenementSession`) ON DELETE RESTRICT,
        CONSTRAINT `FK_EvenementReservations_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE TABLE `EvenementSessionClassQuotas` (
        `IdEvenementSessionClassQuota` int NOT NULL AUTO_INCREMENT,
        `IdEvenementSession` int NOT NULL,
        `IdEvenementClasse` int NOT NULL,
        `CapaciteTotale` int NOT NULL,
        `QuantiteHold` int NOT NULL DEFAULT 0,
        `QuantiteVendue` int NOT NULL DEFAULT 0,
        `PrixUnitaire` decimal(18,2) NOT NULL,
        `CodeDevise` char(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF',
        CONSTRAINT `PK_EvenementSessionClassQuotas` PRIMARY KEY (`IdEvenementSessionClassQuota`),
        CONSTRAINT `CK_EvenementSessionClassQuotas_Capacite` CHECK (`CapaciteTotale` >= 0),
        CONSTRAINT `CK_EvenementSessionClassQuotas_StockMax` CHECK (`QuantiteHold` + `QuantiteVendue` <= `CapaciteTotale`),
        CONSTRAINT `CK_EvenementSessionClassQuotas_StockPositive` CHECK (`QuantiteHold` >= 0 AND `QuantiteVendue` >= 0),
        CONSTRAINT `FK_EvenementSessionClassQuotas_EvenementClasses_IdEvenementClas~` FOREIGN KEY (`IdEvenementClasse`) REFERENCES `EvenementClasses` (`IdEvenementClasse`) ON DELETE RESTRICT,
        CONSTRAINT `FK_EvenementSessionClassQuotas_EvenementSessions_IdEvenementSes~` FOREIGN KEY (`IdEvenementSession`) REFERENCES `EvenementSessions` (`IdEvenementSession`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE TABLE `EvenementSessionGlobalQuotas` (
        `IdEvenementSession` int NOT NULL,
        `CapaciteTotale` int NOT NULL,
        `QuantiteHold` int NOT NULL DEFAULT 0,
        `QuantiteVendue` int NOT NULL DEFAULT 0,
        CONSTRAINT `PK_EvenementSessionGlobalQuotas` PRIMARY KEY (`IdEvenementSession`),
        CONSTRAINT `CK_EvenementSessionGlobalQuotas_Capacite` CHECK (`CapaciteTotale` >= 0),
        CONSTRAINT `CK_EvenementSessionGlobalQuotas_StockMax` CHECK (`QuantiteHold` + `QuantiteVendue` <= `CapaciteTotale`),
        CONSTRAINT `CK_EvenementSessionGlobalQuotas_StockPositive` CHECK (`QuantiteHold` >= 0 AND `QuantiteVendue` >= 0),
        CONSTRAINT `FK_EvenementSessionGlobalQuotas_EvenementSessions_IdEvenementSe~` FOREIGN KEY (`IdEvenementSession`) REFERENCES `EvenementSessions` (`IdEvenementSession`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE TABLE `EvenementSessionSections` (
        `IdEvenementSessionSection` int NOT NULL AUTO_INCREMENT,
        `IdEvenementSession` int NOT NULL,
        `CodeSection` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
        `Libelle` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
        CONSTRAINT `PK_EvenementSessionSections` PRIMARY KEY (`IdEvenementSessionSection`),
        CONSTRAINT `FK_EvenementSessionSections_EvenementSessions_IdEvenementSession` FOREIGN KEY (`IdEvenementSession`) REFERENCES `EvenementSessions` (`IdEvenementSession`) ON DELETE CASCADE
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE TABLE `EvenementPayments` (
        `IdEvenementPayment` int NOT NULL AUTO_INCREMENT,
        `IdEvenementReservation` int NOT NULL,
        `ReferencePaiement` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `Provider` varchar(40) CHARACTER SET utf8mb4 NOT NULL,
        `ProviderTxRef` varchar(120) CHARACTER SET utf8mb4 NULL,
        `Status` enum('PENDING','SUCCEEDED','FAILED','REFUNDED') CHARACTER SET utf8mb4 NOT NULL,
        `Montant` decimal(18,2) NOT NULL,
        `CodeDevise` char(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF',
        `IdempotencyKey` varchar(120) CHARACTER SET utf8mb4 NULL,
        `DateCreation` datetime(6) NOT NULL,
        `DateModification` datetime(6) NULL,
        CONSTRAINT `PK_EvenementPayments` PRIMARY KEY (`IdEvenementPayment`),
        CONSTRAINT `FK_EvenementPayments_EvenementReservations_IdEvenementReservati~` FOREIGN KEY (`IdEvenementReservation`) REFERENCES `EvenementReservations` (`IdEvenementReservation`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

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
        `CodeDevise` char(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF',
        CONSTRAINT `PK_EvenementSessionSeats` PRIMARY KEY (`IdEvenementSessionSeat`),
        CONSTRAINT `FK_EvenementSessionSeats_EvenementClasses_IdEvenementClasse` FOREIGN KEY (`IdEvenementClasse`) REFERENCES `EvenementClasses` (`IdEvenementClasse`) ON DELETE SET NULL,
        CONSTRAINT `FK_EvenementSessionSeats_EvenementReservations_IdEvenementReser~` FOREIGN KEY (`IdEvenementReservationCourante`) REFERENCES `EvenementReservations` (`IdEvenementReservation`) ON DELETE SET NULL,
        CONSTRAINT `FK_EvenementSessionSeats_EvenementSessions_IdEvenementSession` FOREIGN KEY (`IdEvenementSession`) REFERENCES `EvenementSessions` (`IdEvenementSession`) ON DELETE CASCADE,
        CONSTRAINT `FK_EvenementSessionSeats_EvenementSessionSections_IdEvenementSe~` FOREIGN KEY (`IdEvenementSessionSection`) REFERENCES `EvenementSessionSections` (`IdEvenementSessionSection`) ON DELETE SET NULL
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE TABLE `EvenementReservationLines` (
        `IdEvenementReservationLine` int NOT NULL AUTO_INCREMENT,
        `IdEvenementReservation` int NOT NULL,
        `LineType` enum('Seat','ClassQuota','GlobalQuota') CHARACTER SET utf8mb4 NOT NULL,
        `Quantite` int NOT NULL,
        `PrixUnitaire` decimal(18,2) NOT NULL,
        `CodeDevise` char(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF',
        `IdEvenementSessionSeat` int NULL,
        `IdEvenementSessionClassQuota` int NULL,
        CONSTRAINT `PK_EvenementReservationLines` PRIMARY KEY (`IdEvenementReservationLine`),
        CONSTRAINT `CK_EvenementReservationLines_Quantite` CHECK (`Quantite` > 0),
        CONSTRAINT `FK_EvenementReservationLines_EvenementReservations_IdEvenementR~` FOREIGN KEY (`IdEvenementReservation`) REFERENCES `EvenementReservations` (`IdEvenementReservation`) ON DELETE CASCADE,
        CONSTRAINT `FK_EvenementReservationLines_EvenementSessionClassQuotas_IdEven~` FOREIGN KEY (`IdEvenementSessionClassQuota`) REFERENCES `EvenementSessionClassQuotas` (`IdEvenementSessionClassQuota`) ON DELETE RESTRICT,
        CONSTRAINT `FK_EvenementReservationLines_EvenementSessionSeats_IdEvenementS~` FOREIGN KEY (`IdEvenementSessionSeat`) REFERENCES `EvenementSessionSeats` (`IdEvenementSessionSeat`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE TABLE `EvenementTickets` (
        `IdEvenementTicket` int NOT NULL AUTO_INCREMENT,
        `IdEvenementReservationLine` int NOT NULL,
        `TicketCode` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `Status` enum('ISSUED','USED','VOID') CHARACTER SET utf8mb4 NOT NULL DEFAULT 'ISSUED',
        `IssuedAtUtc` datetime(6) NOT NULL,
        `UsedAtUtc` datetime(6) NULL,
        CONSTRAINT `PK_EvenementTickets` PRIMARY KEY (`IdEvenementTicket`),
        CONSTRAINT `FK_EvenementTickets_EvenementReservationLines_IdEvenementReserv~` FOREIGN KEY (`IdEvenementReservationLine`) REFERENCES `EvenementReservationLines` (`IdEvenementReservationLine`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE INDEX `IX_EvenementClasses_IdSociete` ON `EvenementClasses` (`IdSociete`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE UNIQUE INDEX `IX_EvenementClasses_Societe_CodeClasse_UQ` ON `EvenementClasses` (`IdSociete`, `CodeClasse`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE UNIQUE INDEX `IX_EvenementPayments_Idempotency_UQ` ON `EvenementPayments` (`IdempotencyKey`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE UNIQUE INDEX `IX_EvenementPayments_ReferencePaiement_UQ` ON `EvenementPayments` (`ReferencePaiement`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE INDEX `IX_EvenementPayments_Reservation_Status` ON `EvenementPayments` (`IdEvenementReservation`, `Status`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE INDEX `IX_EvenementReservationLines_IdEvenementReservation` ON `EvenementReservationLines` (`IdEvenementReservation`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE INDEX `IX_EvenementReservationLines_IdEvenementSessionClassQuota` ON `EvenementReservationLines` (`IdEvenementSessionClassQuota`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE INDEX `IX_EvenementReservationLines_IdEvenementSessionSeat` ON `EvenementReservationLines` (`IdEvenementSessionSeat`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE UNIQUE INDEX `IX_EvenementReservationLines_Reservation_Seat_UQ` ON `EvenementReservationLines` (`IdEvenementReservation`, `IdEvenementSessionSeat`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE INDEX `IX_EvenementReservations_Session_Status` ON `EvenementReservations` (`IdEvenementSession`, `Status`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE UNIQUE INDEX `IX_EvenementReservations_Societe_Idempotency_UQ` ON `EvenementReservations` (`IdSociete`, `IdempotencyKey`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE UNIQUE INDEX `IX_EvenementReservations_Societe_Reference_UQ` ON `EvenementReservations` (`IdSociete`, `ReferenceReservation`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE INDEX `IX_EvenementReservations_Status_ExpiresAtUtc` ON `EvenementReservations` (`Status`, `ExpiresAtUtc`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE INDEX `IX_EvenementSessionClassQuotas_IdEvenementClasse` ON `EvenementSessionClassQuotas` (`IdEvenementClasse`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE INDEX `IX_EvenementSessionClassQuotas_IdEvenementSession` ON `EvenementSessionClassQuotas` (`IdEvenementSession`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE UNIQUE INDEX `IX_EvenementSessionClassQuotas_Session_Classe_UQ` ON `EvenementSessionClassQuotas` (`IdEvenementSession`, `IdEvenementClasse`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE INDEX `IX_EvenementSessions_IdSociete_StartAtUtc` ON `EvenementSessions` (`IdSociete`, `StartAtUtc`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE UNIQUE INDEX `IX_EvenementSessions_Societe_CodeSession_UQ` ON `EvenementSessions` (`IdSociete`, `CodeSession`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE INDEX `IX_EvenementSessionSeats_HoldExpireAtUtc` ON `EvenementSessionSeats` (`HoldExpireAtUtc`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE INDEX `IX_EvenementSessionSeats_IdEvenementClasse` ON `EvenementSessionSeats` (`IdEvenementClasse`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE INDEX `IX_EvenementSessionSeats_IdEvenementReservationCourante` ON `EvenementSessionSeats` (`IdEvenementReservationCourante`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE INDEX `IX_EvenementSessionSeats_IdEvenementSessionSection` ON `EvenementSessionSeats` (`IdEvenementSessionSection`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE UNIQUE INDEX `IX_EvenementSessionSeats_Session_SeatCode_UQ` ON `EvenementSessionSeats` (`IdEvenementSession`, `SeatCode`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE INDEX `IX_EvenementSessionSeats_Session_SeatStatus` ON `EvenementSessionSeats` (`IdEvenementSession`, `SeatStatus`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE INDEX `IX_EvenementSessionSections_IdEvenementSession` ON `EvenementSessionSections` (`IdEvenementSession`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE UNIQUE INDEX `IX_EvenementSessionSections_Session_CodeSection_UQ` ON `EvenementSessionSections` (`IdEvenementSession`, `CodeSection`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE INDEX `IX_EvenementTickets_IdEvenementReservationLine` ON `EvenementTickets` (`IdEvenementReservationLine`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE INDEX `IX_EvenementTickets_Status` ON `EvenementTickets` (`Status`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    CREATE UNIQUE INDEX `IX_EvenementTickets_TicketCode_UQ` ON `EvenementTickets` (`TicketCode`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703101713_EvenementTicketingV1') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260703101713_EvenementTicketingV1', '6.0.25');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703120104_EvenementSessionGlobalQuotaPricing') THEN

    ALTER TABLE `EvenementSessionGlobalQuotas` ADD `CodeDevise` char(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF';

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703120104_EvenementSessionGlobalQuotaPricing') THEN

    ALTER TABLE `EvenementSessionGlobalQuotas` ADD `PrixUnitaire` decimal(18,2) NOT NULL DEFAULT 0.0;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260703120104_EvenementSessionGlobalQuotaPricing') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260703120104_EvenementSessionGlobalQuotaPricing', '6.0.25');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

