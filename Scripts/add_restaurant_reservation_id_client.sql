-- Ajoute IdClient sur RestaurantReservations (client lié à l'acheteur JWT).
-- Migration EF : AddSiteTouristiqueAndRestaurantReservationIdClient (partie Restaurant)
-- Idempotent : no-op si table absente (vertical non déployé) ou colonne déjà présente.

SET @db := DATABASE();

SET @table_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = @db
      AND LOWER(TABLE_NAME) = LOWER('RestaurantReservations')
);

SET @col_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db
      AND LOWER(TABLE_NAME) = LOWER('RestaurantReservations')
      AND LOWER(COLUMN_NAME) = LOWER('IdClient')
);

SET @sql := IF(
    @table_exists = 0,
    'SELECT ''Table RestaurantReservations absente — IdClient ignoré (déployer production_restaurant_phase2_reservations.sql d''''abord)'' AS Info',
    IF(
        @col_exists = 0,
        'ALTER TABLE `RestaurantReservations`
            ADD COLUMN `IdClient` int NULL,
            ADD INDEX `IX_RestaurantReservations_IdClient` (`IdClient`)',
        'SELECT ''Colonne IdClient déjà présente — ignoré'' AS Info'
    )
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
