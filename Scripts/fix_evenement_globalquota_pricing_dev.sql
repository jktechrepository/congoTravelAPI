-- Fix GET /api/events/sessions : colonnes pricing manquantes sur EvenementSessionGlobalQuotas
-- (migration 20260703120104 jamais appliquée sur le schéma partiel).
-- Idempotent.

SET @col := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'EvenementSessionGlobalQuotas'
      AND COLUMN_NAME = 'PrixUnitaire'
);
SET @sql := IF(
    @col = 0,
    'ALTER TABLE `EvenementSessionGlobalQuotas` ADD `PrixUnitaire` decimal(18,2) NOT NULL DEFAULT 0.00',
    'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'EvenementSessionGlobalQuotas'
      AND COLUMN_NAME = 'CodeDevise'
);
SET @sql := IF(
    @col = 0,
    'ALTER TABLE `EvenementSessionGlobalQuotas` ADD `CodeDevise` char(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT ''CDF''',
    'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260703120104_EvenementSessionGlobalQuotaPricing', '6.0.25');

-- Devise paiement événement (si manquant)
SET @col := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'EvenementPayments'
      AND COLUMN_NAME = 'CodeDeviseTarif'
);
SET @sql := IF(
    @col = 0,
    'ALTER TABLE `EvenementPayments` ADD `CodeDeviseTarif` char(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT ''CDF'', ADD `MontantTarif` decimal(18,2) NOT NULL DEFAULT 0.00, ADD `TauxVersDevisePaiement` decimal(18,8) NOT NULL DEFAULT 1.00000000',
    'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

UPDATE `EvenementPayments`
SET `MontantTarif` = `Montant`,
    `CodeDeviseTarif` = `CodeDevise`,
    `TauxVersDevisePaiement` = 1
WHERE `MontantTarif` = 0;

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260717171358_EvenementPaymentDevisePaiement', '6.0.25');
