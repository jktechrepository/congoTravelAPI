-- Ajoute les flags d'activités société sur ConfigSocietes (défaut true).
-- Migration EF : AddConfigSocieteActiviteFlags

SET @db := DATABASE();

-- ActiviteTransport
SET @col_exists := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'ConfigSocietes' AND COLUMN_NAME = 'ActiviteTransport'
);
SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE `ConfigSocietes` ADD COLUMN `ActiviteTransport` tinyint(1) NOT NULL DEFAULT 1 AFTER `ReservationIsActif`',
    'SELECT ''Colonne ActiviteTransport déjà présente — ignoré'' AS Info'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- ActiviteEvenement
SET @col_exists := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'ConfigSocietes' AND COLUMN_NAME = 'ActiviteEvenement'
);
SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE `ConfigSocietes` ADD COLUMN `ActiviteEvenement` tinyint(1) NOT NULL DEFAULT 1 AFTER `ActiviteTransport`',
    'SELECT ''Colonne ActiviteEvenement déjà présente — ignoré'' AS Info'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- ActiviteSiteTouristique
SET @col_exists := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'ConfigSocietes' AND COLUMN_NAME = 'ActiviteSiteTouristique'
);
SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE `ConfigSocietes` ADD COLUMN `ActiviteSiteTouristique` tinyint(1) NOT NULL DEFAULT 1 AFTER `ActiviteEvenement`',
    'SELECT ''Colonne ActiviteSiteTouristique déjà présente — ignoré'' AS Info'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- ActiviteRestaurant
SET @col_exists := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'ConfigSocietes' AND COLUMN_NAME = 'ActiviteRestaurant'
);
SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE `ConfigSocietes` ADD COLUMN `ActiviteRestaurant` tinyint(1) NOT NULL DEFAULT 1 AFTER `ActiviteSiteTouristique`',
    'SELECT ''Colonne ActiviteRestaurant déjà présente — ignoré'' AS Info'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
