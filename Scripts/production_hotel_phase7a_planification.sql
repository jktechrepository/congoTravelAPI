-- =============================================================================
-- CongoTravel — Hôtel Phase 7a Planifications (MySQL, idempotent)
-- =============================================================================
-- Prérequis : production_hotel_v1.sql + production_hotel_phase2_allotments.sql
-- =============================================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS `HotelPlanifications` (
    `IdHotelPlanification` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `IdHotel` int NOT NULL,
    `Libelle` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `JoursSemaine` longtext CHARACTER SET utf8mb4 NOT NULL,
    `CodeDevise` char(3) CHARACTER SET utf8mb4 NULL,
    `Statut` tinyint(1) NOT NULL DEFAULT TRUE,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_HotelPlanifications` PRIMARY KEY (`IdHotelPlanification`),
    CONSTRAINT `FK_HotelPlanifications_Societes_IdSociete`
        FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
    CONSTRAINT `FK_HotelPlanifications_Hotels_IdHotel`
        FOREIGN KEY (`IdHotel`) REFERENCES `Hotels` (`IdHotel`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `HotelPlanificationLignes` (
    `IdHotelPlanificationLigne` int NOT NULL AUTO_INCREMENT,
    `IdHotelPlanification` int NOT NULL,
    `IdHotelRoomType` int NOT NULL,
    `CapaciteTotale` int NOT NULL,
    `PrixNuit` decimal(18,2) NOT NULL,
    CONSTRAINT `PK_HotelPlanificationLignes` PRIMARY KEY (`IdHotelPlanificationLigne`),
    CONSTRAINT `CK_HotelPlanificationLignes_Capacite` CHECK (`CapaciteTotale` >= 0),
    CONSTRAINT `FK_HotelPlanificationLignes_Planifications`
        FOREIGN KEY (`IdHotelPlanification`)
            REFERENCES `HotelPlanifications` (`IdHotelPlanification`) ON DELETE CASCADE,
    CONSTRAINT `FK_HotelPlanificationLignes_RoomTypes`
        FOREIGN KEY (`IdHotelRoomType`)
            REFERENCES `HotelRoomTypes` (`IdHotelRoomType`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `HotelPlanifGenerationLogs` (
    `IdHotelPlanifGenerationLog` int NOT NULL AUTO_INCREMENT,
    `IdHotelPlanification` int NOT NULL,
    `DateDebut` datetime(6) NOT NULL,
    `DateFin` datetime(6) NOT NULL,
    `NombreCrees` int NOT NULL,
    `NombreIgnores` int NOT NULL,
    `NombreEchecs` int NOT NULL,
    `DetailsJson` longtext CHARACTER SET utf8mb4 NOT NULL,
    `DeclencheParIdUtilisateur` int NULL,
    `DateCreation` datetime(6) NOT NULL,
    CONSTRAINT `PK_HotelPlanifGenerationLogs` PRIMARY KEY (`IdHotelPlanifGenerationLog`),
    CONSTRAINT `FK_HotelPlanifGenerationLogs_Planifications`
        FOREIGN KEY (`IdHotelPlanification`)
            REFERENCES `HotelPlanifications` (`IdHotelPlanification`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

-- FK optionnelle sur allotments (SET NULL)
SET @col_exists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'HotelNightAllotments'
      AND COLUMN_NAME = 'IdHotelPlanification'
);
SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE `HotelNightAllotments` ADD `IdHotelPlanification` int NULL',
    'SELECT ''HotelNightAllotments.IdHotelPlanification déjà présent'' AS Info'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @fk_exists := (
    SELECT COUNT(*)
    FROM information_schema.TABLE_CONSTRAINTS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'HotelNightAllotments'
      AND CONSTRAINT_NAME = 'FK_HotelNightAllotments_Planifications'
);
SET @sql := IF(
    @fk_exists = 0,
    'ALTER TABLE `HotelNightAllotments` ADD CONSTRAINT `FK_HotelNightAllotments_Planifications` FOREIGN KEY (`IdHotelPlanification`) REFERENCES `HotelPlanifications` (`IdHotelPlanification`) ON DELETE SET NULL',
    'SELECT ''FK_HotelNightAllotments_Planifications déjà présente'' AS Info'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Indexes (ignore error if already exist — MySQL lacks IF NOT EXISTS on older versions;
-- CREATE INDEX without IF NOT EXISTS may fail on re-run; use information_schema guards)
SET @idx := (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'HotelPlanifications'
      AND INDEX_NAME = 'IX_HotelPlanifications_IdSociete'
);
SET @sql := IF(@idx = 0,
    'CREATE INDEX `IX_HotelPlanifications_IdSociete` ON `HotelPlanifications` (`IdSociete`)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @idx := (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'HotelPlanifications'
      AND INDEX_NAME = 'IX_HotelPlanifications_IdHotel'
);
SET @sql := IF(@idx = 0,
    'CREATE INDEX `IX_HotelPlanifications_IdHotel` ON `HotelPlanifications` (`IdHotel`)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @idx := (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'HotelPlanificationLignes'
      AND INDEX_NAME = 'IX_HotelPlanificationLignes_Planif_RoomType_UQ'
);
SET @sql := IF(@idx = 0,
    'CREATE UNIQUE INDEX `IX_HotelPlanificationLignes_Planif_RoomType_UQ` ON `HotelPlanificationLignes` (`IdHotelPlanification`, `IdHotelRoomType`)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @idx := (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'HotelPlanificationLignes'
      AND INDEX_NAME = 'IX_HotelPlanificationLignes_IdPlanification'
);
SET @sql := IF(@idx = 0,
    'CREATE INDEX `IX_HotelPlanificationLignes_IdPlanification` ON `HotelPlanificationLignes` (`IdHotelPlanification`)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @idx := (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'HotelPlanifGenerationLogs'
      AND INDEX_NAME = 'IX_HotelPlanifGenerationLogs_IdPlanification'
);
SET @sql := IF(@idx = 0,
    'CREATE INDEX `IX_HotelPlanifGenerationLogs_IdPlanification` ON `HotelPlanifGenerationLogs` (`IdHotelPlanification`)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @idx := (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'HotelNightAllotments'
      AND INDEX_NAME = 'IX_HotelNightAllotments_IdHotelPlanification'
);
SET @sql := IF(@idx = 0,
    'CREATE INDEX `IX_HotelNightAllotments_IdHotelPlanification` ON `HotelNightAllotments` (`IdHotelPlanification`)',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET FOREIGN_KEY_CHECKS = 1;
SELECT 'production_hotel_phase7a_planification.sql appliqué' AS Info;
