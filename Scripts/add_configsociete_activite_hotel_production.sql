-- Ajoute le flag d'activité Hôtel sur ConfigSocietes (défaut true).
-- Migration EF : AddConfigSocieteActiviteHotel

SET @db := DATABASE();

-- ActiviteHotel
SET @col_exists := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'ConfigSocietes' AND COLUMN_NAME = 'ActiviteHotel'
);
SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE `ConfigSocietes` ADD COLUMN `ActiviteHotel` tinyint(1) NOT NULL DEFAULT 1 AFTER `ActiviteRestaurant`',
    'SELECT ''Colonne ActiviteHotel déjà présente — ignoré'' AS Info'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
