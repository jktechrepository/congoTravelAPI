-- =============================================================================
-- CongoTravel — StorageKey photos S3 (DEV)
-- Migration EF : 20260826124242_AddPhotoStorageKeyS3
-- Stamp (si script SQL manuel) :
--   INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
--   VALUES ('20260826124242_AddPhotoStorageKeyS3', '6.0.25');
-- =============================================================================
-- Ajoute StorageKey nullable + PhotoData nullable sur les 4 tables photo.
-- Préfixe objets : congotravel/photos/{vehicules|evenement-sessions|restaurants|sites-touristiques}/...
-- =============================================================================

SET @db := DATABASE();

-- PhotoVehicules
SET @col := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'PhotoVehicules' AND COLUMN_NAME = 'StorageKey'
);
SET @sql := IF(@col = 0,
    'ALTER TABLE `PhotoVehicules` ADD COLUMN `StorageKey` varchar(500) NULL',
    'SELECT ''PhotoVehicules.StorageKey déjà présent'' AS Info');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := 'ALTER TABLE `PhotoVehicules` MODIFY COLUMN `PhotoData` mediumblob NULL';
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- EvenementSessionPhotos
SET @col := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'EvenementSessionPhotos' AND COLUMN_NAME = 'StorageKey'
);
SET @sql := IF(@col = 0,
    'ALTER TABLE `EvenementSessionPhotos` ADD COLUMN `StorageKey` varchar(500) NULL',
    'SELECT ''EvenementSessionPhotos.StorageKey déjà présent'' AS Info');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := 'ALTER TABLE `EvenementSessionPhotos` MODIFY COLUMN `PhotoData` mediumblob NULL';
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- RestaurantPhotos
SET @col := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'RestaurantPhotos' AND COLUMN_NAME = 'StorageKey'
);
SET @sql := IF(@col = 0,
    'ALTER TABLE `RestaurantPhotos` ADD COLUMN `StorageKey` varchar(500) NULL',
    'SELECT ''RestaurantPhotos.StorageKey déjà présent'' AS Info');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := 'ALTER TABLE `RestaurantPhotos` MODIFY COLUMN `PhotoData` mediumblob NULL';
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- SiteTouristiqueLieuPhotos
SET @col := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'SiteTouristiqueLieuPhotos' AND COLUMN_NAME = 'StorageKey'
);
SET @sql := IF(@col = 0,
    'ALTER TABLE `SiteTouristiqueLieuPhotos` ADD COLUMN `StorageKey` varchar(500) NULL',
    'SELECT ''SiteTouristiqueLieuPhotos.StorageKey déjà présent'' AS Info');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := 'ALTER TABLE `SiteTouristiqueLieuPhotos` MODIFY COLUMN `PhotoData` mediumblob NULL';
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
