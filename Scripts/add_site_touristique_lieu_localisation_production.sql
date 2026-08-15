-- =============================================================================
-- CongoTravel — SiteTouristiques localisation / contact
-- Migration : 20260815172635_AddSiteTouristiqueLieuLocalisationFields
-- Prérequis : table SiteTouristiques déjà présente
-- =============================================================================
-- À exécuter UNE SEULE FOIS. Vérifier avant :
--   SELECT * FROM `__EFMigrationsHistory`
--     WHERE `MigrationId` = '20260815172635_AddSiteTouristiqueLieuLocalisationFields';
-- =============================================================================

START TRANSACTION;

ALTER TABLE `SiteTouristiques`
    ADD `Province` varchar(120) CHARACTER SET utf8mb4 NULL,
    ADD `Ville` varchar(120) CHARACTER SET utf8mb4 NULL,
    ADD `Adresse` varchar(500) CHARACTER SET utf8mb4 NULL,
    ADD `Telephone` varchar(30) CHARACTER SET utf8mb4 NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260815172635_AddSiteTouristiqueLieuLocalisationFields', '6.0.25');

COMMIT;
