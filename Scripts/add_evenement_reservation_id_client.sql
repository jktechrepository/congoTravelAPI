-- Ajoute IdClient sur EvenementReservations (client lié à l'acheteur JWT).
-- Migration EF : AddEvenementReservationIdClient
-- Idempotent : no-op si table absente ou colonne déjà présente.

SET @db := DATABASE();

SET @table_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = @db
      AND LOWER(TABLE_NAME) = LOWER('EvenementReservations')
);

SET @col_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db
      AND LOWER(TABLE_NAME) = LOWER('EvenementReservations')
      AND LOWER(COLUMN_NAME) = LOWER('IdClient')
);

SET @sql := IF(
    @table_exists = 0,
    'SELECT ''Table EvenementReservations absente — IdClient ignoré'' AS Info',
    IF(
        @col_exists = 0,
        'ALTER TABLE `EvenementReservations`
            ADD COLUMN `IdClient` int NULL,
            ADD INDEX `IX_EvenementReservations_IdClient` (`IdClient`)',
        'SELECT ''Colonne IdClient déjà présente — ignoré'' AS Info'
    )
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
