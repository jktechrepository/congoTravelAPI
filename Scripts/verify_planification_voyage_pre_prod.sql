-- =============================================================================
-- Pré-vérifications — Migration PlanificationVoyageV1 (production)
-- Migration EF : 20260531142422_PlanificationVoyageV1
-- Exécuter AVANT Scripts/production_planification_voyage_v1.sql
-- =============================================================================

SELECT DATABASE() AS current_database, VERSION() AS mysql_version;

-- 1. Colonne absente (attendu avant migration : 0 ligne)
SHOW COLUMNS FROM Voyages LIKE 'IdPlanificationVoyage';

-- 2. Tables planification absentes (attendu avant migration : 0 ligne)
SHOW TABLES LIKE 'PlanificationsVoyage';
SHOW TABLES LIKE 'PlanificationVoyageEtapes';
SHOW TABLES LIKE 'PlanificationVoyageTarifs';
SHOW TABLES LIKE 'PlanificationGenerationLogs';

-- 3. Dernières migrations EF appliquées
SELECT MigrationId, ProductVersion
FROM __EFMigrationsHistory
ORDER BY MigrationId DESC
LIMIT 15;

-- 4. Migration PlanificationVoyage déjà enregistrée ? (attendu : 0 ligne)
SELECT MigrationId, ProductVersion
FROM __EFMigrationsHistory
WHERE MigrationId = '20260531142422_PlanificationVoyageV1';

-- Interprétation :
-- - Si la colonne IdPlanificationVoyage existe déjà → ne pas exécuter le script principal.
-- - Si la migration est dans l'historique mais la colonne manque → incohérence, contacter l'équipe dev.
-- - Si des migrations intermédiaires manquent (après ConfigSocietePenalitePourcentage) →
--   adapter la migration source lors de la régénération du script EF.
