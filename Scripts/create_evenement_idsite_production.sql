-- IdSite sur sessions / réservations / paiements événement (préparation reversement type Transport).
-- Idempotent : colonnes + FK + indexes + backfill site principal.

SET @col := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'EvenementSessions' AND COLUMN_NAME = 'IdSite'
);
SET @sql := IF(@col = 0,
    'ALTER TABLE `EvenementSessions` ADD `IdSite` int NULL',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'EvenementReservations' AND COLUMN_NAME = 'IdSite'
);
SET @sql := IF(@col = 0,
    'ALTER TABLE `EvenementReservations` ADD `IdSite` int NULL',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'EvenementPayments' AND COLUMN_NAME = 'IdSite'
);
SET @sql := IF(@col = 0,
    'ALTER TABLE `EvenementPayments` ADD `IdSite` int NULL',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

UPDATE `EvenementSessions` es
INNER JOIN (
    SELECT s.`IdSociete`,
           COALESCE(
               MAX(CASE WHEN s.`IsSitePrincipal` = 1 THEN s.`IdSite` END),
               MIN(s.`IdSite`)
           ) AS `IdSite`
    FROM `Sites` s
    WHERE s.`Statut` = 1
    GROUP BY s.`IdSociete`
) pick ON pick.`IdSociete` = es.`IdSociete`
SET es.`IdSite` = pick.`IdSite`
WHERE es.`IdSite` IS NULL;

-- FK (ignore si déjà présentes)
SET @fk := (
    SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'EvenementSessions'
      AND CONSTRAINT_NAME = 'FK_EvenementSessions_Sites_IdSite'
);
SET @sql := IF(@fk = 0,
    'ALTER TABLE `EvenementSessions` ADD CONSTRAINT `FK_EvenementSessions_Sites_IdSite` FOREIGN KEY (`IdSite`) REFERENCES `Sites` (`IdSite`) ON DELETE RESTRICT',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @fk := (
    SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'EvenementReservations'
      AND CONSTRAINT_NAME = 'FK_EvenementReservations_Sites_IdSite'
);
SET @sql := IF(@fk = 0,
    'ALTER TABLE `EvenementReservations` ADD CONSTRAINT `FK_EvenementReservations_Sites_IdSite` FOREIGN KEY (`IdSite`) REFERENCES `Sites` (`IdSite`) ON DELETE RESTRICT',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @fk := (
    SELECT COUNT(*) FROM information_schema.TABLE_CONSTRAINTS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'EvenementPayments'
      AND CONSTRAINT_NAME = 'FK_EvenementPayments_Sites_IdSite'
);
SET @sql := IF(@fk = 0,
    'ALTER TABLE `EvenementPayments` ADD CONSTRAINT `FK_EvenementPayments_Sites_IdSite` FOREIGN KEY (`IdSite`) REFERENCES `Sites` (`IdSite`) ON DELETE RESTRICT',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
