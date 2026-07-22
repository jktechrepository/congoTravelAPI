-- =============================================================================
-- Rollback : PlanificationVoyageV1
-- Migration EF : 20260531142422_PlanificationVoyageV1
-- ATTENTION : supprime les tables planification et la colonne Voyages.IdPlanificationVoyage
-- =============================================================================

START TRANSACTION;

ALTER TABLE `Voyages` DROP FOREIGN KEY `FK_Voyages_PlanificationsVoyage_IdPlanificationVoyage`;

DROP TABLE `PlanificationGenerationLogs`;

DROP TABLE `PlanificationVoyageEtapes`;

DROP TABLE `PlanificationVoyageTarifs`;

DROP TABLE `PlanificationsVoyage`;

ALTER TABLE `Voyages` DROP INDEX `IX_Voyages_IdPlanificationVoyage`;

ALTER TABLE `Voyages` DROP COLUMN `IdPlanificationVoyage`;

DELETE FROM `__EFMigrationsHistory`
WHERE `MigrationId` = '20260531142422_PlanificationVoyageV1';

COMMIT;

