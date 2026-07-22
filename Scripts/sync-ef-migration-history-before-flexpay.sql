-- À exécuter UNIQUEMENT si la base existe déjà mais __EFMigrationsHistory est vide
-- (erreur "Table 'auditlogs' already exists" sur dotnet ef database update).
-- Puis : dotnet ef database update --project CongoTravel.csproj
-- Ou exécuter Scripts/FlexPay-only-migrations.sql + les 2 INSERT finaux ci-dessous.

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`) VALUES
('20260507163135_InitialMigration', '6.0.25'),
('20260508131304_AddIdSiteToVoyages', '6.0.25'),
('20260508135505_MultiDevisePhase1', '6.0.25'),
('20260508141208_VoyageDeviseAndReportingPhase23', '6.0.25'),
('20260508151940_AddIdSocieteToDevisesMonetaires', '6.0.25'),
('20260508152532_AddUniqueDeviseBySociete', '6.0.25'),
('20260520071424_AddPhotoVehicules', '6.0.25'),
('20260520072606_PhotoVehiculeRenameFilePathToPhotoBase64', '6.0.25'),
('20260520083546_PhotoVehiculePhotoDataMediumBlob', '6.0.25');
