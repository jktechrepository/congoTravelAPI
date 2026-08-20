-- =============================================================================
-- PRODUCTION — Migrations FlexPay PayOut + ConfigSociete (idempotent MySQL 8+)
-- =============================================================================
--
-- Fichier : Scripts/production_payout_reversement_migrations.sql
--
-- EXÉCUTION :
--   1. Se connecter à la base cible : USE nom_de_votre_base;
--   2. Exécuter ce script en entier (client MySQL, DBeaver, phpMyAdmin, etc.)
--   3. Vérifier la section « Vérification » en fin de script (6 lignes attendues)
--   4. Redémarrer l'API CongoTravel (seeder permissions ReversementSite.*)
--   5. Appliquer la section « Post-déploiement » manuellement si besoin métier
--
-- Alternative EF Core (même résultat) :
--   dotnet ef database update --context CongoTravelDbContext
--
-- Déploiement partiel (déjà PayOut en prod, seulement ConfigSociete) :
--   → Scripts/production_config_societe_incremental.sql
--
-- Déploiement partiel (uniquement MontAddPaieElectronique) :
--   → Scripts/production_mont_add_paie_electronique_only.sql
--
-- Migrations EF couvertes :
--   20260618112928_SiteNumeroMobileMoney
--   20260618124839_ReversementSiteFlexPayPayOut
--   20260618133404_ReversementAutoPaiementElectronique
--   20260618134551_PourcentageReversementSiteConfig
--   20260618135910_FraisPlateformeConfig
--   20260618171505_MontAddPaieElectroniqueConfig
--
-- Prérequis : MySQL 8+, tables Sites, Societes, ConfigSocietes existantes.
-- Permissions ReversementSite.* : appliquées au redémarrage API (PermissionSeeder).
-- =============================================================================

SET @db := DATABASE();

-- -----------------------------------------------------------------------------
-- Bloc A — Sites.NumeroMobileMoney
-- -----------------------------------------------------------------------------
SET @col_numero_mm := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'Sites' AND COLUMN_NAME = 'NumeroMobileMoney'
);

SET @sql_a := IF(
    @col_numero_mm = 0,
    'ALTER TABLE `Sites` ADD COLUMN `NumeroMobileMoney` varchar(30) CHARACTER SET utf8mb4 NULL',
    'SELECT ''Sites.NumeroMobileMoney déjà présent'' AS Info'
);

PREPARE stmt FROM @sql_a;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260618112928_SiteNumeroMobileMoney', '6.0.25');

-- -----------------------------------------------------------------------------
-- Bloc B — Table ReversementsSite
-- -----------------------------------------------------------------------------
SET @tbl_reversements := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'ReversementsSite'
);

SET @sql_b := IF(
    @tbl_reversements = 0,
    'CREATE TABLE `ReversementsSite` (
        `IdReversementSite` int NOT NULL AUTO_INCREMENT,
        `IdSite` int NOT NULL,
        `IdSociete` int NOT NULL,
        `IdUtilisateur` int NOT NULL,
        `NumeroMobileMoney` varchar(30) CHARACTER SET utf8mb4 NOT NULL,
        `Montant` decimal(18,2) NOT NULL,
        `CodeDevise` varchar(10) CHARACTER SET utf8mb4 NOT NULL,
        `Reference` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
        `OrderNumber` varchar(100) CHARACTER SET utf8mb4 NULL,
        `ProviderReference` varchar(100) CHARACTER SET utf8mb4 NULL,
        `CodeMarchand` varchar(100) CHARACTER SET utf8mb4 NULL,
        `Statut` int NOT NULL,
        `CodeFlexPay` varchar(10) CHARACTER SET utf8mb4 NULL,
        `MessageFlexPay` varchar(500) CHARACTER SET utf8mb4 NULL,
        `Channel` varchar(50) CHARACTER SET utf8mb4 NULL,
        `Motif` varchar(500) CHARACTER SET utf8mb4 NULL,
        `DateCreation` datetime(6) NOT NULL,
        `DateCallback` datetime(6) NULL,
        CONSTRAINT `PK_ReversementsSite` PRIMARY KEY (`IdReversementSite`),
        CONSTRAINT `FK_ReversementsSite_Sites_IdSite` FOREIGN KEY (`IdSite`) REFERENCES `Sites` (`IdSite`) ON DELETE RESTRICT,
        CONSTRAINT `FK_ReversementsSite_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT
    ) CHARACTER SET=utf8mb4',
    'SELECT ''Table ReversementsSite déjà présente'' AS Info'
);

PREPARE stmt FROM @sql_b;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @idx_order := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'ReversementsSite' AND INDEX_NAME = 'IX_ReversementSite_OrderNumber'
);

SET @sql_idx_order := IF(
    @idx_order = 0,
    'CREATE UNIQUE INDEX `IX_ReversementSite_OrderNumber` ON `ReversementsSite` (`OrderNumber`)',
    'SELECT ''Index IX_ReversementSite_OrderNumber déjà présent'' AS Info'
);

PREPARE stmt FROM @sql_idx_order;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @idx_site_date := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'ReversementsSite' AND INDEX_NAME = 'IX_ReversementSite_Societe_Site_Date'
);

SET @sql_idx_site_date := IF(
    @idx_site_date = 0,
    'CREATE INDEX `IX_ReversementSite_Societe_Site_Date` ON `ReversementsSite` (`IdSociete`, `IdSite`, `DateCreation`)',
    'SELECT ''Index IX_ReversementSite_Societe_Site_Date déjà présent'' AS Info'
);

PREPARE stmt FROM @sql_idx_site_date;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @idx_site_fk := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'ReversementsSite' AND INDEX_NAME = 'IX_ReversementsSite_IdSite'
);

SET @sql_idx_site_fk := IF(
    @idx_site_fk = 0,
    'CREATE INDEX `IX_ReversementsSite_IdSite` ON `ReversementsSite` (`IdSite`)',
    'SELECT ''Index IX_ReversementsSite_IdSite déjà présent'' AS Info'
);

PREPARE stmt FROM @sql_idx_site_fk;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260618124839_ReversementSiteFlexPayPayOut', '6.0.25');

-- -----------------------------------------------------------------------------
-- Bloc C — Colonnes auto-reversement sur ReversementsSite + ConfigSocietes
-- -----------------------------------------------------------------------------
SET @col_id_paiement := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'ReversementsSite' AND COLUMN_NAME = 'IdPaiement'
);

SET @sql_c1 := IF(
    @col_id_paiement = 0,
    'ALTER TABLE `ReversementsSite`
        ADD COLUMN `IdPaiement` int NULL,
        ADD COLUMN `IdReservation` int NULL,
        ADD COLUMN `Origine` varchar(30) CHARACTER SET utf8mb4 NOT NULL DEFAULT ''Manuel''',
    'SELECT ''Colonnes IdPaiement/IdReservation/Origine déjà présentes'' AS Info'
);

PREPARE stmt FROM @sql_c1;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_auto_rev := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'ConfigSocietes' AND COLUMN_NAME = 'AutoReversementPaiementElectronique'
);

SET @sql_c2 := IF(
    @col_auto_rev = 0,
    'ALTER TABLE `ConfigSocietes` ADD COLUMN `AutoReversementPaiementElectronique` tinyint(1) NOT NULL DEFAULT 0',
    'SELECT ''ConfigSocietes.AutoReversementPaiementElectronique déjà présent'' AS Info'
);

PREPARE stmt FROM @sql_c2;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @idx_id_paiement := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'ReversementsSite' AND INDEX_NAME = 'IX_ReversementSite_IdPaiement'
);

SET @sql_idx_paiement := IF(
    @idx_id_paiement = 0,
    'CREATE UNIQUE INDEX `IX_ReversementSite_IdPaiement` ON `ReversementsSite` (`IdPaiement`)',
    'SELECT ''Index IX_ReversementSite_IdPaiement déjà présent'' AS Info'
);

PREPARE stmt FROM @sql_idx_paiement;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260618133404_ReversementAutoPaiementElectronique', '6.0.25');

-- -----------------------------------------------------------------------------
-- Bloc C2 — Idempotence multi-module (ModulePaiement + IdPaiementSource)
-- -----------------------------------------------------------------------------
SET @col_module := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'ReversementsSite' AND COLUMN_NAME = 'ModulePaiement'
);

SET @sql_c2_module := IF(
    @col_module = 0,
    'ALTER TABLE `ReversementsSite`
        ADD COLUMN `ModulePaiement` varchar(30) CHARACTER SET utf8mb4 NULL,
        ADD COLUMN `IdPaiementSource` int NULL',
    'SELECT ''Colonnes ModulePaiement/IdPaiementSource déjà présentes'' AS Info'
);

PREPARE stmt FROM @sql_c2_module;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

UPDATE `ReversementsSite`
SET `ModulePaiement` = 'Transport',
    `IdPaiementSource` = `IdPaiement`
WHERE `IdPaiement` IS NOT NULL
  AND (`ModulePaiement` IS NULL OR `IdPaiementSource` IS NULL);

SET @idx_module_source := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'ReversementsSite' AND INDEX_NAME = 'IX_ReversementSite_Module_IdPaiementSource'
);

SET @sql_c2_idx := IF(
    @idx_module_source = 0,
    'CREATE UNIQUE INDEX `IX_ReversementSite_Module_IdPaiementSource` ON `ReversementsSite` (`ModulePaiement`, `IdPaiementSource`)',
    'SELECT ''Index IX_ReversementSite_Module_IdPaiementSource déjà présent'' AS Info'
);

PREPARE stmt FROM @sql_c2_idx;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260820081000_AddReversementSiteModulePaiementSource', '6.0.25');

-- -----------------------------------------------------------------------------
-- Bloc D — PourcentageReversementSite
-- -----------------------------------------------------------------------------
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

-- -----------------------------------------------------------------------------
-- Bloc F — FraisPlateforme + CodeDeviseFraisPlateforme
-- -----------------------------------------------------------------------------
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

-- -----------------------------------------------------------------------------
-- Bloc G — MontAddPaieElectronique + CodeDeviseMontAddPaieElectronique
-- -----------------------------------------------------------------------------
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

-- -----------------------------------------------------------------------------
-- Vérification
-- -----------------------------------------------------------------------------
SELECT MigrationId, ProductVersion
FROM `__EFMigrationsHistory`
WHERE MigrationId IN (
    '20260618112928_SiteNumeroMobileMoney',
    '20260618124839_ReversementSiteFlexPayPayOut',
    '20260618133404_ReversementAutoPaiementElectronique',
    '20260618134551_PourcentageReversementSiteConfig',
    '20260618135910_FraisPlateformeConfig',
    '20260618171505_MontAddPaieElectroniqueConfig',
    '20260820081000_AddReversementSiteModulePaiementSource'
)
ORDER BY MigrationId;

-- -----------------------------------------------------------------------------
-- Post-déploiement (exemples — exécuter manuellement après validation métier)
-- -----------------------------------------------------------------------------
-- Activer reversement auto 95 % + frais fixe 500 CDF pour société 60 :
-- UPDATE ConfigSocietes
-- SET AutoReversementPaiementElectronique = 1,
--     PourcentageReversementSite = 95.00,
--     FraisPlateforme = 500.00,
--     CodeDeviseFraisPlateforme = 'CDF'
-- WHERE IdSociete = 60;
--
-- Renseigner le wallet Mobile Money du site 71 :
-- UPDATE Sites SET NumeroMobileMoney = '243900000000' WHERE IdSite = 71;
--
-- Supplément paiement électronique 500 CDF / place pour société 60 :
-- UPDATE ConfigSocietes
-- SET MontAddPaieElectronique = 500.00,
--     CodeDeviseMontAddPaieElectronique = 'CDF'
-- WHERE IdSociete = 60;
