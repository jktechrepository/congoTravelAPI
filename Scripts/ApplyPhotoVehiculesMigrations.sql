-- =============================================================================
-- Appliquer UNIQUEMENT les migrations PhotoVehicules sur une base EXISTANTE
-- (schéma déjà créé manuellement ou sans __EFMigrationsHistory à jour)
--
-- Base cible : dev-congotravel (voir appsettings.Development.json)
-- Exécuter dans MySQL Workbench, DBeaver, TablePlus, etc.
-- =============================================================================

START TRANSACTION;

-- 1) Créer la table PhotoVehicules
CREATE TABLE IF NOT EXISTS `PhotoVehicules` (
    `IdPhotoVehicule` int NOT NULL AUTO_INCREMENT,
    `IdVehicule` int NOT NULL,
    `FilePath` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `Ordre` int NOT NULL,
    `OriginalFileName` varchar(100) CHARACTER SET utf8mb4 NULL,
    `TypeMIME` varchar(50) CHARACTER SET utf8mb4 NULL,
    `FileSize` bigint NULL,
    `Statut` tinyint(1) NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_PhotoVehicules` PRIMARY KEY (`IdPhotoVehicule`),
    CONSTRAINT `FK_PhotoVehicules_Vehicules_IdVehicule` FOREIGN KEY (`IdVehicule`) REFERENCES `Vehicules` (`IdVehicule`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX IF NOT EXISTS `IX_PhotoVehicules_IdVehicule` ON `PhotoVehicules` (`IdVehicule`);
CREATE UNIQUE INDEX IF NOT EXISTS `IX_PhotoVehicules_Vehicule_Ordre_Unique` ON `PhotoVehicules` (`IdVehicule`, `Ordre`);

-- 2) Migrer l'ancienne colonne Vehicules.Photo (si elle existe encore)
--    Ignorer l'erreur si la colonne Photo n'existe plus.
INSERT INTO PhotoVehicules (IdVehicule, FilePath, Ordre, Statut, DateCreation, TypeMIME)
SELECT IdVehicule, Photo, 1, 1, NOW(), 'image/jpeg'
FROM Vehicules
WHERE Photo IS NOT NULL AND TRIM(Photo) <> '';

-- 3) Supprimer l'ancienne colonne Photo sur Vehicules (si présente)
ALTER TABLE `Vehicules` DROP COLUMN `Photo`;

-- 4) Renommer FilePath -> PhotoBase64 et passer en longtext
ALTER TABLE `PhotoVehicules` RENAME COLUMN `FilePath` TO `PhotoBase64`;
ALTER TABLE `PhotoVehicules` MODIFY COLUMN `PhotoBase64` longtext CHARACTER SET utf8mb4 NOT NULL;

-- 5) Enregistrer les migrations dans l'historique EF (évite un re-run accidentel)
INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES
    ('20260520071424_AddPhotoVehicules', '6.0.25'),
    ('20260520072606_PhotoVehiculeRenameFilePathToPhotoBase64', '6.0.25');

COMMIT;

-- Vérification :
-- SHOW TABLES LIKE 'PhotoVehicules';
-- DESCRIBE PhotoVehicules;
