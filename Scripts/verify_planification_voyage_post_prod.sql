-- =============================================================================
-- Post-vérifications — Migration PlanificationVoyageV1 (production)
-- Exécuter APRÈS Scripts/production_planification_voyage_v1.sql
-- =============================================================================

SELECT DATABASE() AS current_database;

-- 1. Colonne présente (attendu : 1 ligne, Type int, Null YES)
--    Si 0 ligne → exécuter production_planification_voyage_v1_patch_voyages_column.sql
SHOW COLUMNS FROM Voyages LIKE 'IdPlanificationVoyage';

-- 2. Tables créées (attendu : 4 tables)
SHOW TABLES LIKE 'Planification%';

-- 3. Migration enregistrée (attendu : 1 ligne)
SELECT MigrationId, ProductVersion
FROM __EFMigrationsHistory
WHERE MigrationId = '20260531142422_PlanificationVoyageV1';

-- 4. Voyages existants non impactés (attendu : tous NULL tant qu'aucune génération)
SELECT COUNT(*) AS total_voyages,
       SUM(CASE WHEN IdPlanificationVoyage IS NULL THEN 1 ELSE 0 END) AS sans_planification
FROM Voyages;

-- 5. Smoke test SQL équivalent endpoint paged (remplacer @idSociete)
-- SET @idSociete = 1;
-- SELECT IdVoyage, IdSociete, DateDepart, IdPlanificationVoyage
-- FROM Voyages
-- WHERE IdSociete = @idSociete
-- ORDER BY DateDepart, HeureDepart
-- LIMIT 10;
