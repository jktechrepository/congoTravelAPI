-- Ajoute IdClient sur SiteTouristiqueReservations (client lié à l'acheteur JWT).
-- Migration EF : AddSiteTouristiqueAndRestaurantReservationIdClient (partie ST)
-- Idempotent : no-op si table absente (vertical non déployé) ou colonne déjà présente.

SET @db := DATABASE();

SET @table_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = @db
      AND LOWER(TABLE_NAME) = LOWER('SiteTouristiqueReservations')
);

SET @col_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db
      AND LOWER(TABLE_NAME) = LOWER('SiteTouristiqueReservations')
      AND LOWER(COLUMN_NAME) = LOWER('IdClient')
);

SET @sql := IF(
    @table_exists = 0,
    'SELECT ''Table SiteTouristiqueReservations absente — IdClient ignoré (déployer production_site_touristique_ticketing_v1.sql d''''abord)'' AS Info',
    IF(
        @col_exists = 0,
        'ALTER TABLE `SiteTouristiqueReservations`
            ADD COLUMN `IdClient` int NULL,
            ADD INDEX `IX_SiteTouristiqueReservations_IdClient` (`IdClient`)',
        'SELECT ''Colonne IdClient déjà présente — ignoré'' AS Info'
    )
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
