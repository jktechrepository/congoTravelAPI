-- =============================================================================
-- Production : PenaliteReaffectation → PenaliteReaffectationPourcentage
-- Migration EF équivalente : 20260530121511_ConfigSocietePenalitePourcentage
-- =============================================================================

SET @db := DATABASE();

SET @col_old := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'ConfigSocietes' AND COLUMN_NAME = 'PenaliteReaffectation'
);

SET @col_new := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'ConfigSocietes' AND COLUMN_NAME = 'PenaliteReaffectationPourcentage'
);

SET @sql_rename := IF(
    @col_old > 0 AND @col_new = 0,
    'ALTER TABLE `ConfigSocietes` CHANGE COLUMN `PenaliteReaffectation` `PenaliteReaffectationPourcentage` decimal(18,2) NOT NULL DEFAULT 0.00',
    'SELECT ''Rename PenaliteReaffectation déjà appliqué ou colonne absente'' AS Info'
);

PREPARE stmt FROM @sql_rename;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

UPDATE `ConfigSocietes`
SET `PenaliteReaffectationPourcentage` = 0
WHERE `PenaliteReaffectationPourcentage` <> 0;

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260530121511_ConfigSocietePenalitePourcentage', '6.0.25');

SELECT IdSociete, PenaliteReaffectationPourcentage, JoursAvanceMaxReservation
FROM ConfigSocietes
ORDER BY IdSociete;
