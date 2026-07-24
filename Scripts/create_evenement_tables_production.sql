-- =============================================================================
-- CongoTravel — Création tables billetterie Événement (production)
-- =============================================================================
-- Schéma final aligné sur :
--   20260703101713_EvenementTicketingV1
--   20260703120104_EvenementSessionGlobalQuotaPricing
--
-- Prérequis :
--   - Table `Societes` (IdSociete) déjà présente
--   - Table `ConfigSocietes` déjà présente
--   - Table `__EFMigrationsHistory` déjà présente (ou créée ci-dessous)
--
-- Contenu :
--   1. Colonne ConfigSocietes.DureeHoldEvenementMinutes
--   2. 11 tables Evenement*
--   3. Indexes
--   4. Triggers de cohérence EvenementReservationLines
--   5. Stamp __EFMigrationsHistory
--
-- Usage :
--   mysql -h HOST -P 3306 -u USER -p DB_NAME < Scripts/create_evenement_tables_production.sql
--
-- Idempotent : CREATE TABLE IF NOT EXISTS + garde-fous colonne/history.
-- =============================================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ---------------------------------------------------------------------------
-- 0. Historique EF (si absent)
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

-- ---------------------------------------------------------------------------
-- 1. Config société — durée hold événement (minutes)
-- ---------------------------------------------------------------------------
SET @col_exists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'ConfigSocietes'
      AND COLUMN_NAME = 'DureeHoldEvenementMinutes'
);

SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE `ConfigSocietes` ADD `DureeHoldEvenementMinutes` int NOT NULL DEFAULT 15',
    'SELECT ''ConfigSocietes.DureeHoldEvenementMinutes déjà présent'' AS Info'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ---------------------------------------------------------------------------
-- 2. Tables Evenement*
-- ---------------------------------------------------------------------------

-- 2.1 Classes de billetterie (référentiel société)
CREATE TABLE IF NOT EXISTS `EvenementClasses` (
    `IdEvenementClasse` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `CodeClasse` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Libelle` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
    `Description` varchar(500) CHARACTER SET utf8mb4 NULL,
    `Statut` tinyint(1) NOT NULL DEFAULT TRUE,
    `DateCreation` datetime(6) NOT NULL,
    CONSTRAINT `PK_EvenementClasses` PRIMARY KEY (`IdEvenementClasse`),
    CONSTRAINT `FK_EvenementClasses_Societes_IdSociete`
        FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

-- 2.2 Sessions événement
CREATE TABLE IF NOT EXISTS `EvenementSessions` (
    `IdEvenementSession` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `IdSite` int NULL,
    `CodeSession` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `Libelle` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `StartAtUtc` datetime(6) NOT NULL,
    `EndAtUtc` datetime(6) NULL,
    `InventoryMode` enum('SeatNumbered','ClassQuota','GlobalQuota') CHARACTER SET utf8mb4 NOT NULL,
    `Status` enum('Draft','Published','Closed','Cancelled') CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Draft',
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_EvenementSessions` PRIMARY KEY (`IdEvenementSession`),
    CONSTRAINT `CK_EvenementSessions_StartEnd`
        CHECK (`EndAtUtc` IS NULL OR `EndAtUtc` >= `StartAtUtc`),
    CONSTRAINT `FK_EvenementSessions_Societes_IdSociete`
        FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
    CONSTRAINT `FK_EvenementSessions_Sites_IdSite`
        FOREIGN KEY (`IdSite`) REFERENCES `Sites` (`IdSite`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

-- 2.3 Sections de salle (mode SeatNumbered)
CREATE TABLE IF NOT EXISTS `EvenementSessionSections` (
    `IdEvenementSessionSection` int NOT NULL AUTO_INCREMENT,
    `IdEvenementSession` int NOT NULL,
    `CodeSection` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Libelle` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_EvenementSessionSections` PRIMARY KEY (`IdEvenementSessionSection`),
    CONSTRAINT `FK_EvenementSessionSections_EvenementSessions_IdEvenementSession`
        FOREIGN KEY (`IdEvenementSession`) REFERENCES `EvenementSessions` (`IdEvenementSession`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

-- 2.4 Quota global (mode GlobalQuota) — schéma final avec pricing
CREATE TABLE IF NOT EXISTS `EvenementSessionGlobalQuotas` (
    `IdEvenementSession` int NOT NULL,
    `CapaciteTotale` int NOT NULL,
    `QuantiteHold` int NOT NULL DEFAULT 0,
    `QuantiteVendue` int NOT NULL DEFAULT 0,
    `PrixUnitaire` decimal(18,2) NOT NULL DEFAULT 0.00,
    `CodeDevise` char(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF',
    CONSTRAINT `PK_EvenementSessionGlobalQuotas` PRIMARY KEY (`IdEvenementSession`),
    CONSTRAINT `CK_EvenementSessionGlobalQuotas_Capacite` CHECK (`CapaciteTotale` >= 0),
    CONSTRAINT `CK_EvenementSessionGlobalQuotas_StockPositive` CHECK (`QuantiteHold` >= 0 AND `QuantiteVendue` >= 0),
    CONSTRAINT `CK_EvenementSessionGlobalQuotas_StockMax` CHECK (`QuantiteHold` + `QuantiteVendue` <= `CapaciteTotale`),
    CONSTRAINT `FK_EvenementSessionGlobalQuotas_EvenementSessions_IdEvenementSession`
        FOREIGN KEY (`IdEvenementSession`) REFERENCES `EvenementSessions` (`IdEvenementSession`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

-- 2.5 Quotas par classe (mode ClassQuota)
CREATE TABLE IF NOT EXISTS `EvenementSessionClassQuotas` (
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
    CONSTRAINT `CK_EvenementSessionClassQuotas_StockPositive` CHECK (`QuantiteHold` >= 0 AND `QuantiteVendue` >= 0),
    CONSTRAINT `CK_EvenementSessionClassQuotas_StockMax` CHECK (`QuantiteHold` + `QuantiteVendue` <= `CapaciteTotale`),
    CONSTRAINT `FK_EvenementSessionClassQuotas_EvenementSessions_IdEvenementSession`
        FOREIGN KEY (`IdEvenementSession`) REFERENCES `EvenementSessions` (`IdEvenementSession`) ON DELETE CASCADE,
    CONSTRAINT `FK_EvenementSessionClassQuotas_EvenementClasses_IdEvenementClasse`
        FOREIGN KEY (`IdEvenementClasse`) REFERENCES `EvenementClasses` (`IdEvenementClasse`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

-- 2.6 Réservations événement
CREATE TABLE IF NOT EXISTS `EvenementReservations` (
    `IdEvenementReservation` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `IdEvenementSession` int NOT NULL,
    `IdSite` int NULL,
    `ReferenceReservation` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `CustomerRef` varchar(100) CHARACTER SET utf8mb4 NULL,
    `Status` enum('HOLD','CONFIRMED','CANCELLED','EXPIRED') CHARACTER SET utf8mb4 NOT NULL,
    `ExpiresAtUtc` datetime(6) NULL,
    `MontantSousTotal` decimal(18,2) NOT NULL DEFAULT 0.00,
    `CodeDevise` char(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF',
    `IdempotencyKey` varchar(120) CHARACTER SET utf8mb4 NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_EvenementReservations` PRIMARY KEY (`IdEvenementReservation`),
    CONSTRAINT `FK_EvenementReservations_Societes_IdSociete`
        FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
    CONSTRAINT `FK_EvenementReservations_EvenementSessions_IdEvenementSession`
        FOREIGN KEY (`IdEvenementSession`) REFERENCES `EvenementSessions` (`IdEvenementSession`) ON DELETE RESTRICT,
    CONSTRAINT `FK_EvenementReservations_Sites_IdSite`
        FOREIGN KEY (`IdSite`) REFERENCES `Sites` (`IdSite`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

-- 2.7 Sièges numérotés (mode SeatNumbered)
CREATE TABLE IF NOT EXISTS `EvenementSessionSeats` (
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
    CONSTRAINT `FK_EvenementSessionSeats_EvenementSessions_IdEvenementSession`
        FOREIGN KEY (`IdEvenementSession`) REFERENCES `EvenementSessions` (`IdEvenementSession`) ON DELETE CASCADE,
    CONSTRAINT `FK_EvenementSessionSeats_EvenementSessionSections_IdEvenementSessionSection`
        FOREIGN KEY (`IdEvenementSessionSection`) REFERENCES `EvenementSessionSections` (`IdEvenementSessionSection`) ON DELETE SET NULL,
    CONSTRAINT `FK_EvenementSessionSeats_EvenementClasses_IdEvenementClasse`
        FOREIGN KEY (`IdEvenementClasse`) REFERENCES `EvenementClasses` (`IdEvenementClasse`) ON DELETE SET NULL,
    CONSTRAINT `FK_EvenementSessionSeats_EvenementReservations_IdEvenementReservationCourante`
        FOREIGN KEY (`IdEvenementReservationCourante`) REFERENCES `EvenementReservations` (`IdEvenementReservation`) ON DELETE SET NULL
) CHARACTER SET=utf8mb4;

-- 2.8 Lignes de réservation
CREATE TABLE IF NOT EXISTS `EvenementReservationLines` (
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
    CONSTRAINT `FK_EvenementReservationLines_EvenementReservations_IdEvenementReservation`
        FOREIGN KEY (`IdEvenementReservation`) REFERENCES `EvenementReservations` (`IdEvenementReservation`) ON DELETE CASCADE,
    CONSTRAINT `FK_EvenementReservationLines_EvenementSessionSeats_IdEvenementSessionSeat`
        FOREIGN KEY (`IdEvenementSessionSeat`) REFERENCES `EvenementSessionSeats` (`IdEvenementSessionSeat`) ON DELETE RESTRICT,
    CONSTRAINT `FK_EvenementReservationLines_EvenementSessionClassQuotas_IdEvenementSessionClassQuota`
        FOREIGN KEY (`IdEvenementSessionClassQuota`) REFERENCES `EvenementSessionClassQuotas` (`IdEvenementSessionClassQuota`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

-- 2.9 Tickets
CREATE TABLE IF NOT EXISTS `EvenementTickets` (
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

-- 2.10 Paiements événement (FlexPay / autres providers)
CREATE TABLE IF NOT EXISTS `EvenementPayments` (
    `IdEvenementPayment` int NOT NULL AUTO_INCREMENT,
    `IdEvenementReservation` int NOT NULL,
    `IdSite` int NULL,
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
    CONSTRAINT `FK_EvenementPayments_EvenementReservations_IdEvenementReservation`
        FOREIGN KEY (`IdEvenementReservation`) REFERENCES `EvenementReservations` (`IdEvenementReservation`) ON DELETE RESTRICT,
    CONSTRAINT `FK_EvenementPayments_Sites_IdSite`
        FOREIGN KEY (`IdSite`) REFERENCES `Sites` (`IdSite`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

-- ---------------------------------------------------------------------------
-- 2.11 Upgrade éventuel si GlobalQuotas existe déjà sans pricing
-- ---------------------------------------------------------------------------
SET @gq_prix := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'EvenementSessionGlobalQuotas'
      AND COLUMN_NAME = 'PrixUnitaire'
);
SET @sql := IF(
    @gq_prix = 0
    AND EXISTS (
        SELECT 1 FROM information_schema.TABLES
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'EvenementSessionGlobalQuotas'
    ),
    'ALTER TABLE `EvenementSessionGlobalQuotas` ADD `PrixUnitaire` decimal(18,2) NOT NULL DEFAULT 0.00',
    'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @gq_devise := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'EvenementSessionGlobalQuotas'
      AND COLUMN_NAME = 'CodeDevise'
);
SET @sql := IF(
    @gq_devise = 0
    AND EXISTS (
        SELECT 1 FROM information_schema.TABLES
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'EvenementSessionGlobalQuotas'
    ),
    'ALTER TABLE `EvenementSessionGlobalQuotas` ADD `CodeDevise` char(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT ''CDF''',
    'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET FOREIGN_KEY_CHECKS = 1;

-- ---------------------------------------------------------------------------
-- 3. Indexes (création conditionnelle)
-- ---------------------------------------------------------------------------
DROP PROCEDURE IF EXISTS `sp_CreateIndexIfNotExists`;
DELIMITER //
CREATE PROCEDURE `sp_CreateIndexIfNotExists`(
    IN p_table VARCHAR(128),
    IN p_index VARCHAR(128),
    IN p_ddl TEXT
)
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = p_table
          AND INDEX_NAME = p_index
    ) THEN
        SET @ddl := p_ddl;
        PREPARE s FROM @ddl;
        EXECUTE s;
        DEALLOCATE PREPARE s;
    END IF;
END //
DELIMITER ;

-- EvenementClasses
CALL sp_CreateIndexIfNotExists('EvenementClasses', 'IX_EvenementClasses_IdSociete',
    'CREATE INDEX `IX_EvenementClasses_IdSociete` ON `EvenementClasses` (`IdSociete`)');
CALL sp_CreateIndexIfNotExists('EvenementClasses', 'IX_EvenementClasses_Societe_CodeClasse_UQ',
    'CREATE UNIQUE INDEX `IX_EvenementClasses_Societe_CodeClasse_UQ` ON `EvenementClasses` (`IdSociete`, `CodeClasse`)');

-- EvenementSessions
CALL sp_CreateIndexIfNotExists('EvenementSessions', 'IX_EvenementSessions_IdSociete_StartAtUtc',
    'CREATE INDEX `IX_EvenementSessions_IdSociete_StartAtUtc` ON `EvenementSessions` (`IdSociete`, `StartAtUtc`)');
CALL sp_CreateIndexIfNotExists('EvenementSessions', 'IX_EvenementSessions_IdSite',
    'CREATE INDEX `IX_EvenementSessions_IdSite` ON `EvenementSessions` (`IdSite`)');
CALL sp_CreateIndexIfNotExists('EvenementReservations', 'IX_EvenementReservations_IdSite',
    'CREATE INDEX `IX_EvenementReservations_IdSite` ON `EvenementReservations` (`IdSite`)');
CALL sp_CreateIndexIfNotExists('EvenementPayments', 'IX_EvenementPayments_IdSite',
    'CREATE INDEX `IX_EvenementPayments_IdSite` ON `EvenementPayments` (`IdSite`)');
CALL sp_CreateIndexIfNotExists('EvenementSessions', 'IX_EvenementSessions_Societe_CodeSession_UQ',
    'CREATE UNIQUE INDEX `IX_EvenementSessions_Societe_CodeSession_UQ` ON `EvenementSessions` (`IdSociete`, `CodeSession`)');

-- EvenementSessionSections
CALL sp_CreateIndexIfNotExists('EvenementSessionSections', 'IX_EvenementSessionSections_IdEvenementSession',
    'CREATE INDEX `IX_EvenementSessionSections_IdEvenementSession` ON `EvenementSessionSections` (`IdEvenementSession`)');
CALL sp_CreateIndexIfNotExists('EvenementSessionSections', 'IX_EvenementSessionSections_Session_CodeSection_UQ',
    'CREATE UNIQUE INDEX `IX_EvenementSessionSections_Session_CodeSection_UQ` ON `EvenementSessionSections` (`IdEvenementSession`, `CodeSection`)');

-- EvenementSessionClassQuotas
CALL sp_CreateIndexIfNotExists('EvenementSessionClassQuotas', 'IX_EvenementSessionClassQuotas_IdEvenementClasse',
    'CREATE INDEX `IX_EvenementSessionClassQuotas_IdEvenementClasse` ON `EvenementSessionClassQuotas` (`IdEvenementClasse`)');
CALL sp_CreateIndexIfNotExists('EvenementSessionClassQuotas', 'IX_EvenementSessionClassQuotas_IdEvenementSession',
    'CREATE INDEX `IX_EvenementSessionClassQuotas_IdEvenementSession` ON `EvenementSessionClassQuotas` (`IdEvenementSession`)');
CALL sp_CreateIndexIfNotExists('EvenementSessionClassQuotas', 'IX_EvenementSessionClassQuotas_Session_Classe_UQ',
    'CREATE UNIQUE INDEX `IX_EvenementSessionClassQuotas_Session_Classe_UQ` ON `EvenementSessionClassQuotas` (`IdEvenementSession`, `IdEvenementClasse`)');

-- EvenementReservations
CALL sp_CreateIndexIfNotExists('EvenementReservations', 'IX_EvenementReservations_Session_Status',
    'CREATE INDEX `IX_EvenementReservations_Session_Status` ON `EvenementReservations` (`IdEvenementSession`, `Status`)');
CALL sp_CreateIndexIfNotExists('EvenementReservations', 'IX_EvenementReservations_Status_ExpiresAtUtc',
    'CREATE INDEX `IX_EvenementReservations_Status_ExpiresAtUtc` ON `EvenementReservations` (`Status`, `ExpiresAtUtc`)');
CALL sp_CreateIndexIfNotExists('EvenementReservations', 'IX_EvenementReservations_Societe_Reference_UQ',
    'CREATE UNIQUE INDEX `IX_EvenementReservations_Societe_Reference_UQ` ON `EvenementReservations` (`IdSociete`, `ReferenceReservation`)');
CALL sp_CreateIndexIfNotExists('EvenementReservations', 'IX_EvenementReservations_Societe_Idempotency_UQ',
    'CREATE UNIQUE INDEX `IX_EvenementReservations_Societe_Idempotency_UQ` ON `EvenementReservations` (`IdSociete`, `IdempotencyKey`)');

-- EvenementSessionSeats
CALL sp_CreateIndexIfNotExists('EvenementSessionSeats', 'IX_EvenementSessionSeats_HoldExpireAtUtc',
    'CREATE INDEX `IX_EvenementSessionSeats_HoldExpireAtUtc` ON `EvenementSessionSeats` (`HoldExpireAtUtc`)');
CALL sp_CreateIndexIfNotExists('EvenementSessionSeats', 'IX_EvenementSessionSeats_IdEvenementClasse',
    'CREATE INDEX `IX_EvenementSessionSeats_IdEvenementClasse` ON `EvenementSessionSeats` (`IdEvenementClasse`)');
CALL sp_CreateIndexIfNotExists('EvenementSessionSeats', 'IX_EvenementSessionSeats_IdEvenementReservationCourante',
    'CREATE INDEX `IX_EvenementSessionSeats_IdEvenementReservationCourante` ON `EvenementSessionSeats` (`IdEvenementReservationCourante`)');
CALL sp_CreateIndexIfNotExists('EvenementSessionSeats', 'IX_EvenementSessionSeats_IdEvenementSessionSection',
    'CREATE INDEX `IX_EvenementSessionSeats_IdEvenementSessionSection` ON `EvenementSessionSeats` (`IdEvenementSessionSection`)');
CALL sp_CreateIndexIfNotExists('EvenementSessionSeats', 'IX_EvenementSessionSeats_Session_SeatCode_UQ',
    'CREATE UNIQUE INDEX `IX_EvenementSessionSeats_Session_SeatCode_UQ` ON `EvenementSessionSeats` (`IdEvenementSession`, `SeatCode`)');
CALL sp_CreateIndexIfNotExists('EvenementSessionSeats', 'IX_EvenementSessionSeats_Session_SeatStatus',
    'CREATE INDEX `IX_EvenementSessionSeats_Session_SeatStatus` ON `EvenementSessionSeats` (`IdEvenementSession`, `SeatStatus`)');

-- EvenementReservationLines
CALL sp_CreateIndexIfNotExists('EvenementReservationLines', 'IX_EvenementReservationLines_IdEvenementReservation',
    'CREATE INDEX `IX_EvenementReservationLines_IdEvenementReservation` ON `EvenementReservationLines` (`IdEvenementReservation`)');
CALL sp_CreateIndexIfNotExists('EvenementReservationLines', 'IX_EvenementReservationLines_IdEvenementSessionClassQuota',
    'CREATE INDEX `IX_EvenementReservationLines_IdEvenementSessionClassQuota` ON `EvenementReservationLines` (`IdEvenementSessionClassQuota`)');
CALL sp_CreateIndexIfNotExists('EvenementReservationLines', 'IX_EvenementReservationLines_IdEvenementSessionSeat',
    'CREATE INDEX `IX_EvenementReservationLines_IdEvenementSessionSeat` ON `EvenementReservationLines` (`IdEvenementSessionSeat`)');
CALL sp_CreateIndexIfNotExists('EvenementReservationLines', 'IX_EvenementReservationLines_Reservation_Seat_UQ',
    'CREATE UNIQUE INDEX `IX_EvenementReservationLines_Reservation_Seat_UQ` ON `EvenementReservationLines` (`IdEvenementReservation`, `IdEvenementSessionSeat`)');

-- EvenementTickets
CALL sp_CreateIndexIfNotExists('EvenementTickets', 'IX_EvenementTickets_IdEvenementReservationLine',
    'CREATE INDEX `IX_EvenementTickets_IdEvenementReservationLine` ON `EvenementTickets` (`IdEvenementReservationLine`)');
CALL sp_CreateIndexIfNotExists('EvenementTickets', 'IX_EvenementTickets_Status',
    'CREATE INDEX `IX_EvenementTickets_Status` ON `EvenementTickets` (`Status`)');
CALL sp_CreateIndexIfNotExists('EvenementTickets', 'IX_EvenementTickets_TicketCode_UQ',
    'CREATE UNIQUE INDEX `IX_EvenementTickets_TicketCode_UQ` ON `EvenementTickets` (`TicketCode`)');

-- EvenementPayments
CALL sp_CreateIndexIfNotExists('EvenementPayments', 'IX_EvenementPayments_Idempotency_UQ',
    'CREATE UNIQUE INDEX `IX_EvenementPayments_Idempotency_UQ` ON `EvenementPayments` (`IdempotencyKey`)');
CALL sp_CreateIndexIfNotExists('EvenementPayments', 'IX_EvenementPayments_ReferencePaiement_UQ',
    'CREATE UNIQUE INDEX `IX_EvenementPayments_ReferencePaiement_UQ` ON `EvenementPayments` (`ReferencePaiement`)');
CALL sp_CreateIndexIfNotExists('EvenementPayments', 'IX_EvenementPayments_Reservation_Status',
    'CREATE INDEX `IX_EvenementPayments_Reservation_Status` ON `EvenementPayments` (`IdEvenementReservation`, `Status`)');

DROP PROCEDURE IF EXISTS `sp_CreateIndexIfNotExists`;

-- ---------------------------------------------------------------------------
-- 4. Triggers de cohérence EvenementReservationLines
-- ---------------------------------------------------------------------------
DELIMITER $$

DROP TRIGGER IF EXISTS `TRG_EvenementReservationLines_BI`$$
CREATE TRIGGER `TRG_EvenementReservationLines_BI`
BEFORE INSERT ON `EvenementReservationLines`
FOR EACH ROW
BEGIN
    IF NEW.`LineType` = 'Seat' THEN
        IF NEW.`Quantite` <> 1 OR NEW.`IdEvenementSessionSeat` IS NULL OR NEW.`IdEvenementSessionClassQuota` IS NOT NULL THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'LineType Seat invalide: Quantite=1, Seat obligatoire, ClassQuota null.';
        END IF;
    ELSEIF NEW.`LineType` = 'ClassQuota' THEN
        IF NEW.`IdEvenementSessionClassQuota` IS NULL OR NEW.`IdEvenementSessionSeat` IS NOT NULL THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'LineType ClassQuota invalide: ClassQuota obligatoire, Seat null.';
        END IF;
    ELSEIF NEW.`LineType` = 'GlobalQuota' THEN
        IF NEW.`IdEvenementSessionSeat` IS NOT NULL OR NEW.`IdEvenementSessionClassQuota` IS NOT NULL THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'LineType GlobalQuota invalide: Seat et ClassQuota doivent etre null.';
        END IF;
    END IF;
END$$

DROP TRIGGER IF EXISTS `TRG_EvenementReservationLines_BU`$$
CREATE TRIGGER `TRG_EvenementReservationLines_BU`
BEFORE UPDATE ON `EvenementReservationLines`
FOR EACH ROW
BEGIN
    IF NEW.`LineType` = 'Seat' THEN
        IF NEW.`Quantite` <> 1 OR NEW.`IdEvenementSessionSeat` IS NULL OR NEW.`IdEvenementSessionClassQuota` IS NOT NULL THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'LineType Seat invalide: Quantite=1, Seat obligatoire, ClassQuota null.';
        END IF;
    ELSEIF NEW.`LineType` = 'ClassQuota' THEN
        IF NEW.`IdEvenementSessionClassQuota` IS NULL OR NEW.`IdEvenementSessionSeat` IS NOT NULL THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'LineType ClassQuota invalide: ClassQuota obligatoire, Seat null.';
        END IF;
    ELSEIF NEW.`LineType` = 'GlobalQuota' THEN
        IF NEW.`IdEvenementSessionSeat` IS NOT NULL OR NEW.`IdEvenementSessionClassQuota` IS NOT NULL THEN
            SIGNAL SQLSTATE '45000'
                SET MESSAGE_TEXT = 'LineType GlobalQuota invalide: Seat et ClassQuota doivent etre null.';
        END IF;
    END IF;
END$$

DELIMITER ;

-- ---------------------------------------------------------------------------
-- 5. Stamp EF Migrations History
-- ---------------------------------------------------------------------------
INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES
    ('20260703101713_EvenementTicketingV1', '6.0.25'),
    ('20260703120104_EvenementSessionGlobalQuotaPricing', '6.0.25');

-- ---------------------------------------------------------------------------
-- 6. Vérification rapide
-- ---------------------------------------------------------------------------
SELECT TABLE_NAME
FROM information_schema.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME LIKE 'Evenement%'
ORDER BY TABLE_NAME;

SELECT COLUMN_NAME, COLUMN_TYPE, COLUMN_DEFAULT
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'ConfigSocietes'
  AND COLUMN_NAME = 'DureeHoldEvenementMinutes';

SELECT `MigrationId`
FROM `__EFMigrationsHistory`
WHERE `MigrationId` IN (
    '20260703101713_EvenementTicketingV1',
    '20260703120104_EvenementSessionGlobalQuotaPricing'
)
ORDER BY `MigrationId`;

SELECT 'OK — Tables Evenement créées / déjà présentes' AS Resultat;
