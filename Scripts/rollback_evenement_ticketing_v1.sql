-- =============================================================================
-- Rollback migration : Evenement Ticketing V1 (+ pricing GlobalQuota)
-- DESTRUCTIF : supprime toutes les données Evenement* et l'historique EF associé.
-- Exécuter UNIQUEMENT si aucune billetterie événement en production.
-- =============================================================================

START TRANSACTION;

DROP EVENT IF EXISTS `ev_expire_evenement_holds`;
DROP PROCEDURE IF EXISTS `sp_ExpireEvenementHolds`;

DROP TRIGGER IF EXISTS `TRG_EvenementReservationLines_BU`;
DROP TRIGGER IF EXISTS `TRG_EvenementReservationLines_BI`;

DROP TABLE IF EXISTS `EvenementPayments`;
DROP TABLE IF EXISTS `EvenementTickets`;
DROP TABLE IF EXISTS `EvenementReservationLines`;
DROP TABLE IF EXISTS `EvenementSessionSeats`;
DROP TABLE IF EXISTS `EvenementReservations`;
DROP TABLE IF EXISTS `EvenementSessionClassQuotas`;
DROP TABLE IF EXISTS `EvenementSessionGlobalQuotas`;
DROP TABLE IF EXISTS `EvenementSessionSections`;
DROP TABLE IF EXISTS `EvenementClasses`;
DROP TABLE IF EXISTS `EvenementSessions`;

-- Colonne ConfigSociete (migration EvenementTicketingV1)
SET @DbName = DATABASE();
SET @has_col = (
    SELECT COUNT(*) FROM `INFORMATION_SCHEMA`.`COLUMNS`
    WHERE `TABLE_SCHEMA` = @DbName
      AND `TABLE_NAME` = 'ConfigSocietes'
      AND `COLUMN_NAME` = 'DureeHoldEvenementMinutes'
);
SET @sql_drop_col = IF(
    @has_col > 0,
    'ALTER TABLE `ConfigSocietes` DROP COLUMN `DureeHoldEvenementMinutes`',
    'SELECT ''DureeHoldEvenementMinutes déjà absente'' AS `Info`'
);
PREPARE stmt FROM @sql_drop_col;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

DELETE FROM `__EFMigrationsHistory`
WHERE `MigrationId` IN (
    '20260703120104_EvenementSessionGlobalQuotaPricing',
    '20260703101713_EvenementTicketingV1'
);

COMMIT;
