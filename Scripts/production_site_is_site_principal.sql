-- =============================================================================
-- Production : Site.IsSitePrincipal + index + backfill (1 principal / société)
-- Migration EF équivalente : 20260530075224_SiteIsSitePrincipal
-- =============================================================================
-- Prérequis :
--   - MySQL 5.7+ / 8.x
--   - Tables `Sites`, `InfoPaiementsSociete` déjà présentes
--
-- Règles de backfill (par IdSociete) :
--   1. Site ayant une InfoPaiementsSociete active (Statut = 1), IdSite minimal si plusieurs
--   2. Sinon site actif (Statut = 1) avec IdSite minimal
--   3. Sinon premier site (IdSite minimal)
--
-- Recommandation : sauvegarde avant exécution ; fenêtre de maintenance courte.
-- =============================================================================
SET SQL_SAFE_UPDATES = 0;
SET @db := DATABASE();

-- -----------------------------------------------------------------------------
-- 1. Colonne IsSitePrincipal (idempotent)
-- -----------------------------------------------------------------------------
SET @col_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db
      AND TABLE_NAME = 'Sites'
      AND COLUMN_NAME = 'IsSitePrincipal'
);

SET @sql_add_col := IF(
    @col_exists = 0,
    'ALTER TABLE `Sites` ADD COLUMN `IsSitePrincipal` tinyint(1) NOT NULL DEFAULT 0',
    'SELECT ''Colonne Sites.IsSitePrincipal déjà présente — ignoré'' AS Info'
);

PREPARE stmt FROM @sql_add_col;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- -----------------------------------------------------------------------------
-- 2. Index IX_Sites_IdSociete_IsSitePrincipal (idempotent)
-- -----------------------------------------------------------------------------
SET @idx_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = @db
      AND TABLE_NAME = 'Sites'
      AND INDEX_NAME = 'IX_Sites_IdSociete_IsSitePrincipal'
);

SET @sql_add_idx := IF(
    @idx_exists = 0,
    'CREATE INDEX `IX_Sites_IdSociete_IsSitePrincipal` ON `Sites` (`IdSociete`, `IsSitePrincipal`)',
    'SELECT ''Index IX_Sites_IdSociete_IsSitePrincipal déjà présent — ignoré'' AS Info'
);

PREPARE stmt FROM @sql_add_idx;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- -----------------------------------------------------------------------------
-- 3. Backfill (ré-exécutable : remet tout à 0 puis re-marque les principaux)
-- -----------------------------------------------------------------------------
START TRANSACTION;

UPDATE `Sites` SET `IsSitePrincipal` = 0;

-- Priorité 1 : site avec InfoPaiement FlexPay actif
UPDATE `Sites` s
INNER JOIN (
    SELECT ips.`IdSociete`, MIN(ips.`IdSite`) AS `IdSite`
    FROM `InfoPaiementsSociete` ips
    INNER JOIN `Sites` st ON st.`IdSite` = ips.`IdSite` AND st.`IdSociete` = ips.`IdSociete`
    WHERE ips.`Statut` = 1
    GROUP BY ips.`IdSociete`
) pick ON s.`IdSociete` = pick.`IdSociete` AND s.`IdSite` = pick.`IdSite`
SET s.`IsSitePrincipal` = 1;

-- Priorité 2 : plus ancien site actif (sociétés sans principal après étape 1)
UPDATE `Sites` s
INNER JOIN (
    SELECT st.`IdSociete`, MIN(st.`IdSite`) AS `IdSite`
    FROM `Sites` st
    WHERE st.`Statut` = 1
      AND NOT EXISTS (
          SELECT 1 FROM `Sites` p
          WHERE p.`IdSociete` = st.`IdSociete` AND p.`IsSitePrincipal` = 1)
    GROUP BY st.`IdSociete`
) pick ON s.`IdSociete` = pick.`IdSociete` AND s.`IdSite` = pick.`IdSite`
SET s.`IsSitePrincipal` = 1;

-- Priorité 3 : premier site (sociétés sans site actif ou sans match)
UPDATE `Sites` s
INNER JOIN (
    SELECT st.`IdSociete`, MIN(st.`IdSite`) AS `IdSite`
    FROM `Sites` st
    WHERE NOT EXISTS (
          SELECT 1 FROM `Sites` p
          WHERE p.`IdSociete` = st.`IdSociete` AND p.`IsSitePrincipal` = 1)
    GROUP BY st.`IdSociete`
) pick ON s.`IdSociete` = pick.`IdSociete` AND s.`IdSite` = pick.`IdSite`
SET s.`IsSitePrincipal` = 1;

-- Historique EF (si vous utilisez dotnet ef database update ensuite, cette ligne évite un re-run)
INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260530075224_SiteIsSitePrincipal', '6.0.25');

COMMIT;

-- -----------------------------------------------------------------------------
-- 4. Contrôles post-déploiement (lecture seule)
-- -----------------------------------------------------------------------------

-- Sociétés sans site principal (doit retourner 0 ligne)
SELECT s.`IdSociete`, COUNT(*) AS nb_sites
FROM `Sites` s
GROUP BY s.`IdSociete`
HAVING SUM(CASE WHEN s.`IsSitePrincipal` = 1 THEN 1 ELSE 0 END) = 0;

-- Sociétés avec plus d'un principal (doit retourner 0 ligne)
SELECT `IdSociete`, SUM(`IsSitePrincipal`) AS nb_principaux
FROM `Sites`
GROUP BY `IdSociete`
HAVING nb_principaux > 1;

-- Aperçu des sites principaux
SELECT s.`IdSociete`, s.`IdSite`, s.`CodeSite`, s.`NomSite`, s.`Statut`, s.`IsSitePrincipal`,
       EXISTS (
           SELECT 1 FROM `InfoPaiementsSociete` ips
           WHERE ips.`IdSite` = s.`IdSite` AND ips.`IdSociete` = s.`IdSociete` AND ips.`Statut` = 1
       ) AS has_info_paiement_actif
FROM `Sites` s
WHERE s.`IsSitePrincipal` = 1
ORDER BY s.`IdSociete`, s.`IdSite`;

-- =============================================================================
-- ROLLBACK (manuel, si besoin — exécuter hors transaction du script ci-dessus)
-- =============================================================================
-- START TRANSACTION;
-- DROP INDEX `IX_Sites_IdSociete_IsSitePrincipal` ON `Sites`;
-- ALTER TABLE `Sites` DROP COLUMN `IsSitePrincipal`;
-- DELETE FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260530075224_SiteIsSitePrincipal';
-- COMMIT;
