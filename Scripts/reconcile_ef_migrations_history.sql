-- =============================================================================
-- Réconciliation __EFMigrationsHistory ↔ schéma physique
-- Cas typique : colonne/table déjà créée (script SQL manuel) mais migration
-- non enregistrée → erreur "Duplicate column name 'StatutPaiementMetier'" au démarrage.
--
-- Usage :
--   USE votre_base;
--   SOURCE Scripts/reconcile_ef_migrations_history.sql;
--   Relancer l'API ou : dotnet ef database update
-- =============================================================================

SET @db := DATABASE();

-- -----------------------------------------------------------------------------
-- 1) Diagnostic rapide
-- -----------------------------------------------------------------------------
SELECT 'Paiements.StatutPaiementMetier' AS Objet,
       CASE WHEN EXISTS (
           SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
           WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'Paiements'
             AND COLUMN_NAME = 'StatutPaiementMetier'
       ) THEN 'PRESENT' ELSE 'ABSENT' END AS SchemaPhysique,
       CASE WHEN EXISTS (
           SELECT 1 FROM `__EFMigrationsHistory`
           WHERE MigrationId = '20260524142738_FlexPayRegressionFoundation'
       ) THEN 'STAMPED' ELSE 'MANQUANT' END AS HistoriqueEF;

SELECT MigrationId,
       CASE WHEN MigrationId IS NOT NULL THEN 'STAMPED' ELSE 'MANQUANT' END AS Statut
FROM (
    SELECT '20260524142738_FlexPayRegressionFoundation' AS expected
    UNION ALL SELECT '20260524144823_FlexPayCallbackAndInfoPaiement'
) e
LEFT JOIN `__EFMigrationsHistory` h ON h.MigrationId = e.expected;

-- -----------------------------------------------------------------------------
-- 2) FlexPayRegressionFoundation — stamp si schéma complet déjà présent
-- -----------------------------------------------------------------------------
INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
SELECT '20260524142738_FlexPayRegressionFoundation', '6.0.25'
FROM DUAL
WHERE EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'Paiements'
      AND COLUMN_NAME = 'StatutPaiementMetier'
)
AND EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'CommandesReservationEnAttente'
)
AND EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'SiegeHoldsEnAttente'
);

-- -----------------------------------------------------------------------------
-- 3) FlexPayCallbackAndInfoPaiement — stamp si tables présentes
-- -----------------------------------------------------------------------------
INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
SELECT '20260524144823_FlexPayCallbackAndInfoPaiement', '6.0.25'
FROM DUAL
WHERE EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'InfoPaiementsSociete'
)
AND EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'TransactionsFlexPay'
);

-- -----------------------------------------------------------------------------
-- 4) EvenementTicketingV1 — stamp si tables principales présentes
-- -----------------------------------------------------------------------------
INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
SELECT '20260703101713_EvenementTicketingV1', '6.0.25'
FROM DUAL
WHERE EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'EvenementSessions'
)
AND EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'ConfigSocietes'
      AND COLUMN_NAME = 'DureeHoldEvenementMinutes'
);

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
SELECT '20260703120104_EvenementSessionGlobalQuotaPricing', '6.0.25'
FROM DUAL
WHERE EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'EvenementSessionGlobalQuotas'
      AND COLUMN_NAME = 'PrixUnitaire'
);

-- -----------------------------------------------------------------------------
-- 5) État après réconciliation
-- -----------------------------------------------------------------------------
SELECT h.MigrationId, h.ProductVersion, 'OK' AS Statut
FROM `__EFMigrationsHistory` h
WHERE h.MigrationId IN (
    '20260524142738_FlexPayRegressionFoundation',
    '20260524144823_FlexPayCallbackAndInfoPaiement',
    '20260703101713_EvenementTicketingV1',
    '20260703120104_EvenementSessionGlobalQuotaPricing'
)
ORDER BY h.MigrationId;

SELECT 'Migrations EF encore MANQUANTES (audit complet)' AS Info;
-- Déléguer à audit_ef_migrations_history.sql pour la liste complète
