-- =============================================================================
-- Migration : PhotoBase64 (LONGTEXT) -> PhotoData (MEDIUMBLOB)
-- À exécuter si la table PhotoVehicules existe déjà avec PhotoBase64
-- =============================================================================

START TRANSACTION;

ALTER TABLE `PhotoVehicules`
    ADD COLUMN `PhotoData` mediumblob NULL AFTER `IdVehicule`;

UPDATE `PhotoVehicules`
SET `PhotoData` = FROM_BASE64(`PhotoBase64`)
WHERE `PhotoBase64` IS NOT NULL AND TRIM(`PhotoBase64`) <> '';

ALTER TABLE `PhotoVehicules`
    MODIFY COLUMN `PhotoData` mediumblob NOT NULL;

ALTER TABLE `PhotoVehicules`
    DROP COLUMN `PhotoBase64`;

COMMIT;

-- Vérification :
-- DESCRIBE PhotoVehicules;
