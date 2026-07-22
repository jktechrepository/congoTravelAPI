-- =============================================================================
-- Patch production : colonne Voyages.IdPlanificationVoyage manquante
-- Cas : les 4 tables Planification* ont été créées manuellement SANS
--       ALTER TABLE Voyages ADD IdPlanificationVoyage
--
-- Exécuter verify_planification_voyage_post_prod.sql AVANT ce script.
-- Si SHOW COLUMNS FROM Voyages LIKE 'IdPlanificationVoyage' retourne déjà 1 ligne,
-- NE PAS exécuter ce script.
-- =============================================================================

START TRANSACTION;

-- Colonne nullable sur Voyages (requise par tous les endpoints Voyage)
ALTER TABLE `Voyages` ADD COLUMN `IdPlanificationVoyage` int NULL;

CREATE INDEX `IX_Voyages_IdPlanificationVoyage` ON `Voyages` (`IdPlanificationVoyage`);

ALTER TABLE `Voyages` ADD CONSTRAINT `FK_Voyages_PlanificationsVoyage_IdPlanificationVoyage`
    FOREIGN KEY (`IdPlanificationVoyage`) REFERENCES `PlanificationsVoyage` (`IdPlanificationVoyage`)
    ON DELETE SET NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
SELECT '20260531142422_PlanificationVoyageV1', '6.0.25'
WHERE NOT EXISTS (
    SELECT 1 FROM `__EFMigrationsHistory`
    WHERE `MigrationId` = '20260531142422_PlanificationVoyageV1'
);

COMMIT;
