-- =============================================================================
-- CongoTravel — Restaurant Phase 4 : Zones + ZoneQuotas (Mode B ClassQuota)
-- Prérequis : production_restaurant_v1.sql + production_restaurant_phase2_reservations.sql
-- =============================================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS `RestaurantZones` (
    `IdRestaurantZone` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `IdRestaurant` int NOT NULL,
    `Code` varchar(64) CHARACTER SET utf8mb4 NULL,
    `Libelle` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
    `Description` varchar(500) CHARACTER SET utf8mb4 NULL,
    `Actif` tinyint(1) NOT NULL DEFAULT 1,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_RestaurantZones` PRIMARY KEY (`IdRestaurantZone`),
    CONSTRAINT `FK_RestaurantZones_Societes`
        FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
    CONSTRAINT `FK_RestaurantZones_Restaurants`
        FOREIGN KEY (`IdRestaurant`) REFERENCES `Restaurants` (`IdRestaurant`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `RestaurantCreneauZoneQuotas` (
    `IdRestaurantCreneauZoneQuota` int NOT NULL AUTO_INCREMENT,
    `IdRestaurantCreneau` int NOT NULL,
    `IdRestaurantZone` int NOT NULL,
    `CapaciteTotale` int NOT NULL,
    `QuantiteHold` int NOT NULL DEFAULT 0,
    `QuantiteVendue` int NOT NULL DEFAULT 0,
    `PrixUnitaire` decimal(18,2) NOT NULL,
    CONSTRAINT `PK_RestaurantCreneauZoneQuotas` PRIMARY KEY (`IdRestaurantCreneauZoneQuota`),
    CONSTRAINT `CK_RestaurantCreneauZoneQuotas_Capacite` CHECK (`CapaciteTotale` >= 0),
    CONSTRAINT `CK_RestaurantCreneauZoneQuotas_StockPositive` CHECK (`QuantiteHold` >= 0 AND `QuantiteVendue` >= 0),
    CONSTRAINT `CK_RestaurantCreneauZoneQuotas_StockMax` CHECK (`QuantiteHold` + `QuantiteVendue` <= `CapaciteTotale`),
    CONSTRAINT `FK_RestaurantCreneauZoneQuotas_Creneaux`
        FOREIGN KEY (`IdRestaurantCreneau`) REFERENCES `RestaurantCreneaux` (`IdRestaurantCreneau`) ON DELETE CASCADE,
    CONSTRAINT `FK_RestaurantCreneauZoneQuotas_Zones`
        FOREIGN KEY (`IdRestaurantZone`) REFERENCES `RestaurantZones` (`IdRestaurantZone`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

SET FOREIGN_KEY_CHECKS = 1;

-- Indexes zones
SET @idx := (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'RestaurantZones' AND INDEX_NAME = 'IX_RestaurantZones_Restaurant_Code_UQ');
SET @sql := IF(@idx = 0, 'CREATE UNIQUE INDEX `IX_RestaurantZones_Restaurant_Code_UQ` ON `RestaurantZones` (`IdRestaurant`, `Code`)', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

SET @idx := (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'RestaurantZones' AND INDEX_NAME = 'IX_RestaurantZones_IdRestaurant');
SET @sql := IF(@idx = 0, 'CREATE INDEX `IX_RestaurantZones_IdRestaurant` ON `RestaurantZones` (`IdRestaurant`)', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

SET @idx := (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'RestaurantZones' AND INDEX_NAME = 'IX_RestaurantZones_IdSociete');
SET @sql := IF(@idx = 0, 'CREATE INDEX `IX_RestaurantZones_IdSociete` ON `RestaurantZones` (`IdSociete`)', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- Indexes zone quotas
SET @idx := (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'RestaurantCreneauZoneQuotas' AND INDEX_NAME = 'IX_RestaurantCreneauZoneQuotas_Creneau_Zone_UQ');
SET @sql := IF(@idx = 0, 'CREATE UNIQUE INDEX `IX_RestaurantCreneauZoneQuotas_Creneau_Zone_UQ` ON `RestaurantCreneauZoneQuotas` (`IdRestaurantCreneau`, `IdRestaurantZone`)', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

SET @idx := (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'RestaurantCreneauZoneQuotas' AND INDEX_NAME = 'IX_RestaurantCreneauZoneQuotas_IdRestaurantCreneau');
SET @sql := IF(@idx = 0, 'CREATE INDEX `IX_RestaurantCreneauZoneQuotas_IdRestaurantCreneau` ON `RestaurantCreneauZoneQuotas` (`IdRestaurantCreneau`)', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- FK ligne réservation → zone quota
SET @col := (SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'RestaurantReservationLines' AND COLUMN_NAME = 'IdRestaurantCreneauZoneQuota');
SET @sql := IF(
    @col = 0,
    'ALTER TABLE `RestaurantReservationLines` ADD `IdRestaurantCreneauZoneQuota` int NULL',
    'SELECT 1'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

SET @fk := (SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'RestaurantReservationLines' AND CONSTRAINT_NAME = 'FK_RestaurantReservationLines_ZoneQuota');
SET @sql := IF(
    @fk = 0,
    'ALTER TABLE `RestaurantReservationLines` ADD CONSTRAINT `FK_RestaurantReservationLines_ZoneQuota` FOREIGN KEY (`IdRestaurantCreneauZoneQuota`) REFERENCES `RestaurantCreneauZoneQuotas` (`IdRestaurantCreneauZoneQuota`) ON DELETE RESTRICT',
    'SELECT 1'
);
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

SET @idx := (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'RestaurantReservationLines' AND INDEX_NAME = 'IX_RestaurantReservationLines_IdZoneQuota');
SET @sql := IF(@idx = 0, 'CREATE INDEX `IX_RestaurantReservationLines_IdZoneQuota` ON `RestaurantReservationLines` (`IdRestaurantCreneauZoneQuota`)', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

-- =============================================================================
-- Procédure d'expiration HOLD (GlobalQuota + ZoneQuota ClassQuota)
-- =============================================================================

DELIMITER $$

DROP PROCEDURE IF EXISTS `sp_ExpireRestaurantHolds`$$
CREATE PROCEDURE `sp_ExpireRestaurantHolds`()
BEGIN
    DECLARE v_done INT DEFAULT 0;
    DECLARE v_IdRestaurantReservation INT;

    DECLARE cur CURSOR FOR
        SELECT r.`IdRestaurantReservation`
        FROM `RestaurantReservations` r
        WHERE r.`Status` = 'HOLD'
          AND r.`ExpiresAtUtc` IS NOT NULL
          AND r.`ExpiresAtUtc` < UTC_TIMESTAMP(6)
        ORDER BY r.`ExpiresAtUtc`
        FOR UPDATE;

    DECLARE CONTINUE HANDLER FOR NOT FOUND SET v_done = 1;

    START TRANSACTION;

    OPEN cur;
    read_loop: LOOP
        FETCH cur INTO v_IdRestaurantReservation;
        IF v_done = 1 THEN
            LEAVE read_loop;
        END IF;

        UPDATE `RestaurantCreneauGlobalQuotas` g
        JOIN `RestaurantReservations` r ON r.`IdRestaurantCreneau` = g.`IdRestaurantCreneau`
        JOIN `RestaurantReservationLines` rl ON rl.`IdRestaurantReservation` = r.`IdRestaurantReservation`
        SET g.`QuantiteHold` = GREATEST(0, g.`QuantiteHold` - rl.`Quantite`)
        WHERE r.`IdRestaurantReservation` = v_IdRestaurantReservation
          AND rl.`LineType` = 'GlobalQuota';

        UPDATE `RestaurantCreneauZoneQuotas` q
        JOIN `RestaurantReservationLines` rl ON rl.`IdRestaurantCreneauZoneQuota` = q.`IdRestaurantCreneauZoneQuota`
        SET q.`QuantiteHold` = GREATEST(0, q.`QuantiteHold` - rl.`Quantite`)
        WHERE rl.`IdRestaurantReservation` = v_IdRestaurantReservation
          AND rl.`LineType` = 'ClassQuota';

        UPDATE `RestaurantReservations`
        SET `Status` = 'EXPIRED',
            `DateModification` = UTC_TIMESTAMP(6)
        WHERE `IdRestaurantReservation` = v_IdRestaurantReservation
          AND `Status` = 'HOLD';
    END LOOP;
    CLOSE cur;

    COMMIT;
END$$

DELIMITER ;
