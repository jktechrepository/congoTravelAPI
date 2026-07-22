-- =============================================================================
-- Migration production : PlanificationVoyageV1
-- Migration EF : 20260531142422_PlanificationVoyageV1
-- Depuis       : 20260530121511_ConfigSocietePenalitePourcentage
-- Généré par   : dotnet ef migrations script (voir README_PLANIFICATION_VOYAGE_PRODUCTION.md)
--
-- PRÉREQUIS :
--   1. Sauvegarde complète de la base
--   2. Exécuter verify_planification_voyage_pre_prod.sql
--   3. Vérifier que la dernière migration prod = ConfigSocietePenalitePourcentage
--      (sinon régénérer ce script avec la bonne migration source)
-- =============================================================================

START TRANSACTION;

ALTER TABLE `Voyages` ADD `IdPlanificationVoyage` int NULL;

CREATE TABLE `PlanificationsVoyage` (
    `IdPlanificationVoyage` int NOT NULL AUTO_INCREMENT,
    `Libelle` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `IdSociete` int NOT NULL,
    `IdSite` int NOT NULL,
    `IdVehicule` int NOT NULL,
    `HeureDepart` time(6) NOT NULL,
    `Prix` int NOT NULL,
    `CodeDevisePrix` varchar(3) CHARACTER SET utf8mb4 NOT NULL,
    `JoursSemaine` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Statut` tinyint(1) NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_PlanificationsVoyage` PRIMARY KEY (`IdPlanificationVoyage`),
    CONSTRAINT `FK_PlanificationsVoyage_Sites_IdSite` FOREIGN KEY (`IdSite`) REFERENCES `Sites` (`IdSite`) ON DELETE RESTRICT,
    CONSTRAINT `FK_PlanificationsVoyage_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
    CONSTRAINT `FK_PlanificationsVoyage_Vehicules_IdVehicule` FOREIGN KEY (`IdVehicule`) REFERENCES `Vehicules` (`IdVehicule`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `PlanificationGenerationLogs` (
    `IdPlanificationGenerationLog` int NOT NULL AUTO_INCREMENT,
    `IdPlanificationVoyage` int NOT NULL,
    `DateDebut` datetime(6) NOT NULL,
    `DateFin` datetime(6) NOT NULL,
    `NombreCrees` int NOT NULL,
    `NombreIgnores` int NOT NULL,
    `NombreEchecs` int NOT NULL,
    `DetailsJson` longtext CHARACTER SET utf8mb4 NOT NULL,
    `DeclencheParIdUtilisateur` int NULL,
    `DateCreation` datetime(6) NOT NULL,
    CONSTRAINT `PK_PlanificationGenerationLogs` PRIMARY KEY (`IdPlanificationGenerationLog`),
    CONSTRAINT `FK_PlanificationGenerationLogs_PlanificationsVoyage_IdPlanifica~` FOREIGN KEY (`IdPlanificationVoyage`) REFERENCES `PlanificationsVoyage` (`IdPlanificationVoyage`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `PlanificationVoyageEtapes` (
    `IdPlanificationVoyageEtape` int NOT NULL AUTO_INCREMENT,
    `IdPlanificationVoyage` int NOT NULL,
    `IdDestination` int NOT NULL,
    `Ordre` int NOT NULL,
    `IdSociete` int NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    CONSTRAINT `PK_PlanificationVoyageEtapes` PRIMARY KEY (`IdPlanificationVoyageEtape`),
    CONSTRAINT `FK_PlanificationVoyageEtapes_Destinations_IdDestination` FOREIGN KEY (`IdDestination`) REFERENCES `Destinations` (`IdDestination`) ON DELETE RESTRICT,
    CONSTRAINT `FK_PlanificationVoyageEtapes_PlanificationsVoyage_IdPlanificati~` FOREIGN KEY (`IdPlanificationVoyage`) REFERENCES `PlanificationsVoyage` (`IdPlanificationVoyage`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `PlanificationVoyageTarifs` (
    `IdPlanificationVoyageTarif` int NOT NULL AUTO_INCREMENT,
    `IdPlanificationVoyage` int NOT NULL,
    `IdCategorieSiege` int NOT NULL,
    `Prix` int NOT NULL,
    `IdSociete` int NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    CONSTRAINT `PK_PlanificationVoyageTarifs` PRIMARY KEY (`IdPlanificationVoyageTarif`),
    CONSTRAINT `FK_PlanificationVoyageTarifs_CategorieSieges_IdCategorieSiege` FOREIGN KEY (`IdCategorieSiege`) REFERENCES `CategorieSieges` (`IdCategorieSiege`) ON DELETE RESTRICT,
    CONSTRAINT `FK_PlanificationVoyageTarifs_PlanificationsVoyage_IdPlanificati~` FOREIGN KEY (`IdPlanificationVoyage`) REFERENCES `PlanificationsVoyage` (`IdPlanificationVoyage`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_Voyages_IdPlanificationVoyage` ON `Voyages` (`IdPlanificationVoyage`);

CREATE INDEX `IX_PlanificationGenerationLogs_IdPlanificationVoyage` ON `PlanificationGenerationLogs` (`IdPlanificationVoyage`);

CREATE INDEX `IX_PlanificationsVoyage_IdSite` ON `PlanificationsVoyage` (`IdSite`);

CREATE INDEX `IX_PlanificationsVoyage_IdSociete` ON `PlanificationsVoyage` (`IdSociete`);

CREATE INDEX `IX_PlanificationsVoyage_IdVehicule` ON `PlanificationsVoyage` (`IdVehicule`);

CREATE INDEX `IX_PlanificationVoyageEtapes_IdDestination` ON `PlanificationVoyageEtapes` (`IdDestination`);

CREATE UNIQUE INDEX `IX_PlanificationVoyageEtapes_Planif_Ordre_Unique` ON `PlanificationVoyageEtapes` (`IdPlanificationVoyage`, `Ordre`);

CREATE INDEX `IX_PlanificationVoyageTarifs_IdCategorieSiege` ON `PlanificationVoyageTarifs` (`IdCategorieSiege`);

CREATE UNIQUE INDEX `IX_PlanificationVoyageTarifs_Planif_Categorie_Unique` ON `PlanificationVoyageTarifs` (`IdPlanificationVoyage`, `IdCategorieSiege`);

ALTER TABLE `Voyages` ADD CONSTRAINT `FK_Voyages_PlanificationsVoyage_IdPlanificationVoyage` FOREIGN KEY (`IdPlanificationVoyage`) REFERENCES `PlanificationsVoyage` (`IdPlanificationVoyage`) ON DELETE SET NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260531142422_PlanificationVoyageV1', '6.0.25');

COMMIT;

