-- =============================================================================
-- CongoTravel — Site Touristique Planification V1.1
-- =============================================================================
-- Prérequis : production_site_touristique_ticketing_v1.sql déjà appliqué
-- Idempotent : CREATE TABLE IF NOT EXISTS + garde-fou colonne FK.
-- =============================================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS `SiteTouristiquePlanifications` (
    `IdSiteTouristiquePlanification` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `IdSiteTouristique` int NOT NULL,
    `Libelle` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `JoursSemaine` longtext CHARACTER SET utf8mb4 NOT NULL,
    `InventoryMode` enum('ClassQuota','GlobalQuota') CHARACTER SET utf8mb4 NOT NULL,
    `CodeDevise` char(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF',
    `SalesOpenOffsetHours` int NULL,
    `SalesCloseOffsetHours` int NULL,
    `Statut` tinyint(1) NOT NULL DEFAULT TRUE,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_SiteTouristiquePlanifications` PRIMARY KEY (`IdSiteTouristiquePlanification`),
    CONSTRAINT `FK_SiteTouristiquePlanifications_Societes_IdSociete`
        FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
    CONSTRAINT `FK_SiteTouristiquePlanifications_Lieux_IdSiteTouristique`
        FOREIGN KEY (`IdSiteTouristique`) REFERENCES `SiteTouristiques` (`IdSiteTouristique`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `SiteTouristiquePlanifGlobalQuotas` (
    `IdSiteTouristiquePlanification` int NOT NULL,
    `CapaciteTotale` int NOT NULL,
    `PrixUnitaire` decimal(18,2) NOT NULL,
    CONSTRAINT `PK_SiteTouristiquePlanifGlobalQuotas` PRIMARY KEY (`IdSiteTouristiquePlanification`),
    CONSTRAINT `CK_SiteTouristiquePlanifGlobalQuotas_Capacite` CHECK (`CapaciteTotale` >= 0),
    CONSTRAINT `FK_SiteTouristiquePlanifGlobalQuotas_Planifications`
        FOREIGN KEY (`IdSiteTouristiquePlanification`)
            REFERENCES `SiteTouristiquePlanifications` (`IdSiteTouristiquePlanification`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `SiteTouristiquePlanifClassQuotas` (
    `IdSiteTouristiquePlanifClassQuota` int NOT NULL AUTO_INCREMENT,
    `IdSiteTouristiquePlanification` int NOT NULL,
    `IdSiteTouristiqueClasse` int NOT NULL,
    `CapaciteTotale` int NOT NULL,
    `PrixUnitaire` decimal(18,2) NOT NULL,
    CONSTRAINT `PK_SiteTouristiquePlanifClassQuotas` PRIMARY KEY (`IdSiteTouristiquePlanifClassQuota`),
    CONSTRAINT `CK_SiteTouristiquePlanifClassQuotas_Capacite` CHECK (`CapaciteTotale` >= 0),
    CONSTRAINT `FK_SiteTouristiquePlanifClassQuotas_Planifications`
        FOREIGN KEY (`IdSiteTouristiquePlanification`)
            REFERENCES `SiteTouristiquePlanifications` (`IdSiteTouristiquePlanification`) ON DELETE CASCADE,
    CONSTRAINT `FK_SiteTouristiquePlanifClassQuotas_Classes`
        FOREIGN KEY (`IdSiteTouristiqueClasse`)
            REFERENCES `SiteTouristiqueClasses` (`IdSiteTouristiqueClasse`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `SiteTouristiquePlanifGenerationLogs` (
    `IdSiteTouristiquePlanifGenerationLog` int NOT NULL AUTO_INCREMENT,
    `IdSiteTouristiquePlanification` int NOT NULL,
    `DateDebut` datetime(6) NOT NULL,
    `DateFin` datetime(6) NOT NULL,
    `NombreCrees` int NOT NULL,
    `NombreIgnores` int NOT NULL,
    `NombreEchecs` int NOT NULL,
    `DetailsJson` longtext CHARACTER SET utf8mb4 NOT NULL,
    `DeclencheParIdUtilisateur` int NULL,
    `DateCreation` datetime(6) NOT NULL,
    CONSTRAINT `PK_SiteTouristiquePlanifGenerationLogs` PRIMARY KEY (`IdSiteTouristiquePlanifGenerationLog`),
    CONSTRAINT `FK_SiteTouristiquePlanifGenerationLogs_Planifications`
        FOREIGN KEY (`IdSiteTouristiquePlanification`)
            REFERENCES `SiteTouristiquePlanifications` (`IdSiteTouristiquePlanification`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

-- FK optionnelle sur journées (SET NULL)
SET @col_exists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'SiteTouristiqueJournees'
      AND COLUMN_NAME = 'IdSiteTouristiquePlanification'
);
SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE `SiteTouristiqueJournees` ADD `IdSiteTouristiquePlanification` int NULL',
    'SELECT ''SiteTouristiqueJournees.IdSiteTouristiquePlanification déjà présent'' AS Info'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @fk_exists := (
    SELECT COUNT(*)
    FROM information_schema.TABLE_CONSTRAINTS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'SiteTouristiqueJournees'
      AND CONSTRAINT_NAME = 'FK_SiteTouristiqueJournees_Planifications'
);
SET @sql := IF(
    @fk_exists = 0,
    'ALTER TABLE `SiteTouristiqueJournees` ADD CONSTRAINT `FK_SiteTouristiqueJournees_Planifications` FOREIGN KEY (`IdSiteTouristiquePlanification`) REFERENCES `SiteTouristiquePlanifications` (`IdSiteTouristiquePlanification`) ON DELETE SET NULL',
    'SELECT ''FK_SiteTouristiqueJournees_Planifications déjà présente'' AS Info'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

CREATE INDEX  `IX_SiteTouristiquePlanifications_IdSociete`
    ON `SiteTouristiquePlanifications` (`IdSociete`);
CREATE INDEX  `IX_SiteTouristiquePlanifications_IdSiteTouristique`
    ON `SiteTouristiquePlanifications` (`IdSiteTouristique`);
CREATE UNIQUE INDEX  `IX_SiteTouristiquePlanifClassQuotas_Planif_Classe_UQ`
    ON `SiteTouristiquePlanifClassQuotas` (`IdSiteTouristiquePlanification`, `IdSiteTouristiqueClasse`);
CREATE INDEX  `IX_SiteTouristiquePlanifClassQuotas_IdPlanification`
    ON `SiteTouristiquePlanifClassQuotas` (`IdSiteTouristiquePlanification`);
CREATE INDEX  `IX_SiteTouristiquePlanifGenerationLogs_IdPlanification`
    ON `SiteTouristiquePlanifGenerationLogs` (`IdSiteTouristiquePlanification`);
CREATE INDEX `IX_SiteTouristiqueJournees_IdSiteTouristiquePlanification`
    ON `SiteTouristiqueJournees` (`IdSiteTouristiquePlanification`);

SET FOREIGN_KEY_CHECKS = 1;
