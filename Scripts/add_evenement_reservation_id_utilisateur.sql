-- Ajoute IdUtilisateur sur EvenementReservations (acheteur JWT / SignalR FlexPay).
-- Migration EF : AddEvenementReservationIdUtilisateur

SET @db := DATABASE();

SET @col_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db
      AND TABLE_NAME = 'EvenementReservations'
      AND COLUMN_NAME = 'IdUtilisateur'
);

SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE `EvenementReservations`
        ADD COLUMN `IdUtilisateur` int NULL,
        ADD INDEX `IX_EvenementReservations_IdUtilisateur` (`IdUtilisateur`)',
    'SELECT ''Colonne IdUtilisateur déjà présente — ignoré'' AS Info'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
