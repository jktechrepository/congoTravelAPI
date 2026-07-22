-- =============================================================================
-- Correctif immédiat : colonne ConfigSocietes.DureeHoldEvenementMinutes
-- Erreur API : Unknown column 'DureeHoldEvenementMinutes' in 'field list'
-- (POST /api/Societe, création ConfigSociete bootstrap)
--
-- Cause : migration 20260703101713_EvenementTicketingV1 non appliquée
-- (souvent bloquée par un décalage FlexPay / __EFMigrationsHistory).
-- =============================================================================

SET @db := DATABASE();

SELECT
    'ConfigSocietes.DureeHoldEvenementMinutes' AS Objet,
    CASE WHEN EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = @db
          AND TABLE_NAME = 'ConfigSocietes'
          AND COLUMN_NAME = 'DureeHoldEvenementMinutes'
    ) THEN 'DEJA_PRESENTE' ELSE 'A_AJOUTER' END AS Etat;

SET @sql := IF(
    EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = @db
          AND TABLE_NAME = 'ConfigSocietes'
          AND COLUMN_NAME = 'DureeHoldEvenementMinutes'
    ),
    'SELECT ''Colonne déjà présente — rien à faire'' AS Info',
    'ALTER TABLE `ConfigSocietes` ADD `DureeHoldEvenementMinutes` int NOT NULL DEFAULT 15'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Ne pas stamp la migration complète ici si les tables Evenement* n'existent pas encore.
-- Après ce hotfix, exécuter reconcile_ef_migrations_history.sql puis
-- dotnet ef database update  OU  generated_evenement_migrations.sql

SELECT COLUMN_NAME, COLUMN_TYPE, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = @db
  AND TABLE_NAME = 'ConfigSocietes'
  AND COLUMN_NAME = 'DureeHoldEvenementMinutes';
