-- PoidsBagageParKiloOffert sur ConfigSocietes (kg offerts, défaut 0).
-- Idempotent + stamp EF.

SET @col := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'ConfigSocietes'
      AND COLUMN_NAME = 'PoidsBagageParKiloOffert'
);
SET @sql := IF(@col = 0,
    'ALTER TABLE `ConfigSocietes` ADD `PoidsBagageParKiloOffert` decimal(18,2) NOT NULL DEFAULT 0.0',
    'SELECT ''ConfigSocietes.PoidsBagageParKiloOffert déjà présent'' AS Info'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
SELECT '20260724174943_AddConfigSocietePoidsBagageParKiloOffert', '6.0.25'
FROM DUAL
WHERE NOT EXISTS (
    SELECT 1 FROM `__EFMigrationsHistory`
    WHERE `MigrationId` = '20260724174943_AddConfigSocietePoidsBagageParKiloOffert'
);
