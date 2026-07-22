-- =====================================================
-- Script SQL pour ajouter le champ Statut à la table ClientUsages
-- Date: 2025-01-04
-- Description: Ajoute une colonne Statut (TINYINT(1)) avec valeur par défaut TRUE
-- =====================================================

SET @OLD_FOREIGN_KEY_CHECKS = @@FOREIGN_KEY_CHECKS;
SET FOREIGN_KEY_CHECKS = 0;

-- Vérifier si la colonne Statut existe déjà
SET @column_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = DATABASE() 
    AND TABLE_NAME = 'ClientUsages' 
    AND COLUMN_NAME = 'Statut'
);

-- Ajouter la colonne Statut si elle n'existe pas
SET @sql = IF(@column_exists = 0,
    'ALTER TABLE `ClientUsages` 
     ADD COLUMN `Statut` TINYINT(1) NOT NULL DEFAULT 1 
     COMMENT ''Statut de la relation Client-Usage (1 = actif, 0 = inactif)'' 
     AFTER `DateAttribution`',
    'SELECT ''La colonne Statut existe déjà dans la table ClientUsages.'' AS Message'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Mettre à jour les valeurs NULL existantes (si la colonne était nullable avant)
UPDATE `ClientUsages` 
SET `Statut` = 1 
WHERE `Statut` IS NULL;

-- Vérification finale
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT,
    COLUMN_COMMENT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
AND TABLE_NAME = 'ClientUsages'
AND COLUMN_NAME = 'Statut';

-- Afficher le nombre de lignes mises à jour
SELECT 
    COUNT(*) AS TotalClientUsages,
    SUM(CASE WHEN Statut = 1 THEN 1 ELSE 0 END) AS Actifs,
    SUM(CASE WHEN Statut = 0 THEN 1 ELSE 0 END) AS Inactifs
FROM `ClientUsages`;

SET FOREIGN_KEY_CHECKS = @OLD_FOREIGN_KEY_CHECKS;

-- =====================================================
-- Script terminé avec succès
-- =====================================================
