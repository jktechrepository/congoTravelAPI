-- =============================================================================
-- CongoTravel — EvenementSessionPhotos (max 3 photos / session)
-- Migration : 20260723101605_EvenementSessionPhotos
-- Prérequis : table EvenementSessions déjà présente
-- =============================================================================
-- À exécuter UNE SEULE FOIS. Vérifier avant :
--   SELECT * FROM `__EFMigrationsHistory`
--     WHERE `MigrationId` = '20260723101605_EvenementSessionPhotos';
-- =============================================================================

START TRANSACTION;

CREATE TABLE `EvenementSessionPhotos` (
    `IdEvenementSessionPhoto` int NOT NULL AUTO_INCREMENT,
    `IdEvenementSession` int NOT NULL,
    `PhotoData` mediumblob NOT NULL,
    `Ordre` int NOT NULL,
    `OriginalFileName` varchar(100) CHARACTER SET utf8mb4 NULL,
    `TypeMIME` varchar(50) CHARACTER SET utf8mb4 NULL,
    `FileSize` bigint NULL,
    `Statut` tinyint(1) NOT NULL DEFAULT TRUE,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_EvenementSessionPhotos` PRIMARY KEY (`IdEvenementSessionPhoto`),
    CONSTRAINT `FK_EvenementSessionPhotos_EvenementSessions_IdEvenementSession`
        FOREIGN KEY (`IdEvenementSession`) REFERENCES `EvenementSessions` (`IdEvenementSession`)
        ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_EvenementSessionPhotos_IdEvenementSession`
    ON `EvenementSessionPhotos` (`IdEvenementSession`);

CREATE UNIQUE INDEX `IX_EvenementSessionPhotos_Session_Ordre_UQ`
    ON `EvenementSessionPhotos` (`IdEvenementSession`, `Ordre`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260723101605_EvenementSessionPhotos', '6.0.25');

COMMIT;
