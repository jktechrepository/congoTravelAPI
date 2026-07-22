-- Phase E — Addon workflow réservation V2 (EF migrations Phase A + Phase B).
-- Généré par : dotnet ef migrations script 20260501083042_ChangeBusAliasBusToString 20260502083420_PhaseB_BilletPassengerSiege -i
-- Prérequis : schéma aligné avec les migrations EF jusqu'à ChangeBusAliasBusToString (inclus).
-- Sinon : depuis le dépôt, préférer `dotnet ef database update`.
START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260501134759_PhaseA_SiegeVoyageDestPassagerAllocation') THEN

    CREATE TABLE `ReservationPassengers` (
        `IdReservationPassenger` int NOT NULL AUTO_INCREMENT,
        `IdReservation` int NOT NULL,
        `IdClient` int NULL,
        `NomComplet` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
        `Telephone` varchar(20) CHARACTER SET utf8mb4 NULL,
        `Email` varchar(256) CHARACTER SET utf8mb4 NULL,
        `DocumentType` varchar(50) CHARACTER SET utf8mb4 NULL,
        `DocumentNumero` varchar(100) CHARACTER SET utf8mb4 NULL,
        `DateNaissance` date NULL,
        `Genre` varchar(10) CHARACTER SET utf8mb4 NULL,
        `IdSociete` int NOT NULL,
        `Statut` tinyint(1) NOT NULL,
        `DateCreation` datetime(6) NOT NULL,
        `DateModification` datetime(6) NULL,
        CONSTRAINT `PK_ReservationPassengers` PRIMARY KEY (`IdReservationPassenger`),
        CONSTRAINT `FK_ReservationPassengers_Clients_IdClient` FOREIGN KEY (`IdClient`) REFERENCES `Clients` (`IdClient`) ON DELETE RESTRICT,
        CONSTRAINT `FK_ReservationPassengers_Reservations_IdReservation` FOREIGN KEY (`IdReservation`) REFERENCES `Reservations` (`IdReservation`) ON DELETE CASCADE,
        CONSTRAINT `FK_ReservationPassengers_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT
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
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260501134759_PhaseA_SiegeVoyageDestPassagerAllocation') THEN

    CREATE TABLE `Sieges` (
        `IdSiege` int NOT NULL AUTO_INCREMENT,
        `IdBus` int NOT NULL,
        `NumeroOrdre` int NOT NULL,
        `CodeSiege` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
        `EstActif` tinyint(1) NOT NULL,
        `IdSociete` int NOT NULL,
        `DateCreation` datetime(6) NOT NULL,
        `DateModification` datetime(6) NULL,
        CONSTRAINT `PK_Sieges` PRIMARY KEY (`IdSiege`),
        CONSTRAINT `FK_Sieges_Buses_IdBus` FOREIGN KEY (`IdBus`) REFERENCES `Buses` (`IdBus`) ON DELETE RESTRICT,
        CONSTRAINT `FK_Sieges_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT
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
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260501134759_PhaseA_SiegeVoyageDestPassagerAllocation') THEN

    CREATE TABLE `VoyageDestinations` (
        `IdVoyageDestination` int NOT NULL AUTO_INCREMENT,
        `IdVoyage` int NOT NULL,
        `IdDestination` int NOT NULL,
        `Ordre` int NOT NULL,
        `IdSociete` int NOT NULL,
        `DateCreation` datetime(6) NOT NULL,
        `DateModification` datetime(6) NULL,
        CONSTRAINT `PK_VoyageDestinations` PRIMARY KEY (`IdVoyageDestination`),
        CONSTRAINT `FK_VoyageDestinations_Destinations_IdDestination` FOREIGN KEY (`IdDestination`) REFERENCES `Destinations` (`IdDestination`) ON DELETE RESTRICT,
        CONSTRAINT `FK_VoyageDestinations_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
        CONSTRAINT `FK_VoyageDestinations_Voyages_IdVoyage` FOREIGN KEY (`IdVoyage`) REFERENCES `Voyages` (`Id`) ON DELETE CASCADE
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
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260501134759_PhaseA_SiegeVoyageDestPassagerAllocation') THEN

    CREATE TABLE `VoyageSeatAllocations` (
        `IdVoyageSeatAllocation` int NOT NULL AUTO_INCREMENT,
        `IdVoyage` int NOT NULL,
        `IdSiege` int NOT NULL,
        `IdReservationPassenger` int NOT NULL,
        `Statut` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
        `DateCreation` datetime(6) NOT NULL,
        `DateModification` datetime(6) NULL,
        CONSTRAINT `PK_VoyageSeatAllocations` PRIMARY KEY (`IdVoyageSeatAllocation`),
        CONSTRAINT `FK_VoyageSeatAllocations_ReservationPassengers_IdReservationPas~` FOREIGN KEY (`IdReservationPassenger`) REFERENCES `ReservationPassengers` (`IdReservationPassenger`) ON DELETE CASCADE,
        CONSTRAINT `FK_VoyageSeatAllocations_Sieges_IdSiege` FOREIGN KEY (`IdSiege`) REFERENCES `Sieges` (`IdSiege`) ON DELETE RESTRICT,
        CONSTRAINT `FK_VoyageSeatAllocations_Voyages_IdVoyage` FOREIGN KEY (`IdVoyage`) REFERENCES `Voyages` (`Id`) ON DELETE RESTRICT
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
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260501134759_PhaseA_SiegeVoyageDestPassagerAllocation') THEN

    CREATE INDEX `IX_ReservationPassengers_IdClient` ON `ReservationPassengers` (`IdClient`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260501134759_PhaseA_SiegeVoyageDestPassagerAllocation') THEN

    CREATE INDEX `IX_ReservationPassengers_IdReservation` ON `ReservationPassengers` (`IdReservation`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260501134759_PhaseA_SiegeVoyageDestPassagerAllocation') THEN

    CREATE INDEX `IX_ReservationPassengers_IdSociete` ON `ReservationPassengers` (`IdSociete`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260501134759_PhaseA_SiegeVoyageDestPassagerAllocation') THEN

    CREATE UNIQUE INDEX `IX_Sieges_Bus_CodeSiege_Unique` ON `Sieges` (`IdBus`, `CodeSiege`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260501134759_PhaseA_SiegeVoyageDestPassagerAllocation') THEN

    CREATE UNIQUE INDEX `IX_Sieges_Bus_NumeroOrdre_Unique` ON `Sieges` (`IdBus`, `NumeroOrdre`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260501134759_PhaseA_SiegeVoyageDestPassagerAllocation') THEN

    CREATE INDEX `IX_Sieges_IdSociete` ON `Sieges` (`IdSociete`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260501134759_PhaseA_SiegeVoyageDestPassagerAllocation') THEN

    CREATE INDEX `IX_VoyageDestinations_IdDestination` ON `VoyageDestinations` (`IdDestination`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260501134759_PhaseA_SiegeVoyageDestPassagerAllocation') THEN

    CREATE INDEX `IX_VoyageDestinations_IdSociete` ON `VoyageDestinations` (`IdSociete`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260501134759_PhaseA_SiegeVoyageDestPassagerAllocation') THEN

    CREATE UNIQUE INDEX `IX_VoyageDestinations_Voyage_Ordre_Unique` ON `VoyageDestinations` (`IdVoyage`, `Ordre`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260501134759_PhaseA_SiegeVoyageDestPassagerAllocation') THEN

    CREATE INDEX `IX_VoyageSeatAllocations_IdSiege` ON `VoyageSeatAllocations` (`IdSiege`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260501134759_PhaseA_SiegeVoyageDestPassagerAllocation') THEN

    CREATE INDEX `IX_VoyageSeatAllocations_IdVoyage` ON `VoyageSeatAllocations` (`IdVoyage`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260501134759_PhaseA_SiegeVoyageDestPassagerAllocation') THEN

    CREATE UNIQUE INDEX `IX_VoyageSeatAllocations_ReservationPassenger_Unique` ON `VoyageSeatAllocations` (`IdReservationPassenger`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260501134759_PhaseA_SiegeVoyageDestPassagerAllocation') THEN

    CREATE UNIQUE INDEX `IX_VoyageSeatAllocations_Voyage_Siege_Unique` ON `VoyageSeatAllocations` (`IdVoyage`, `IdSiege`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260501134759_PhaseA_SiegeVoyageDestPassagerAllocation') THEN


    INSERT INTO `VoyageDestinations` (`IdVoyage`, `IdDestination`, `Ordre`, `IdSociete`, `DateCreation`)
    SELECT v.`Id`, v.`IdDestination`, 1, v.`IdSociete`, UTC_TIMESTAMP(6)
    FROM `Voyages` v
    WHERE NOT EXISTS (
      SELECT 1 FROM `VoyageDestinations` vd WHERE vd.`IdVoyage` = v.`Id` AND vd.`Ordre` = 1
    );


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260501134759_PhaseA_SiegeVoyageDestPassagerAllocation') THEN


    INSERT INTO `Sieges` (`IdBus`, `NumeroOrdre`, `CodeSiege`, `EstActif`, `IdSociete`, `DateCreation`)
    SELECT b.`IdBus`, r.`n`, CONCAT(TRIM(b.`AliasBus`), '/', r.`n`), 1, b.`IdSociete`, UTC_TIMESTAMP(6)
    FROM `Buses` b
    JOIN (
      WITH RECURSIVE `seq` AS (
        SELECT 1 AS `n`
        UNION ALL
        SELECT `n` + 1 FROM `seq` WHERE `n` < 500
      )
      SELECT `n` FROM `seq`
    ) r ON r.`n` <= b.`NombreSiege`
    WHERE NOT EXISTS (
      SELECT 1 FROM `Sieges` s WHERE s.`IdBus` = b.`IdBus` AND s.`NumeroOrdre` = r.`n`
    );


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260501134759_PhaseA_SiegeVoyageDestPassagerAllocation') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260501134759_PhaseA_SiegeVoyageDestPassagerAllocation', '6.0.25');

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
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260502083420_PhaseB_BilletPassengerSiege') THEN

    ALTER TABLE `Billets` ADD `CodeSiege` varchar(120) CHARACTER SET utf8mb4 NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260502083420_PhaseB_BilletPassengerSiege') THEN

    ALTER TABLE `Billets` ADD `IdReservationPassenger` int NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260502083420_PhaseB_BilletPassengerSiege') THEN

    ALTER TABLE `Billets` ADD `IdSiege` int NULL;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260502083420_PhaseB_BilletPassengerSiege') THEN

    CREATE INDEX `IX_Billets_IdReservationPassenger` ON `Billets` (`IdReservationPassenger`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260502083420_PhaseB_BilletPassengerSiege') THEN

    CREATE INDEX `IX_Billets_IdSiege` ON `Billets` (`IdSiege`);

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260502083420_PhaseB_BilletPassengerSiege') THEN

    ALTER TABLE `Billets` ADD CONSTRAINT `FK_Billets_ReservationPassengers_IdReservationPassenger` FOREIGN KEY (`IdReservationPassenger`) REFERENCES `ReservationPassengers` (`IdReservationPassenger`) ON DELETE RESTRICT;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260502083420_PhaseB_BilletPassengerSiege') THEN

    ALTER TABLE `Billets` ADD CONSTRAINT `FK_Billets_Sieges_IdSiege` FOREIGN KEY (`IdSiege`) REFERENCES `Sieges` (`IdSiege`) ON DELETE RESTRICT;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260502083420_PhaseB_BilletPassengerSiege') THEN


    INSERT INTO `ReservationPassengers` (`IdReservation`, `IdClient`, `NomComplet`, `Telephone`, `Email`, `IdSociete`, `Statut`, `DateCreation`)
    SELECT r.`IdReservation`, r.`IdClient`, COALESCE(c.`NomClient`, CONCAT('Client #', r.`IdClient`)), c.`Telephone`, c.`EmailClient`, r.`IdSociete`, 1, UTC_TIMESTAMP(6)
    FROM `Reservations` r
    LEFT JOIN `Clients` c ON c.`IdClient` = r.`IdClient`
    WHERE NOT EXISTS (SELECT 1 FROM `ReservationPassengers` rp2 WHERE rp2.`IdReservation` = r.`IdReservation`);


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260502083420_PhaseB_BilletPassengerSiege') THEN


    INSERT INTO `VoyageSeatAllocations` (`IdVoyage`, `IdSiege`, `IdReservationPassenger`, `Statut`, `DateCreation`)
    SELECT v.`Id`, seat.`IdSiege`, pr.`IdReservationPassenger`, 'CONFIRME', UTC_TIMESTAMP(6)
    FROM (
      SELECT rp.`IdReservationPassenger`, r.`IdVoyage`,
             ROW_NUMBER() OVER (PARTITION BY r.`IdVoyage` ORDER BY r.`IdReservation`, rp.`IdReservationPassenger`) AS `rn`
      FROM `ReservationPassengers` rp
      INNER JOIN `Reservations` r ON r.`IdReservation` = rp.`IdReservation`
    ) pr
    INNER JOIN `Voyages` v ON v.`Id` = pr.`IdVoyage`
    INNER JOIN (
      SELECT v2.`Id` AS `IdVoyage`, s.`IdSiege`,
             ROW_NUMBER() OVER (PARTITION BY v2.`Id` ORDER BY s.`NumeroOrdre`) AS `sn`
      FROM `Voyages` v2
      INNER JOIN `Sieges` s ON s.`IdBus` = v2.`IdBus` AND s.`EstActif` = 1
    ) seat ON seat.`IdVoyage` = pr.`IdVoyage` AND seat.`sn` = pr.`rn`
    WHERE NOT EXISTS (
      SELECT 1 FROM `VoyageSeatAllocations` a WHERE a.`IdReservationPassenger` = pr.`IdReservationPassenger`
    );


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260502083420_PhaseB_BilletPassengerSiege') THEN


    UPDATE `Billets` b
    INNER JOIN `Reservations` r ON r.`IdReservation` = b.`IdReservation`
    INNER JOIN `ReservationPassengers` rp ON rp.`IdReservation` = r.`IdReservation`
    INNER JOIN `VoyageSeatAllocations` a ON a.`IdReservationPassenger` = rp.`IdReservationPassenger` AND a.`IdVoyage` = r.`IdVoyage`
    INNER JOIN `Sieges` s ON s.`IdSiege` = a.`IdSiege`
    SET
      b.`IdReservationPassenger` = rp.`IdReservationPassenger`,
      b.`IdSiege` = s.`IdSiege`,
      b.`CodeSiege` = s.`CodeSiege`
    WHERE b.`IdReservation` IS NOT NULL
      AND b.`IdReservationPassenger` IS NULL;


    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260502083420_PhaseB_BilletPassengerSiege') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20260502083420_PhaseB_BilletPassengerSiege', '6.0.25');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;

