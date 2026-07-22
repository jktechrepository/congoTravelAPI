-- =============================================================================
-- PRODUCTION — Migrations incrémentales ConfigSociete (reversement + supplément)
-- =============================================================================
--
-- Migrations EF :
--   20260618134551_PourcentageReversementSiteConfig
--   20260618135910_FraisPlateformeConfig
--   20260618171505_MontAddPaieElectroniqueConfig
--
-- Quand l'utiliser :
--   - Table ReversementsSite et PayOut déjà en prod (blocs A–C appliqués)
--   - Il manque les champs ConfigSociete pour % reversement, frais plateforme, supplément MM/carte
--
-- EXÉCUTION :
--   USE nom_de_votre_base;
--   Exécuter ce script en entier (idempotent)
--
-- Script complet depuis zéro :
--   → Scripts/production_payout_reversement_migrations.sql
-- =============================================================================

SET @db := DATABASE();

-- Bloc D — PourcentageReversementSite
SET @col_pct := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'ConfigSocietes' AND COLUMN_NAME = 'PourcentageReversementSite'
);

SET @sql_d := IF(
    @col_pct = 0,
    'ALTER TABLE `ConfigSocietes` ADD COLUMN `PourcentageReversementSite` decimal(18,2) NOT NULL DEFAULT 100.00',
    'SELECT ''ConfigSocietes.PourcentageReversementSite déjà présent'' AS Info'
);

PREPARE stmt FROM @sql_d;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

UPDATE `ConfigSocietes`
SET `PourcentageReversementSite` = 100.00
WHERE `PourcentageReversementSite` IS NULL OR `PourcentageReversementSite` < 0 OR `PourcentageReversementSite` > 100;

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260618134551_PourcentageReversementSiteConfig', '6.0.25');

-- Bloc F — FraisPlateforme
SET @col_frais := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'ConfigSocietes' AND COLUMN_NAME = 'FraisPlateforme'
);

SET @sql_f := IF(
    @col_frais = 0,
    'ALTER TABLE `ConfigSocietes` ADD COLUMN `FraisPlateforme` decimal(18,2) NOT NULL DEFAULT 0.00',
    'SELECT ''ConfigSocietes.FraisPlateforme déjà présent'' AS Info'
);

PREPARE stmt FROM @sql_f;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_devise_frais := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'ConfigSocietes' AND COLUMN_NAME = 'CodeDeviseFraisPlateforme'
);

SET @sql_f2 := IF(
    @col_devise_frais = 0,
    'ALTER TABLE `ConfigSocietes` ADD COLUMN `CodeDeviseFraisPlateforme` varchar(3) CHARACTER SET utf8mb4 NULL',
    'SELECT ''ConfigSocietes.CodeDeviseFraisPlateforme déjà présent'' AS Info'
);

PREPARE stmt FROM @sql_f2;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

UPDATE `ConfigSocietes`
SET `FraisPlateforme` = 0.00
WHERE `FraisPlateforme` IS NULL OR `FraisPlateforme` < 0;

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260618135910_FraisPlateformeConfig', '6.0.25');

-- Bloc G — MontAddPaieElectronique
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
WHERE MigrationId IN (
    '20260618134551_PourcentageReversementSiteConfig',
    '20260618135910_FraisPlateformeConfig',
    '20260618171505_MontAddPaieElectroniqueConfig'
)
ORDER BY MigrationId;

-- Post-déploiement (exemples — exécuter manuellement)
-- UPDATE ConfigSocietes
-- SET PourcentageReversementSite = 95.00,
--     FraisPlateforme = 500.00,
--     CodeDeviseFraisPlateforme = 'CDF',
--     MontAddPaieElectronique = 500.00,
--     CodeDeviseMontAddPaieElectronique = 'CDF',
--     AutoReversementPaiementElectronique = 1
-- WHERE IdSociete = 60;
