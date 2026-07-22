-- =====================================================
-- Script SQL pour ajouter le champ Description à la table Cabines
-- Date: 2025-01-04
-- Description: Ajoute une colonne Description (VARCHAR(1000)) nullable
-- =====================================================

SET @OLD_FOREIGN_KEY_CHECKS = @@FOREIGN_KEY_CHECKS;
SET FOREIGN_KEY_CHECKS = 0;

-- Vérifier si la colonne Description existe déjà
SET @column_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = DATABASE() 
    AND TABLE_NAME = 'Cabines' 
    AND COLUMN_NAME = 'Description'
);

-- Ajouter la colonne Description si elle n'existe pas
SET @sql = IF(@column_exists = 0,
    'ALTER TABLE `Cabines` 
     ADD COLUMN `Description` VARCHAR(1000) NULL 
     COMMENT ''Description de la cabine'' 
     AFTER `Adresse`',
    'SELECT ''La colonne Description existe déjà dans la table Cabines.'' AS Message'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Vérification finale
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE,
    COLUMN_DEFAULT,
    COLUMN_COMMENT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
AND TABLE_NAME = 'Cabines'
AND COLUMN_NAME = 'Description';

-- Afficher le nombre de cabines
SELECT 
    COUNT(*) AS TotalCabines,
    COUNT(Description) AS CabinesAvecDescription,
    COUNT(*) - COUNT(Description) AS CabinesSansDescription
FROM `Cabines`;

SET FOREIGN_KEY_CHECKS = @OLD_FOREIGN_KEY_CHECKS;

-- =====================================================
-- Script terminé avec succès
-- =====================================================
