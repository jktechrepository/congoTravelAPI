-- =============================================================================
-- CongoTravel — Hôtel Phase 7b GlobalQuota (MySQL, idempotent)
-- =============================================================================
-- Prérequis : production_hotel_phase3_reservations.sql + phase7a_planification.sql
-- =============================================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS `HotelNights` (
    `IdHotelNight` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `IdHotel` int NOT NULL,
    `NightDate` date NOT NULL,
    `CapaciteTotale` int NOT NULL,
    `QuantiteHold` int NOT NULL DEFAULT 0,
    `QuantiteVendue` int NOT NULL DEFAULT 0,
    `PrixNuit` decimal(18,2) NOT NULL,
    `CodeDevise` char(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF',
    `Status` enum('Draft','Published','Closed','Cancelled') NOT NULL DEFAULT 'Draft',
    `IdHotelPlanification` int NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_HotelNights` PRIMARY KEY (`IdHotelNight`),
    CONSTRAINT `CK_HotelNights_Capacite` CHECK (`CapaciteTotale` >= 0),
    CONSTRAINT `CK_HotelNights_StockPositive` CHECK (`QuantiteHold` >= 0 AND `QuantiteVendue` >= 0),
    CONSTRAINT `CK_HotelNights_StockMax` CHECK (`QuantiteHold` + `QuantiteVendue` <= `CapaciteTotale`),
    CONSTRAINT `FK_HotelNights_Societes` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
    CONSTRAINT `FK_HotelNights_Hotels` FOREIGN KEY (`IdHotel`) REFERENCES `Hotels` (`IdHotel`) ON DELETE RESTRICT,
    CONSTRAINT `FK_HotelNights_Planifications` FOREIGN KEY (`IdHotelPlanification`) REFERENCES `HotelPlanifications` (`IdHotelPlanification`) ON DELETE SET NULL,
    UNIQUE KEY `IX_HotelNights_Hotel_Night_UQ` (`IdHotel`, `NightDate`),
    KEY `IX_HotelNights_IdSociete` (`IdSociete`),
    KEY `IX_HotelNights_IdHotelPlanification` (`IdHotelPlanification`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `HotelPlanifGlobalQuotas` (
    `IdHotelPlanification` int NOT NULL,
    `CapaciteTotale` int NOT NULL,
    `PrixNuit` decimal(18,2) NOT NULL,
    CONSTRAINT `PK_HotelPlanifGlobalQuotas` PRIMARY KEY (`IdHotelPlanification`),
    CONSTRAINT `CK_HotelPlanifGlobalQuotas_Capacite` CHECK (`CapaciteTotale` >= 0),
    CONSTRAINT `FK_HotelPlanifGlobalQuotas_Planifications`
        FOREIGN KEY (`IdHotelPlanification`)
            REFERENCES `HotelPlanifications` (`IdHotelPlanification`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

-- InventoryMode on HotelPlanifications
SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'HotelPlanifications' AND COLUMN_NAME = 'InventoryMode'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `HotelPlanifications` ADD `InventoryMode` enum(''ClassQuota'',''GlobalQuota'') NOT NULL DEFAULT ''ClassQuota'' AFTER `JoursSemaine`',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- InventoryMode on HotelReservations
SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'HotelReservations' AND COLUMN_NAME = 'InventoryMode'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `HotelReservations` ADD `InventoryMode` enum(''ClassQuota'',''GlobalQuota'') NOT NULL DEFAULT ''ClassQuota'' AFTER `Status`',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- LineType on HotelReservationLines
SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'HotelReservationLines' AND COLUMN_NAME = 'LineType'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `HotelReservationLines` ADD `LineType` enum(''ClassQuota'',''GlobalQuota'') NOT NULL DEFAULT ''ClassQuota'' AFTER `IdHotelReservation`',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- Nullable IdHotelRoomType
SET @col_nullable := (
    SELECT IS_NULLABLE FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'HotelReservationLines' AND COLUMN_NAME = 'IdHotelRoomType'
);
SET @sql := IF(@col_nullable = 'NO',
    'ALTER TABLE `HotelReservationLines` MODIFY `IdHotelRoomType` int NULL',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- IdHotelNight on lines
SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'HotelReservationLines' AND COLUMN_NAME = 'IdHotelNight'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `HotelReservationLines` ADD `IdHotelNight` int NULL AFTER `IdHotelRoomType`',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @fk_exists := (
    SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'HotelReservationLines'
      AND CONSTRAINT_NAME = 'FK_HotelReservationLines_HotelNights'
);
SET @sql := IF(@fk_exists = 0,
    'ALTER TABLE `HotelReservationLines` ADD CONSTRAINT `FK_HotelReservationLines_HotelNights` FOREIGN KEY (`IdHotelNight`) REFERENCES `HotelNights` (`IdHotelNight`) ON DELETE RESTRICT',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @idx := (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'HotelReservationLines'
      AND INDEX_NAME = 'IX_HotelReservationLines_IdHotelNight'
);
SET @sql := IF(@idx = 0,
    'CREATE INDEX `IX_HotelReservationLines_IdHotelNight` ON `HotelReservationLines` (`IdHotelNight`)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET FOREIGN_KEY_CHECKS = 1;
SELECT 'production_hotel_phase7b_global_quota.sql appliqué' AS Info;
