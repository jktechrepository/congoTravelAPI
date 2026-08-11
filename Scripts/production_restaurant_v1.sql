-- =============================================================================
-- CongoTravel — Réservation Restaurant V1 Phase 1 (tables + ConfigSociete)
-- =============================================================================
-- Prérequis : Societes, Sites, ConfigSocietes
-- Idempotent : CREATE TABLE IF NOT EXISTS + garde-fou colonne.
-- =============================================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- Config : durée hold restaurant
SET @col_exists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'ConfigSocietes'
      AND COLUMN_NAME = 'DureeHoldRestaurantMinutes'
);
SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE `ConfigSocietes` ADD `DureeHoldRestaurantMinutes` int NOT NULL DEFAULT 15',
    'SELECT ''ConfigSocietes.DureeHoldRestaurantMinutes déjà présent'' AS Info'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

CREATE TABLE IF NOT EXISTS `Restaurants` (
    `IdRestaurant` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `IdSite` int NULL,
    `CodeRestaurant` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `Nom` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Description` varchar(2000) CHARACTER SET utf8mb4 NULL,
    `Adresse` varchar(500) CHARACTER SET utf8mb4 NULL,
    `AcomptePourcentDefaut` decimal(5,2) NOT NULL DEFAULT 0.00,
    `Status` enum('Draft','Published','Closed','Cancelled') CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Draft',
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_Restaurants` PRIMARY KEY (`IdRestaurant`),
    CONSTRAINT `FK_Restaurants_Societes_IdSociete`
        FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Restaurants_Sites_IdSite`
        FOREIGN KEY (`IdSite`) REFERENCES `Sites` (`IdSite`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `RestaurantCreneaux` (
    `IdRestaurantCreneau` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `IdRestaurant` int NOT NULL,
    `DateService` date NOT NULL,
    `StartAtUtc` datetime(6) NOT NULL,
    `EndAtUtc` datetime(6) NOT NULL,
    `InventoryMode` enum('GlobalQuota','ClassQuota') CHARACTER SET utf8mb4 NOT NULL,
    `Status` enum('Draft','Published','Closed','Cancelled') CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Draft',
    `CodeDevise` char(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF',
    `MontantAcompte` decimal(18,2) NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_RestaurantCreneaux` PRIMARY KEY (`IdRestaurantCreneau`),
    CONSTRAINT `CK_RestaurantCreneaux_StartEnd` CHECK (`EndAtUtc` > `StartAtUtc`),
    CONSTRAINT `FK_RestaurantCreneaux_Societes_IdSociete`
        FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
    CONSTRAINT `FK_RestaurantCreneaux_Restaurants_IdRestaurant`
        FOREIGN KEY (`IdRestaurant`) REFERENCES `Restaurants` (`IdRestaurant`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `RestaurantCreneauGlobalQuotas` (
    `IdRestaurantCreneau` int NOT NULL,
    `CapaciteTotale` int NOT NULL,
    `QuantiteHold` int NOT NULL DEFAULT 0,
    `QuantiteVendue` int NOT NULL DEFAULT 0,
    `PrixUnitaire` decimal(18,2) NOT NULL,
    CONSTRAINT `PK_RestaurantCreneauGlobalQuotas` PRIMARY KEY (`IdRestaurantCreneau`),
    CONSTRAINT `CK_RestaurantCreneauGlobalQuotas_Capacite` CHECK (`CapaciteTotale` >= 0),
    CONSTRAINT `CK_RestaurantCreneauGlobalQuotas_StockPositive` CHECK (`QuantiteHold` >= 0 AND `QuantiteVendue` >= 0),
    CONSTRAINT `CK_RestaurantCreneauGlobalQuotas_StockMax` CHECK (`QuantiteHold` + `QuantiteVendue` <= `CapaciteTotale`),
    CONSTRAINT `FK_RestaurantCreneauGlobalQuotas_Creneaux`
        FOREIGN KEY (`IdRestaurantCreneau`) REFERENCES `RestaurantCreneaux` (`IdRestaurantCreneau`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

SET FOREIGN_KEY_CHECKS = 1;

-- Indexes (idempotent via procedure-style checks)
SET @idx_exists := (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Restaurants'
      AND INDEX_NAME = 'IX_Restaurants_Societe_CodeRestaurant_UQ'
);
SET @sql := IF(
    @idx_exists = 0,
    'CREATE UNIQUE INDEX `IX_Restaurants_Societe_CodeRestaurant_UQ` ON `Restaurants` (`IdSociete`, `CodeRestaurant`)',
    'SELECT ''IX_Restaurants_Societe_CodeRestaurant_UQ déjà présent'' AS Info'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @idx_exists := (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Restaurants'
      AND INDEX_NAME = 'IX_Restaurants_IdSite'
);
SET @sql := IF(
    @idx_exists = 0,
    'CREATE INDEX `IX_Restaurants_IdSite` ON `Restaurants` (`IdSite`)',
    'SELECT ''IX_Restaurants_IdSite déjà présent'' AS Info'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @idx_exists := (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'RestaurantCreneaux'
      AND INDEX_NAME = 'IX_RestaurantCreneaux_IdRestaurant_StartAtUtc'
);
SET @sql := IF(
    @idx_exists = 0,
    'CREATE INDEX `IX_RestaurantCreneaux_IdRestaurant_StartAtUtc` ON `RestaurantCreneaux` (`IdRestaurant`, `StartAtUtc`)',
    'SELECT ''IX_RestaurantCreneaux_IdRestaurant_StartAtUtc déjà présent'' AS Info'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @idx_exists := (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'RestaurantCreneaux'
      AND INDEX_NAME = 'IX_RestaurantCreneaux_IdSociete_DateService'
);
SET @sql := IF(
    @idx_exists = 0,
    'CREATE INDEX `IX_RestaurantCreneaux_IdSociete_DateService` ON `RestaurantCreneaux` (`IdSociete`, `DateService`)',
    'SELECT ''IX_RestaurantCreneaux_IdSociete_DateService déjà présent'' AS Info'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @idx_exists := (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'RestaurantCreneaux'
      AND INDEX_NAME = 'IX_RestaurantCreneaux_IdRestaurant'
);
SET @sql := IF(
    @idx_exists = 0,
    'CREATE INDEX `IX_RestaurantCreneaux_IdRestaurant` ON `RestaurantCreneaux` (`IdRestaurant`)',
    'SELECT ''IX_RestaurantCreneaux_IdRestaurant déjà présent'' AS Info'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SELECT 'production_restaurant_v1.sql appliqué' AS Info;
