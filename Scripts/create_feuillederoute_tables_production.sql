-- =============================================================================
-- CongoTravel — FeuilleDeRoute (manifeste embarquement) — script production
-- Migration : 20260717104104_FeuilleDeRouteV1
-- Prérequis : tables Societes, Voyages, Utilisateurs déjà présentes
-- =============================================================================
-- À exécuter UNE SEULE FOIS sur une base qui n'a pas encore cette migration.
-- Vérifier avant :
--   SELECT * FROM `__EFMigrationsHistory`
--     WHERE `MigrationId` = '20260717104104_FeuilleDeRouteV1';
-- Si une ligne existe déjà : ne pas réexécuter ce script.
-- =============================================================================

START TRANSACTION;

CREATE TABLE `FeuilleDeRoutes` (
    `IdFeuilleDeRoute` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `IdVoyage` int NOT NULL,
    `DateEmbarquement` date NOT NULL,
    `DateGenerationUtc` datetime(6) NOT NULL,
    `IdUtilisateurGeneration` int NULL,
    `SocieteNom` varchar(150) CHARACTER SET utf8mb4 NULL,
    `SocieteTelephone` varchar(50) CHARACTER SET utf8mb4 NULL,
    `SocieteEmail` varchar(256) CHARACTER SET utf8mb4 NULL,
    `SocieteAdresse` varchar(500) CHARACTER SET utf8mb4 NULL,
    `SocieteLogo` longtext CHARACTER SET utf8mb4 NULL,
    `VoyageDateDepart` datetime(6) NOT NULL,
    `VoyageHeureDepart` time(6) NOT NULL,
    `VoyagePrix` int NOT NULL,
    `VoyageCodeDevise` varchar(3) CHARACTER SET utf8mb4 NOT NULL,
    `IdDestination` int NOT NULL,
    `DestinationLibelle` varchar(450) CHARACTER SET utf8mb4 NULL,
    `IdVehicule` int NOT NULL,
    `VehiculeImmatriculation` varchar(20) CHARACTER SET utf8mb4 NULL,
    `VehiculeAlias` varchar(100) CHARACTER SET utf8mb4 NULL,
    `IdSite` int NULL,
    `SiteNom` varchar(200) CHARACTER SET utf8mb4 NULL,
    `NombrePassagers` int NOT NULL,
    CONSTRAINT `PK_FeuilleDeRoutes` PRIMARY KEY (`IdFeuilleDeRoute`),
    CONSTRAINT `FK_FeuilleDeRoutes_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
    CONSTRAINT `FK_FeuilleDeRoutes_Utilisateurs_IdUtilisateurGeneration` FOREIGN KEY (`IdUtilisateurGeneration`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE RESTRICT,
    CONSTRAINT `FK_FeuilleDeRoutes_Voyages_IdVoyage` FOREIGN KEY (`IdVoyage`) REFERENCES `Voyages` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `FeuilleDeRoutePassagers` (
    `IdFeuilleDeRoutePassager` int NOT NULL AUTO_INCREMENT,
    `IdFeuilleDeRoute` int NOT NULL,
    `IdEmbarquement` int NULL,
    `IdBillet` int NULL,
    `IdReservationPassenger` int NULL,
    `IdReservation` int NULL,
    `NomComplet` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Telephone` varchar(20) CHARACTER SET utf8mb4 NULL,
    `Email` varchar(256) CHARACTER SET utf8mb4 NULL,
    `DocumentType` varchar(50) CHARACTER SET utf8mb4 NULL,
    `DocumentNumero` varchar(100) CHARACTER SET utf8mb4 NULL,
    `CodeSiege` varchar(120) CHARACTER SET utf8mb4 NULL,
    `DateEmbarquementUtc` datetime(6) NULL,
    `IdUtilisateurEnregistrement` int NULL,
    CONSTRAINT `PK_FeuilleDeRoutePassagers` PRIMARY KEY (`IdFeuilleDeRoutePassager`),
    CONSTRAINT `FK_FeuilleDeRoutePassagers_FeuilleDeRoutes_IdFeuilleDeRoute` FOREIGN KEY (`IdFeuilleDeRoute`) REFERENCES `FeuilleDeRoutes` (`IdFeuilleDeRoute`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_FeuilleDeRoutePassagers_IdFeuilleDeRoute` ON `FeuilleDeRoutePassagers` (`IdFeuilleDeRoute`);

CREATE INDEX `IX_FeuilleDeRoutes_IdSociete` ON `FeuilleDeRoutes` (`IdSociete`);

CREATE INDEX `IX_FeuilleDeRoutes_IdUtilisateurGeneration` ON `FeuilleDeRoutes` (`IdUtilisateurGeneration`);

CREATE INDEX `IX_FeuilleDeRoutes_IdVoyage` ON `FeuilleDeRoutes` (`IdVoyage`);

CREATE INDEX `IX_FeuilleDeRoutes_Societe_DateEmbarquement` ON `FeuilleDeRoutes` (`IdSociete`, `DateEmbarquement`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260717104104_FeuilleDeRouteV1', '6.0.25');

COMMIT;
