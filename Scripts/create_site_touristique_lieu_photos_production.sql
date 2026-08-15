-- =============================================================================
-- CongoTravel — SiteTouristiqueLieuPhotos (max 3 photos / lieu)
-- Migration : 20260815162926_AddSiteTouristiqueLieuPhotos
-- Prérequis : table SiteTouristiques déjà présente
-- =============================================================================
-- À exécuter UNE SEULE FOIS. Vérifier avant :
--   SELECT * FROM `__EFMigrationsHistory`
--     WHERE `MigrationId` = '20260815162926_AddSiteTouristiqueLieuPhotos';
-- =============================================================================

START TRANSACTION;

CREATE TABLE `SiteTouristiqueLieuPhotos` (
    `IdSiteTouristiqueLieuPhoto` int NOT NULL AUTO_INCREMENT,
    `IdSiteTouristique` int NOT NULL,
    `PhotoData` mediumblob NOT NULL,
    `Ordre` int NOT NULL,
    `OriginalFileName` varchar(100) CHARACTER SET utf8mb4 NULL,
    `TypeMIME` varchar(50) CHARACTER SET utf8mb4 NULL,
    `FileSize` bigint NULL,
    `Statut` tinyint(1) NOT NULL DEFAULT TRUE,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_SiteTouristiqueLieuPhotos` PRIMARY KEY (`IdSiteTouristiqueLieuPhoto`),
    CONSTRAINT `FK_SiteTouristiqueLieuPhotos_SiteTouristiques_IdSiteTouristique`
        FOREIGN KEY (`IdSiteTouristique`) REFERENCES `SiteTouristiques` (`IdSiteTouristique`)
        ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_SiteTouristiqueLieuPhotos_IdSiteTouristique`
    ON `SiteTouristiqueLieuPhotos` (`IdSiteTouristique`);

CREATE UNIQUE INDEX `IX_SiteTouristiqueLieuPhotos_Lieu_Ordre_UQ`
    ON `SiteTouristiqueLieuPhotos` (`IdSiteTouristique`, `Ordre`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260815162926_AddSiteTouristiqueLieuPhotos', '6.0.25');

COMMIT;
