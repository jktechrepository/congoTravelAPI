-- =============================================================================
-- CongoTravel — RestaurantPhotos (max 3 photos / établissement)
-- Migration : 20260815164009_AddRestaurantPhotos
-- Prérequis : table Restaurants déjà présente
-- =============================================================================
-- À exécuter UNE SEULE FOIS. Vérifier avant :
--   SELECT * FROM `__EFMigrationsHistory`
--     WHERE `MigrationId` = '20260815164009_AddRestaurantPhotos';
-- =============================================================================

START TRANSACTION;

CREATE TABLE `RestaurantPhotos` (
    `IdRestaurantPhoto` int NOT NULL AUTO_INCREMENT,
    `IdRestaurant` int NOT NULL,
    `PhotoData` mediumblob NOT NULL,
    `Ordre` int NOT NULL,
    `OriginalFileName` varchar(100) CHARACTER SET utf8mb4 NULL,
    `TypeMIME` varchar(50) CHARACTER SET utf8mb4 NULL,
    `FileSize` bigint NULL,
    `Statut` tinyint(1) NOT NULL DEFAULT TRUE,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_RestaurantPhotos` PRIMARY KEY (`IdRestaurantPhoto`),
    CONSTRAINT `FK_RestaurantPhotos_Restaurants_IdRestaurant`
        FOREIGN KEY (`IdRestaurant`) REFERENCES `Restaurants` (`IdRestaurant`)
        ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_RestaurantPhotos_IdRestaurant`
    ON `RestaurantPhotos` (`IdRestaurant`);

CREATE UNIQUE INDEX `IX_RestaurantPhotos_Restaurant_Ordre_UQ`
    ON `RestaurantPhotos` (`IdRestaurant`, `Ordre`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260815164009_AddRestaurantPhotos', '6.0.25');

COMMIT;
