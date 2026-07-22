-- =============================================================================
-- CongoTravel — PhotoVehicules
-- Création de la table PhotoVehicules + suppression de Vehicules.Photo
--
-- Base : MariaDB / MySQL (utf8mb4)
-- Table EF : PhotoVehicules (pluriel)
-- =============================================================================

START TRANSACTION;

-- -----------------------------------------------------------------------------
-- 1) Création de la table PhotoVehicules (schéma final aligné sur le modèle C#)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `PhotoVehicules` (
    `IdPhotoVehicule` int NOT NULL AUTO_INCREMENT,
    `IdVehicule` int NOT NULL,
    `PhotoData` mediumblob NOT NULL COMMENT 'Image binaire (JPEG/PNG)',
    `Ordre` int NOT NULL COMMENT 'Position 1, 2 ou 3',
    `OriginalFileName` varchar(100) CHARACTER SET utf8mb4 NULL,
    `TypeMIME` varchar(50) CHARACTER SET utf8mb4 NULL,
    `FileSize` bigint NULL,
    `Statut` tinyint(1) NOT NULL DEFAULT 1,
    `DateCreation` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_PhotoVehicules` PRIMARY KEY (`IdPhotoVehicule`),
    CONSTRAINT `FK_PhotoVehicules_Vehicules_IdVehicule`
        FOREIGN KEY (`IdVehicule`) REFERENCES `Vehicules` (`IdVehicule`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE INDEX `IX_PhotoVehicules_IdVehicule`
    ON `PhotoVehicules` (`IdVehicule`);

CREATE UNIQUE INDEX `IX_PhotoVehicules_Vehicule_Ordre_Unique`
    ON `PhotoVehicules` (`IdVehicule`, `Ordre`);

-- -----------------------------------------------------------------------------
-- 2) Migration des anciennes photos (Vehicules.Photo -> PhotoVehicules)
--    Exécuter uniquement si la colonne Photo existe encore sur Vehicules.
--    Si erreur "Unknown column Photo", passer directement à l'étape 3.
-- -----------------------------------------------------------------------------
INSERT INTO `PhotoVehicules` (
    `IdVehicule`,
    `PhotoData`,
    `Ordre`,
    `Statut`,
    `DateCreation`,
    `TypeMIME`
)
SELECT
    v.`IdVehicule`,
    FROM_BASE64(v.`Photo`),
    1,
    1,
    NOW(6),
    'image/jpeg'
FROM `Vehicules` v
WHERE v.`Photo` IS NOT NULL
  AND TRIM(v.`Photo`) <> ''
  AND NOT EXISTS (
      SELECT 1 FROM `PhotoVehicules` p
      WHERE p.`IdVehicule` = v.`IdVehicule` AND p.`Ordre` = 1
  );

-- -----------------------------------------------------------------------------
-- 3) Suppression de l'ancienne colonne Photo sur Vehicules
--    Si erreur "Can't DROP COLUMN Photo", la colonne est déjà supprimée.
-- -----------------------------------------------------------------------------
ALTER TABLE `Vehicules` DROP COLUMN `Photo`;

COMMIT;

-- -----------------------------------------------------------------------------
-- Vérifications
-- -----------------------------------------------------------------------------
-- SHOW CREATE TABLE PhotoVehicules;
-- SHOW COLUMNS FROM Vehicules LIKE 'Photo';
-- SELECT COUNT(*) FROM PhotoVehicules;
