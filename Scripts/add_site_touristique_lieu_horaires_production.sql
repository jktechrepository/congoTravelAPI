-- =============================================================================
-- CongoTravel — SiteTouristiques horaires d'ouverture (V1)
-- Migration : 20260815173814_AddSiteTouristiqueLieuHorairesFields
-- Prérequis : table SiteTouristiques déjà présente
-- =============================================================================
-- À exécuter UNE SEULE FOIS. Vérifier avant :
--   SELECT * FROM `__EFMigrationsHistory`
--     WHERE `MigrationId` = '20260815173814_AddSiteTouristiqueLieuHorairesFields';
-- =============================================================================

START TRANSACTION;

ALTER TABLE `SiteTouristiques`
    ADD `HeureOuverture` time NULL,
    ADD `HeureFermeture` time NULL,
    ADD `JourOuverture` varchar(100) CHARACTER SET utf8mb4 NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260815173814_AddSiteTouristiqueLieuHorairesFields', '6.0.25');

COMMIT;
