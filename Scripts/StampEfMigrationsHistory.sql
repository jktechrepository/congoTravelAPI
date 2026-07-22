-- =============================================================================
-- À utiliser si votre base a DÉJÀ toutes les tables SAUF PhotoVehicules,
-- mais que __EFMigrationsHistory est vide ou incomplet.
--
-- Après ce script, lancez :
--   dotnet ef database update --context CongoTravelDbContext
-- EF n'appliquera alors que les migrations PhotoVehicules (si pas déjà stampées).
-- =============================================================================

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES
    ('20260507163135_InitialMigration', '6.0.25'),
    ('20260508131304_AddIdSiteToVoyages', '6.0.25'),
    ('20260508135505_MultiDevisePhase1', '6.0.25'),
    ('20260508141208_VoyageDeviseAndReportingPhase23', '6.0.25'),
    ('20260508151940_AddIdSocieteToDevisesMonetaires', '6.0.25'),
    ('20260508152532_AddUniqueDeviseBySociete', '6.0.25');
