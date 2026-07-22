-- =============================================================================
-- PRODUCTION — Rattrapage migrations juin 2026 (API déployée, schéma en retard)
-- =============================================================================
--
-- Erreur typique sans ce script :
--   Unknown column 'c.CodeDeviseMontAddPaieElectronique' in 'SELECT'
--
-- Migrations EF couvertes (idempotent) :
--   20260608101418_OrigineOperationReservationPaiement
--   20260615121938_DestinationSocieteVillesUnique (si pas de doublons)
--   20260618134551_PourcentageReversementSiteConfig
--   20260618135910_FraisPlateformeConfig
--   20260618171505_MontAddPaieElectroniqueConfig  ← corrige l'erreur ci-dessus
--   20260619134037_ClientAdresseClientOptional
--
-- NON inclus ici (script dédié si FlexPay PayOut pas encore en prod) :
--   20260618112928_SiteNumeroMobileMoney
--   20260618124839_ReversementSiteFlexPayPayOut
--   20260618133404_ReversementAutoPaiementElectronique
--   → Scripts/production_payout_reversement_migrations.sql
--
-- EXÉCUTION :
--   1. USE nom_de_votre_base;
--   2. Sauvegarde recommandée
--   3. Exécuter la section « Diagnostic »
--   4. Exécuter la section « Application » en entier
--   5. Si ReversementsSite manquante : exécuter production_payout_reversement_migrations.sql
--   6. Redémarrer l'API
-- =============================================================================

SET @db := DATABASE();

-- =============================================================================
-- DIAGNOSTIC — migrations EF attendues vs appliquées
-- =============================================================================
SELECT m.MigrationId,
       CASE WHEN h.MigrationId IS NOT NULL THEN 'OK' ELSE 'MANQUANT' END AS Statut
FROM (
    SELECT '20260608101418_OrigineOperationReservationPaiement' AS MigrationId
    UNION ALL SELECT '20260615121938_DestinationSocieteVillesUnique'
    UNION ALL SELECT '20260618112928_SiteNumeroMobileMoney'
    UNION ALL SELECT '20260618124839_ReversementSiteFlexPayPayOut'
    UNION ALL SELECT '20260618133404_ReversementAutoPaiementElectronique'
    UNION ALL SELECT '20260618134551_PourcentageReversementSiteConfig'
    UNION ALL SELECT '20260618135910_FraisPlateformeConfig'
    UNION ALL SELECT '20260618171505_MontAddPaieElectroniqueConfig'
    UNION ALL SELECT '20260619134037_ClientAdresseClientOptional'
) m
LEFT JOIN `__EFMigrationsHistory` h ON h.MigrationId = m.MigrationId
ORDER BY m.MigrationId;

-- Colonnes ConfigSocietes requises par l'API actuelle
SELECT COLUMN_NAME,
       CASE WHEN COUNT(*) > 0 THEN 'OK' ELSE 'MANQUANT' END AS Statut
FROM (
    SELECT 'PourcentageReversementSite' AS COLUMN_NAME
    UNION ALL SELECT 'FraisPlateforme'
    UNION ALL SELECT 'CodeDeviseFraisPlateforme'
    UNION ALL SELECT 'MontAddPaieElectronique'
    UNION ALL SELECT 'CodeDeviseMontAddPaieElectronique'
    UNION ALL SELECT 'AutoReversementPaiementElectronique'
) expected
LEFT JOIN INFORMATION_SCHEMA.COLUMNS c
    ON c.TABLE_SCHEMA = @db
   AND c.TABLE_NAME = 'ConfigSocietes'
   AND c.COLUMN_NAME = expected.COLUMN_NAME
GROUP BY COLUMN_NAME
ORDER BY COLUMN_NAME;

-- =============================================================================
-- APPLICATION
-- =============================================================================

-- -----------------------------------------------------------------------------
-- A — Origine sur Reservations / Paiements / CommandesReservationEnAttente
-- Migration : 20260608101418_OrigineOperationReservationPaiement
-- -----------------------------------------------------------------------------
SET @col_origine_res := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'Reservations' AND COLUMN_NAME = 'Origine'
);

SET @sql_origine_res := IF(
    @col_origine_res = 0,
    'ALTER TABLE `Reservations` ADD COLUMN `Origine` varchar(20) CHARACTER SET utf8mb4 NOT NULL DEFAULT ''INCONNU''',
    'SELECT ''Reservations.Origine déjà présent'' AS Info'
);
PREPARE stmt FROM @sql_origine_res; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_origine_pay := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'Paiements' AND COLUMN_NAME = 'Origine'
);

SET @sql_origine_pay := IF(
    @col_origine_pay = 0,
    'ALTER TABLE `Paiements` ADD COLUMN `Origine` varchar(20) CHARACTER SET utf8mb4 NOT NULL DEFAULT ''INCONNU''',
    'SELECT ''Paiements.Origine déjà présent'' AS Info'
);
PREPARE stmt FROM @sql_origine_pay; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_origine_cmd := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'CommandesReservationEnAttente' AND COLUMN_NAME = 'Origine'
);

SET @sql_origine_cmd := IF(
    @col_origine_cmd = 0,
    'ALTER TABLE `CommandesReservationEnAttente` ADD COLUMN `Origine` varchar(20) CHARACTER SET utf8mb4 NOT NULL DEFAULT ''INCONNU''',
    'SELECT ''CommandesReservationEnAttente.Origine déjà présent'' AS Info'
);
PREPARE stmt FROM @sql_origine_cmd; EXECUTE stmt; DEALLOCATE PREPARE stmt;

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260608101418_OrigineOperationReservationPaiement', '6.0.25');

-- -----------------------------------------------------------------------------
-- B — Index unique Destinations (IdSociete, VilleDepart, VilleArrivee)
-- Migration : 20260615121938_DestinationSocieteVillesUnique
-- Échoue si doublons : corriger les données avant de relancer.
-- -----------------------------------------------------------------------------
SET @idx_dest_unique := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = @db
      AND TABLE_NAME = 'Destinations'
      AND INDEX_NAME = 'IX_Destinations_Societe_Villes_Unique'
);

SET @dest_dupes := (
    SELECT COUNT(*) FROM (
        SELECT IdSociete, VilleDepart, VilleArrivee, COUNT(*) AS n
        FROM `Destinations`
        GROUP BY IdSociete, VilleDepart, VilleArrivee
        HAVING n > 1
    ) d
);

SELECT @dest_dupes AS DestinationsDoublonsAVerifier;

SET @sql_dest_idx := IF(
    @idx_dest_unique = 0 AND @dest_dupes = 0,
    'CREATE UNIQUE INDEX `IX_Destinations_Societe_Villes_Unique` ON `Destinations` (`IdSociete`, `VilleDepart`, `VilleArrivee`)',
    IF(@idx_dest_unique > 0,
        'SELECT ''Index IX_Destinations_Societe_Villes_Unique déjà présent'' AS Info',
        'SELECT ''Index Destinations NON créé : doublons détectés — nettoyer puis relancer'' AS Info')
);

PREPARE stmt FROM @sql_dest_idx; EXECUTE stmt; DEALLOCATE PREPARE stmt;

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
SELECT '20260615121938_DestinationSocieteVillesUnique', '6.0.25'
FROM DUAL
WHERE @idx_dest_unique > 0 OR @dest_dupes = 0;

-- -----------------------------------------------------------------------------
-- C — ConfigSocietes.PourcentageReversementSite
-- Migration : 20260618134551_PourcentageReversementSiteConfig
-- -----------------------------------------------------------------------------
SET @col_pct := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'ConfigSocietes' AND COLUMN_NAME = 'PourcentageReversementSite'
);

SET @sql_pct := IF(
    @col_pct = 0,
    'ALTER TABLE `ConfigSocietes` ADD COLUMN `PourcentageReversementSite` decimal(18,2) NOT NULL DEFAULT 100.00',
    'SELECT ''ConfigSocietes.PourcentageReversementSite déjà présent'' AS Info'
);
PREPARE stmt FROM @sql_pct; EXECUTE stmt; DEALLOCATE PREPARE stmt;

UPDATE `ConfigSocietes`
SET `PourcentageReversementSite` = 100.00
WHERE `PourcentageReversementSite` IS NULL
   OR `PourcentageReversementSite` < 0
   OR `PourcentageReversementSite` > 100;

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260618134551_PourcentageReversementSiteConfig', '6.0.25');

-- -----------------------------------------------------------------------------
-- D — ConfigSocietes.FraisPlateforme + CodeDeviseFraisPlateforme
-- Migration : 20260618135910_FraisPlateformeConfig
-- -----------------------------------------------------------------------------
SET @col_frais := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'ConfigSocietes' AND COLUMN_NAME = 'FraisPlateforme'
);

SET @sql_frais := IF(
    @col_frais = 0,
    'ALTER TABLE `ConfigSocietes` ADD COLUMN `FraisPlateforme` decimal(18,2) NOT NULL DEFAULT 0.00',
    'SELECT ''ConfigSocietes.FraisPlateforme déjà présent'' AS Info'
);
PREPARE stmt FROM @sql_frais; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_devise_frais := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'ConfigSocietes' AND COLUMN_NAME = 'CodeDeviseFraisPlateforme'
);

SET @sql_devise_frais := IF(
    @col_devise_frais = 0,
    'ALTER TABLE `ConfigSocietes` ADD COLUMN `CodeDeviseFraisPlateforme` varchar(3) CHARACTER SET utf8mb4 NULL',
    'SELECT ''ConfigSocietes.CodeDeviseFraisPlateforme déjà présent'' AS Info'
);
PREPARE stmt FROM @sql_devise_frais; EXECUTE stmt; DEALLOCATE PREPARE stmt;

UPDATE `ConfigSocietes`
SET `FraisPlateforme` = 0.00
WHERE `FraisPlateforme` IS NULL OR `FraisPlateforme` < 0;

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260618135910_FraisPlateformeConfig', '6.0.25');

-- -----------------------------------------------------------------------------
-- E — ConfigSocietes.MontAddPaieElectronique + CodeDeviseMontAddPaieElectronique
-- Migration : 20260618171505_MontAddPaieElectroniqueConfig
-- Corrige : Unknown column 'c.CodeDeviseMontAddPaieElectronique'
-- -----------------------------------------------------------------------------
SET @col_mont_add := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'ConfigSocietes' AND COLUMN_NAME = 'MontAddPaieElectronique'
);

SET @sql_mont_add := IF(
    @col_mont_add = 0,
    'ALTER TABLE `ConfigSocietes` ADD COLUMN `MontAddPaieElectronique` decimal(18,2) NOT NULL DEFAULT 0.00',
    'SELECT ''ConfigSocietes.MontAddPaieElectronique déjà présent'' AS Info'
);
PREPARE stmt FROM @sql_mont_add; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_devise_mont_add := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'ConfigSocietes' AND COLUMN_NAME = 'CodeDeviseMontAddPaieElectronique'
);

SET @sql_devise_mont_add := IF(
    @col_devise_mont_add = 0,
    'ALTER TABLE `ConfigSocietes` ADD COLUMN `CodeDeviseMontAddPaieElectronique` varchar(3) CHARACTER SET utf8mb4 NULL',
    'SELECT ''ConfigSocietes.CodeDeviseMontAddPaieElectronique déjà présent'' AS Info'
);
PREPARE stmt FROM @sql_devise_mont_add; EXECUTE stmt; DEALLOCATE PREPARE stmt;

UPDATE `ConfigSocietes`
SET `MontAddPaieElectronique` = 0.00
WHERE `MontAddPaieElectronique` IS NULL OR `MontAddPaieElectronique` < 0;

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260618171505_MontAddPaieElectroniqueConfig', '6.0.25');

-- -----------------------------------------------------------------------------
-- F — Clients.AdresseClient optionnel + Agents email/téléphone
-- Migration : 20260619134037_ClientAdresseClientOptional
-- -----------------------------------------------------------------------------
UPDATE `Clients`
SET `AdresseClient` = NULL
WHERE `AdresseClient` IS NOT NULL AND TRIM(`AdresseClient`) = '';

SET @clients_adresse_nullable := (
    SELECT IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'Clients' AND COLUMN_NAME = 'AdresseClient'
    LIMIT 1
);

SET @sql_clients_adresse := IF(
    @clients_adresse_nullable = 'NO',
    'ALTER TABLE `Clients` MODIFY COLUMN `AdresseClient` varchar(500) CHARACTER SET utf8mb4 NULL',
    'SELECT ''Clients.AdresseClient déjà nullable'' AS Info'
);
PREPARE stmt FROM @sql_clients_adresse; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @agents_tel_type := (
    SELECT COLUMN_TYPE FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'Agents' AND COLUMN_NAME = 'TelephoneAgent'
    LIMIT 1
);

SET @sql_agents_tel := IF(
    @agents_tel_type IS NOT NULL AND @agents_tel_type <> 'varchar(200)',
    'ALTER TABLE `Agents` MODIFY COLUMN `TelephoneAgent` varchar(200) CHARACTER SET utf8mb4 NULL',
    'SELECT ''Agents.TelephoneAgent déjà varchar(200) ou absent'' AS Info'
);
PREPARE stmt FROM @sql_agents_tel; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @agents_email_type := (
    SELECT COLUMN_TYPE FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'Agents' AND COLUMN_NAME = 'EmailAgent'
    LIMIT 1
);

SET @sql_agents_email := IF(
    @agents_email_type IS NOT NULL AND @agents_email_type <> 'varchar(200)',
    'ALTER TABLE `Agents` MODIFY COLUMN `EmailAgent` varchar(200) CHARACTER SET utf8mb4 NULL',
    'SELECT ''Agents.EmailAgent déjà varchar(200) ou absent'' AS Info'
);
PREPARE stmt FROM @sql_agents_email; EXECUTE stmt; DEALLOCATE PREPARE stmt;

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260619134037_ClientAdresseClientOptional', '6.0.25');

-- =============================================================================
-- VÉRIFICATION FINALE
-- =============================================================================
SELECT COLUMN_NAME, COLUMN_TYPE, IS_NULLABLE, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = @db
  AND TABLE_NAME = 'ConfigSocietes'
  AND COLUMN_NAME IN (
      'PourcentageReversementSite',
      'FraisPlateforme',
      'CodeDeviseFraisPlateforme',
      'MontAddPaieElectronique',
      'CodeDeviseMontAddPaieElectronique',
      'AutoReversementPaiementElectronique'
  )
ORDER BY COLUMN_NAME;

SELECT MigrationId, ProductVersion
FROM `__EFMigrationsHistory`
WHERE MigrationId IN (
    '20260608101418_OrigineOperationReservationPaiement',
    '20260615121938_DestinationSocieteVillesUnique',
    '20260618134551_PourcentageReversementSiteConfig',
    '20260618135910_FraisPlateformeConfig',
    '20260618171505_MontAddPaieElectroniqueConfig',
    '20260619134037_ClientAdresseClientOptional'
)
ORDER BY MigrationId;

-- Attendu pour corriger l'erreur API :
--   CodeDeviseMontAddPaieElectronique et MontAddPaieElectronique présents sur ConfigSocietes
--   Migration 20260618171505_MontAddPaieElectroniqueConfig dans __EFMigrationsHistory

-- -----------------------------------------------------------------------------
-- Post-déploiement métier (exemples — optionnel)
-- -----------------------------------------------------------------------------
-- Supplément paiement électronique 500 CDF par place :
-- UPDATE ConfigSocietes
-- SET MontAddPaieElectronique = 500.00,
--     CodeDeviseMontAddPaieElectronique = 'CDF'
-- WHERE IdSociete = 60;
