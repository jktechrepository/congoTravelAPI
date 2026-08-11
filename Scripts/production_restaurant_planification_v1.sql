-- =============================================================================
-- CongoTravel — Restaurant Planification V1.1 (multi-plages)
-- =============================================================================
-- Prérequis : production_restaurant_v1.sql + production_restaurant_phase4_zones.sql
-- Idempotent : CREATE TABLE IF NOT EXISTS + garde-fou colonnes FK.
-- =============================================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS `RestaurantPlanifications` (
    `IdRestaurantPlanification` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `IdRestaurant` int NOT NULL,
    `Libelle` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `JoursSemaine` longtext CHARACTER SET utf8mb4 NOT NULL,
    `InventoryMode` enum('GlobalQuota','ClassQuota') CHARACTER SET utf8mb4 NOT NULL,
    `CodeDevise` char(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF',
    `MontantAcompte` decimal(18,2) NULL,
    `Statut` tinyint(1) NOT NULL DEFAULT TRUE,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_RestaurantPlanifications` PRIMARY KEY (`IdRestaurantPlanification`),
    CONSTRAINT `FK_RestaurantPlanifications_Societes_IdSociete`
        FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
    CONSTRAINT `FK_RestaurantPlanifications_Restaurants_IdRestaurant`
        FOREIGN KEY (`IdRestaurant`) REFERENCES `Restaurants` (`IdRestaurant`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `RestaurantPlanificationPlages` (
    `IdRestaurantPlanificationPlage` int NOT NULL AUTO_INCREMENT,
    `IdRestaurantPlanification` int NOT NULL,
    `Ordre` int NOT NULL DEFAULT 0,
    `Libelle` varchar(120) CHARACTER SET utf8mb4 NULL,
    `StartTime` time NOT NULL,
    `EndTime` time NOT NULL,
    CONSTRAINT `PK_RestaurantPlanificationPlages` PRIMARY KEY (`IdRestaurantPlanificationPlage`),
    CONSTRAINT `CK_RestaurantPlanificationPlages_StartEnd` CHECK (`EndTime` > `StartTime`),
    CONSTRAINT `FK_RestaurantPlanificationPlages_Planifications`
        FOREIGN KEY (`IdRestaurantPlanification`)
            REFERENCES `RestaurantPlanifications` (`IdRestaurantPlanification`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `RestaurantPlanifPlageGlobalQuotas` (
    `IdRestaurantPlanificationPlage` int NOT NULL,
    `CapaciteTotale` int NOT NULL,
    `PrixUnitaire` decimal(18,2) NOT NULL,
    CONSTRAINT `PK_RestaurantPlanifPlageGlobalQuotas` PRIMARY KEY (`IdRestaurantPlanificationPlage`),
    CONSTRAINT `CK_RestaurantPlanifPlageGlobalQuotas_Capacite` CHECK (`CapaciteTotale` >= 0),
    CONSTRAINT `FK_RestaurantPlanifPlageGlobalQuotas_Plages`
        FOREIGN KEY (`IdRestaurantPlanificationPlage`)
            REFERENCES `RestaurantPlanificationPlages` (`IdRestaurantPlanificationPlage`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `RestaurantPlanifPlageZoneQuotas` (
    `IdRestaurantPlanifPlageZoneQuota` int NOT NULL AUTO_INCREMENT,
    `IdRestaurantPlanificationPlage` int NOT NULL,
    `IdRestaurantZone` int NOT NULL,
    `CapaciteTotale` int NOT NULL,
    `PrixUnitaire` decimal(18,2) NOT NULL,
    CONSTRAINT `PK_RestaurantPlanifPlageZoneQuotas` PRIMARY KEY (`IdRestaurantPlanifPlageZoneQuota`),
    CONSTRAINT `CK_RestaurantPlanifPlageZoneQuotas_Capacite` CHECK (`CapaciteTotale` >= 0),
    CONSTRAINT `FK_RestaurantPlanifPlageZoneQuotas_Plages`
        FOREIGN KEY (`IdRestaurantPlanificationPlage`)
            REFERENCES `RestaurantPlanificationPlages` (`IdRestaurantPlanificationPlage`) ON DELETE CASCADE,
    CONSTRAINT `FK_RestaurantPlanifPlageZoneQuotas_Zones`
        FOREIGN KEY (`IdRestaurantZone`)
            REFERENCES `RestaurantZones` (`IdRestaurantZone`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `RestaurantPlanifGenerationLogs` (
    `IdRestaurantPlanifGenerationLog` int NOT NULL AUTO_INCREMENT,
    `IdRestaurantPlanification` int NOT NULL,
    `DateDebut` datetime(6) NOT NULL,
    `DateFin` datetime(6) NOT NULL,
    `NombreCrees` int NOT NULL,
    `NombreIgnores` int NOT NULL,
    `NombreEchecs` int NOT NULL,
    `NombrePublies` int NOT NULL DEFAULT 0,
    `DetailsJson` longtext CHARACTER SET utf8mb4 NOT NULL,
    `DeclencheParIdUtilisateur` int NULL,
    `DateCreation` datetime(6) NOT NULL,
    CONSTRAINT `PK_RestaurantPlanifGenerationLogs` PRIMARY KEY (`IdRestaurantPlanifGenerationLog`),
    CONSTRAINT `FK_RestaurantPlanifGenerationLogs_Planifications`
        FOREIGN KEY (`IdRestaurantPlanification`)
            REFERENCES `RestaurantPlanifications` (`IdRestaurantPlanification`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

-- FK optionnelles sur créneaux (SET NULL)
SET @col_exists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'RestaurantCreneaux'
      AND COLUMN_NAME = 'IdRestaurantPlanification'
);
SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE `RestaurantCreneaux` ADD `IdRestaurantPlanification` int NULL',
    'SELECT ''RestaurantCreneaux.IdRestaurantPlanification déjà présent'' AS Info'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'RestaurantCreneaux'
      AND COLUMN_NAME = 'IdRestaurantPlanificationPlage'
);
SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE `RestaurantCreneaux` ADD `IdRestaurantPlanificationPlage` int NULL',
    'SELECT ''RestaurantCreneaux.IdRestaurantPlanificationPlage déjà présent'' AS Info'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @fk_exists := (
    SELECT COUNT(*)
    FROM information_schema.TABLE_CONSTRAINTS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'RestaurantCreneaux'
      AND CONSTRAINT_NAME = 'FK_RestaurantCreneaux_Planifications'
);
SET @sql := IF(
    @fk_exists = 0,
    'ALTER TABLE `RestaurantCreneaux` ADD CONSTRAINT `FK_RestaurantCreneaux_Planifications` FOREIGN KEY (`IdRestaurantPlanification`) REFERENCES `RestaurantPlanifications` (`IdRestaurantPlanification`) ON DELETE SET NULL',
    'SELECT ''FK_RestaurantCreneaux_Planifications déjà présente'' AS Info'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @fk_exists := (
    SELECT COUNT(*)
    FROM information_schema.TABLE_CONSTRAINTS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'RestaurantCreneaux'
      AND CONSTRAINT_NAME = 'FK_RestaurantCreneaux_PlanificationPlages'
);
SET @sql := IF(
    @fk_exists = 0,
    'ALTER TABLE `RestaurantCreneaux` ADD CONSTRAINT `FK_RestaurantCreneaux_PlanificationPlages` FOREIGN KEY (`IdRestaurantPlanificationPlage`) REFERENCES `RestaurantPlanificationPlages` (`IdRestaurantPlanificationPlage`) ON DELETE SET NULL',
    'SELECT ''FK_RestaurantCreneaux_PlanificationPlages déjà présente'' AS Info'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Index (ignore si déjà présents via procédure conditionnelle)
SET @idx_exists := (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'RestaurantPlanifications'
      AND INDEX_NAME = 'IX_RestaurantPlanifications_IdSociete'
);
SET @sql := IF(@idx_exists = 0,
    'CREATE INDEX `IX_RestaurantPlanifications_IdSociete` ON `RestaurantPlanifications` (`IdSociete`)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @idx_exists := (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'RestaurantPlanifications'
      AND INDEX_NAME = 'IX_RestaurantPlanifications_IdRestaurant'
);
SET @sql := IF(@idx_exists = 0,
    'CREATE INDEX `IX_RestaurantPlanifications_IdRestaurant` ON `RestaurantPlanifications` (`IdRestaurant`)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @idx_exists := (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'RestaurantPlanificationPlages'
      AND INDEX_NAME = 'IX_RestaurantPlanificationPlages_IdPlanification'
);
SET @sql := IF(@idx_exists = 0,
    'CREATE INDEX `IX_RestaurantPlanificationPlages_IdPlanification` ON `RestaurantPlanificationPlages` (`IdRestaurantPlanification`)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @idx_exists := (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'RestaurantPlanifPlageZoneQuotas'
      AND INDEX_NAME = 'IX_RestaurantPlanifPlageZoneQuotas_Plage_Zone_UQ'
);
SET @sql := IF(@idx_exists = 0,
    'CREATE UNIQUE INDEX `IX_RestaurantPlanifPlageZoneQuotas_Plage_Zone_UQ` ON `RestaurantPlanifPlageZoneQuotas` (`IdRestaurantPlanificationPlage`, `IdRestaurantZone`)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @idx_exists := (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'RestaurantPlanifPlageZoneQuotas'
      AND INDEX_NAME = 'IX_RestaurantPlanifPlageZoneQuotas_IdPlage'
);
SET @sql := IF(@idx_exists = 0,
    'CREATE INDEX `IX_RestaurantPlanifPlageZoneQuotas_IdPlage` ON `RestaurantPlanifPlageZoneQuotas` (`IdRestaurantPlanificationPlage`)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @idx_exists := (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'RestaurantPlanifGenerationLogs'
      AND INDEX_NAME = 'IX_RestaurantPlanifGenerationLogs_IdPlanification'
);
SET @sql := IF(@idx_exists = 0,
    'CREATE INDEX `IX_RestaurantPlanifGenerationLogs_IdPlanification` ON `RestaurantPlanifGenerationLogs` (`IdRestaurantPlanification`)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @idx_exists := (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'RestaurantCreneaux'
      AND INDEX_NAME = 'IX_RestaurantCreneaux_IdRestaurantPlanification'
);
SET @sql := IF(@idx_exists = 0,
    'CREATE INDEX `IX_RestaurantCreneaux_IdRestaurantPlanification` ON `RestaurantCreneaux` (`IdRestaurantPlanification`)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @idx_exists := (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'RestaurantCreneaux'
      AND INDEX_NAME = 'IX_RestaurantCreneaux_IdRestaurantPlanificationPlage'
);
SET @sql := IF(@idx_exists = 0,
    'CREATE INDEX `IX_RestaurantCreneaux_IdRestaurantPlanificationPlage` ON `RestaurantCreneaux` (`IdRestaurantPlanificationPlage`)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET FOREIGN_KEY_CHECKS = 1;
