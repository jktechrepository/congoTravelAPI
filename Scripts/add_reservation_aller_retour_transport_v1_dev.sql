-- =============================================================================
-- CongoTravel — Réservation Aller-Retour Transport V1 (DEV)
-- Migration EF : 20260823154748_AddReservationAllerRetourTransportV1
-- =============================================================================
-- Prérequis :
--   1. Backup de la base de développement
--   2. Vérifier si déjà appliqué :
--        SELECT * FROM `__EFMigrationsHistory`
--          WHERE `MigrationId` = '20260823154748_AddReservationAllerRetourTransportV1';
--        SHOW TABLES LIKE 'ReservationsAllerRetour';
--
-- Exécution : MySQL / MariaDB sur la base de DEV (une fois).
-- Script idempotent : colonnes / table / index / FK / stamp EF déjà présents → ignorés.
-- =============================================================================

SET @db := DATABASE();
SET @migration_id := '20260823154748_AddReservationAllerRetourTransportV1';
SET @product_version := '6.0.25';

-- ---------------------------------------------------------------------------
-- 1) Table ReservationsAllerRetour
-- ---------------------------------------------------------------------------
SET @table_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = @db
      AND TABLE_NAME = 'ReservationsAllerRetour'
);

SET @sql := IF(
    @table_exists = 0,
    'CREATE TABLE `ReservationsAllerRetour` (
        `IdReservationAllerRetour` int NOT NULL AUTO_INCREMENT,
        `IdVoyageAller` int NOT NULL,
        `IdVoyageRetour` int NOT NULL,
        `IdReservationAller` int NULL,
        `IdReservationRetour` int NULL,
        `IdPaiement` int NULL,
        `IdCommandeReservationEnAttente` char(36) COLLATE ascii_general_ci NULL,
        `Statut` varchar(30) CHARACTER SET utf8mb4 NOT NULL,
        `IdSociete` int NOT NULL,
        `IdClient` int NOT NULL,
        `IdUtilisateur` int NOT NULL,
        `IdSite` int NULL,
        `Origine` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
        `DateCreation` datetime(6) NOT NULL,
        `DateModification` datetime(6) NULL,
        CONSTRAINT `PK_ReservationsAllerRetour` PRIMARY KEY (`IdReservationAllerRetour`)
    ) CHARACTER SET=utf8mb4',
    'SELECT ''Table ReservationsAllerRetour déjà présente — ignoré'' AS Info'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ---------------------------------------------------------------------------
-- 2) Index sur ReservationsAllerRetour
-- ---------------------------------------------------------------------------
SET @table_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = @db
      AND TABLE_NAME = 'ReservationsAllerRetour'
);

SET @idx_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = @db
      AND TABLE_NAME = 'ReservationsAllerRetour'
      AND INDEX_NAME = 'IX_ReservationsAllerRetour_IdSociete'
);

SET @sql := IF(
    @table_exists > 0 AND @idx_exists = 0,
    'CREATE INDEX `IX_ReservationsAllerRetour_IdSociete` ON `ReservationsAllerRetour` (`IdSociete`)',
    'SELECT ''Index IX_ReservationsAllerRetour_IdSociete déjà présent ou table absente — ignoré'' AS Info'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @idx_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = @db
      AND TABLE_NAME = 'ReservationsAllerRetour'
      AND INDEX_NAME = 'IX_ReservationsAllerRetour_Statut'
);

SET @sql := IF(
    @table_exists > 0 AND @idx_exists = 0,
    'CREATE INDEX `IX_ReservationsAllerRetour_Statut` ON `ReservationsAllerRetour` (`Statut`)',
    'SELECT ''Index IX_ReservationsAllerRetour_Statut déjà présent ou table absente — ignoré'' AS Info'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ---------------------------------------------------------------------------
-- 3) Colonnes Reservations
-- ---------------------------------------------------------------------------
SET @col_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db
      AND TABLE_NAME = 'Reservations'
      AND COLUMN_NAME = 'AllerRetourLeg'
);

SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE `Reservations` ADD COLUMN `AllerRetourLeg` int NULL',
    'SELECT ''Colonne Reservations.AllerRetourLeg déjà présente — ignoré'' AS Info'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db
      AND TABLE_NAME = 'Reservations'
      AND COLUMN_NAME = 'IdReservationAllerRetour'
);

SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE `Reservations` ADD COLUMN `IdReservationAllerRetour` int NULL',
    'SELECT ''Colonne Reservations.IdReservationAllerRetour déjà présente — ignoré'' AS Info'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ---------------------------------------------------------------------------
-- 4) Colonne Paiements.IdReservationAllerRetour
-- ---------------------------------------------------------------------------
SET @col_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db
      AND TABLE_NAME = 'Paiements'
      AND COLUMN_NAME = 'IdReservationAllerRetour'
);

SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE `Paiements` ADD COLUMN `IdReservationAllerRetour` int NULL',
    'SELECT ''Colonne Paiements.IdReservationAllerRetour déjà présente — ignoré'' AS Info'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ---------------------------------------------------------------------------
-- 5) Colonne CommandesReservationEnAttente.TypeCommande
-- ---------------------------------------------------------------------------
SET @col_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db
      AND TABLE_NAME = 'CommandesReservationEnAttente'
      AND COLUMN_NAME = 'TypeCommande'
);

SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE `CommandesReservationEnAttente`
        ADD COLUMN `TypeCommande` varchar(20) CHARACTER SET utf8mb4 NOT NULL DEFAULT ''Single''',
    'SELECT ''Colonne CommandesReservationEnAttente.TypeCommande déjà présente — ignoré'' AS Info'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ---------------------------------------------------------------------------
-- 6) Index Reservations.IdReservationAllerRetour
-- ---------------------------------------------------------------------------
SET @idx_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = @db
      AND TABLE_NAME = 'Reservations'
      AND INDEX_NAME = 'IX_Reservations_IdReservationAllerRetour'
);

SET @sql := IF(
    @idx_exists = 0,
    'CREATE INDEX `IX_Reservations_IdReservationAllerRetour` ON `Reservations` (`IdReservationAllerRetour`)',
    'SELECT ''Index IX_Reservations_IdReservationAllerRetour déjà présent — ignoré'' AS Info'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ---------------------------------------------------------------------------
-- 7) FK Reservations → ReservationsAllerRetour
-- ---------------------------------------------------------------------------
SET @fk_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
    WHERE TABLE_SCHEMA = @db
      AND TABLE_NAME = 'Reservations'
      AND CONSTRAINT_NAME = 'FK_Reservations_ReservationsAllerRetour_IdReservationAllerRetour'
      AND CONSTRAINT_TYPE = 'FOREIGN KEY'
);

SET @table_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = @db
      AND TABLE_NAME = 'ReservationsAllerRetour'
);

SET @sql := IF(
    @fk_exists = 0 AND @table_exists > 0,
    'ALTER TABLE `Reservations`
        ADD CONSTRAINT `FK_Reservations_ReservationsAllerRetour_IdReservationAllerRetour`
        FOREIGN KEY (`IdReservationAllerRetour`)
        REFERENCES `ReservationsAllerRetour` (`IdReservationAllerRetour`)
        ON DELETE RESTRICT',
    'SELECT ''FK Reservations→ReservationsAllerRetour déjà présente ou table absente — ignoré'' AS Info'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- ---------------------------------------------------------------------------
-- 8) Stamp historique EF (évite re-apply via dotnet ef database update)
-- ---------------------------------------------------------------------------
INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES (@migration_id, @product_version);

-- ---------------------------------------------------------------------------
-- 9) Vérifications post-apply
-- ---------------------------------------------------------------------------

SHOW COLUMNS FROM `Reservations` LIKE 'AllerRetourLeg';
SHOW COLUMNS FROM `Reservations` LIKE 'IdReservationAllerRetour';
SHOW COLUMNS FROM `Paiements` LIKE 'IdReservationAllerRetour';
SHOW COLUMNS FROM `CommandesReservationEnAttente` LIKE 'TypeCommande';
SHOW TABLES LIKE 'ReservationsAllerRetour';

SELECT 'Script add_reservation_aller_retour_transport_v1_dev.sql terminé.' AS Resultat;
