-- =============================================================================
-- AUDIT — Migrations EF attendues vs __EFMigrationsHistory
-- =============================================================================
-- Usage :
--   USE votre_base;
--   Exécuter ce script avant/après déploiement.
--   Toute ligne Statut = MANQUANT doit être corrigée (dotnet ef database update
--   ou script SQL idempotent dans Scripts/production_*.sql).
-- =============================================================================

SET @db := DATABASE();

SELECT m.MigrationId,
       m.ProductVersionAttendu,
       CASE WHEN h.MigrationId IS NOT NULL THEN 'OK' ELSE 'MANQUANT' END AS Statut,
       h.ProductVersion AS ProductVersionApplique
FROM (
    SELECT '20260507163135_InitialMigration' AS MigrationId, '6.0.25' AS ProductVersionAttendu
    UNION ALL SELECT '20260508131304_AddIdSiteToVoyages', '6.0.25'
    UNION ALL SELECT '20260508135505_MultiDevisePhase1', '6.0.25'
    UNION ALL SELECT '20260508141208_VoyageDeviseAndReportingPhase23', '6.0.25'
    UNION ALL SELECT '20260508151940_AddIdSocieteToDevisesMonetaires', '6.0.25'
    UNION ALL SELECT '20260508152532_AddUniqueDeviseBySociete', '6.0.25'
    UNION ALL SELECT '20260520071424_AddPhotoVehicules', '6.0.25'
    UNION ALL SELECT '20260520072606_PhotoVehiculeRenameFilePathToPhotoBase64', '6.0.25'
    UNION ALL SELECT '20260520083546_PhotoVehiculePhotoDataMediumBlob', '6.0.25'
    UNION ALL SELECT '20260524142738_FlexPayRegressionFoundation', '6.0.25'
    UNION ALL SELECT '20260524144823_FlexPayCallbackAndInfoPaiement', '6.0.25'
    UNION ALL SELECT '20260528110345_BilletValiditeMultiVoyages', '6.0.25'
    UNION ALL SELECT '20260528113255_PenaliteReaffectationBillet', '6.0.25'
    UNION ALL SELECT '20260528124139_LimiteReaffectationVoyage', '6.0.25'
    UNION ALL SELECT '20260530075224_SiteIsSitePrincipal', '6.0.25'
    UNION ALL SELECT '20260530094931_ConfigSocieteCentralizedRules', '6.0.25'
    UNION ALL SELECT '20260530121511_ConfigSocietePenalitePourcentage', '6.0.25'
    UNION ALL SELECT '20260531142422_PlanificationVoyageV1', '6.0.25'
    UNION ALL SELECT '20260608101418_OrigineOperationReservationPaiement', '6.0.25'
    UNION ALL SELECT '20260615121938_DestinationSocieteVillesUnique', '6.0.25'
    UNION ALL SELECT '20260618112928_SiteNumeroMobileMoney', '6.0.25'
    UNION ALL SELECT '20260618124839_ReversementSiteFlexPayPayOut', '6.0.25'
    UNION ALL SELECT '20260618133404_ReversementAutoPaiementElectronique', '6.0.25'
    UNION ALL SELECT '20260618134551_PourcentageReversementSiteConfig', '6.0.25'
    UNION ALL SELECT '20260618135910_FraisPlateformeConfig', '6.0.25'
    UNION ALL SELECT '20260618171505_MontAddPaieElectroniqueConfig', '6.0.25'
    UNION ALL SELECT '20260619134037_ClientAdresseClientOptional', '6.0.25'
) m
LEFT JOIN `__EFMigrationsHistory` h ON h.MigrationId = m.MigrationId
ORDER BY m.MigrationId;

SELECT COUNT(*) AS MigrationsManquantes
FROM (
    SELECT m.MigrationId
    FROM (
        SELECT '20260618171505_MontAddPaieElectroniqueConfig' AS MigrationId
        UNION ALL SELECT '20260619134037_ClientAdresseClientOptional'
    ) m
    LEFT JOIN `__EFMigrationsHistory` h ON h.MigrationId = m.MigrationId
    WHERE h.MigrationId IS NULL
) x;

-- Colonnes ConfigSocietes critiques pour l'API actuelle
SELECT COLUMN_NAME,
       CASE WHEN COUNT(*) > 0 THEN 'OK' ELSE 'MANQUANT' END AS Statut
FROM (
    SELECT 'MontAddPaieElectronique' AS COLUMN_NAME
    UNION ALL SELECT 'CodeDeviseMontAddPaieElectronique'
    UNION ALL SELECT 'PourcentageReversementSite'
    UNION ALL SELECT 'FraisPlateforme'
    UNION ALL SELECT 'AutoReversementPaiementElectronique'
) expected
LEFT JOIN INFORMATION_SCHEMA.COLUMNS c
    ON c.TABLE_SCHEMA = @db
   AND c.TABLE_NAME = 'ConfigSocietes'
   AND c.COLUMN_NAME = expected.COLUMN_NAME
GROUP BY COLUMN_NAME
ORDER BY COLUMN_NAME;
