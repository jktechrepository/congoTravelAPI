-- Ajoute ReservationIsActif sur ConfigSocietes (défaut true).
-- Migration EF : AddSocieteReservationIsActif

SET @db := DATABASE();

SET @col_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db
      AND TABLE_NAME = 'ConfigSocietes'
      AND COLUMN_NAME = 'ReservationIsActif'
);

SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE `ConfigSocietes`
        ADD COLUMN `ReservationIsActif` tinyint(1) NOT NULL DEFAULT 1
        AFTER `ReaffectationActive`',
    'SELECT ''Colonne ReservationIsActif déjà présente — ignoré'' AS Info'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
