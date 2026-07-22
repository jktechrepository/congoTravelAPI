-- =============================================================================
-- PRODUCTION — Migration incrémentale : MontAddPaieElectronique (ConfigSociete)
-- =============================================================================
--
-- Migration EF : 20260618171505_MontAddPaieElectroniqueConfig
--
-- Quand l'utiliser :
--   - PayOut / reversement déjà déployés en prod
--   - Il manque uniquement le supplément paiement électronique par place
--
-- EXÉCUTION :
--   USE nom_de_votre_base;
--   Exécuter ce script en entier (idempotent — relançable sans erreur)
--
-- Après exécution : redémarrer l'API (aucune permission EF supplémentaire).
-- L'enrichissement VoyageResponseDto est côté code API (pas de colonne Voyages).
-- =============================================================================

SET @db := DATABASE();

SET @col_mont_add := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'ConfigSocietes' AND COLUMN_NAME = 'MontAddPaieElectronique'
);

SET @sql_g := IF(
    @col_mont_add = 0,
    'ALTER TABLE `ConfigSocietes` ADD COLUMN `MontAddPaieElectronique` decimal(18,2) NOT NULL DEFAULT 0.00',
    'SELECT ''ConfigSocietes.MontAddPaieElectronique déjà présent'' AS Info'
);

PREPARE stmt FROM @sql_g;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_devise_mont_add := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'ConfigSocietes' AND COLUMN_NAME = 'CodeDeviseMontAddPaieElectronique'
);

SET @sql_g2 := IF(
    @col_devise_mont_add = 0,
    'ALTER TABLE `ConfigSocietes` ADD COLUMN `CodeDeviseMontAddPaieElectronique` varchar(3) CHARACTER SET utf8mb4 NULL',
    'SELECT ''ConfigSocietes.CodeDeviseMontAddPaieElectronique déjà présent'' AS Info'
);

PREPARE stmt FROM @sql_g2;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

UPDATE `ConfigSocietes`
SET `MontAddPaieElectronique` = 0.00
WHERE `MontAddPaieElectronique` IS NULL OR `MontAddPaieElectronique` < 0;

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260618171505_MontAddPaieElectroniqueConfig', '6.0.25');

-- Vérification
SELECT MigrationId, ProductVersion
FROM `__EFMigrationsHistory`
WHERE MigrationId = '20260618171505_MontAddPaieElectroniqueConfig';

SELECT COLUMN_NAME, COLUMN_TYPE, IS_NULLABLE, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = @db
  AND TABLE_NAME = 'ConfigSocietes'
  AND COLUMN_NAME IN ('MontAddPaieElectronique', 'CodeDeviseMontAddPaieElectronique')
ORDER BY COLUMN_NAME;

-- Post-déploiement (exemple — exécuter manuellement)
-- UPDATE ConfigSocietes
-- SET MontAddPaieElectronique = 500.00,
--     CodeDeviseMontAddPaieElectronique = 'CDF'
-- WHERE IdSociete = 60;
