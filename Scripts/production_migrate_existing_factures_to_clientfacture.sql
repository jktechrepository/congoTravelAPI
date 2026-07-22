-- ============================================================================
-- Script de Migration Production : Migrer les factures existantes vers ClientFactures
-- Date : 2025-01-05
-- Description : Crée des ClientFacture pour toutes les factures existantes
--               en calculant les montants depuis les paiements
-- IMPORTANT : Ce script est IDEMPOTENT - peut être exécuté plusieurs fois sans erreur
-- ============================================================================
--
-- AVANT D'EXÉCUTER:
-- 1. ⚠️  FAITES UN BACKUP COMPLET de votre base de données
-- 2. Testez ce script sur une base de données de test d'abord
-- 3. Vérifiez que vous êtes connecté à la bonne base de données
-- 4. Exécutez pendant une période de faible activité si possible
--
-- DURÉE ESTIMÉE: 5-15 minutes selon le volume de données
-- ============================================================================

SET FOREIGN_KEY_CHECKS = 0;
SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET AUTOCOMMIT = 0;
START TRANSACTION;
SET time_zone = "+00:00";

-- ============================================================================
-- PARTIE 1: VÉRIFICATIONS PRÉALABLES
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '🔍 VÉRIFICATIONS PRÉALABLES' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Vérifier que la table ClientFactures existe
SELECT 
    CASE 
        WHEN COUNT(*) > 0 THEN '✅ Table ClientFactures existe'
        ELSE '❌ ERREUR: Table ClientFactures n''existe pas. Exécutez d''abord la migration AddClientFacture'
    END as Statut_Table
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'ClientFactures';

-- Compter les factures existantes
SELECT 
    '📊 Factures existantes:' as Info;
SELECT 
    COUNT(*) as Nombre_Factures,
    COUNT(DISTINCT IdUsage) as Nombre_Usages_Differents
FROM Factures
WHERE Statut = 1;

-- Compter les ClientFactures déjà créées
SELECT 
    '📊 ClientFactures déjà créées:' as Info;
SELECT 
    COUNT(*) as Nombre_ClientFactures_Existantes,
    COUNT(DISTINCT IdFacture) as Nombre_Factures_Deja_Migrees
FROM ClientFactures
WHERE IdFacture IS NOT NULL;

-- ============================================================================
-- PARTIE 2: MIGRATION DES DONNÉES
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '🚀 DÉBUT DE LA MIGRATION' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Pour chaque facture, créer les ClientFacture pour tous les clients ayant l'usage
-- Utilisation d'une procédure stockée temporaire ou d'une requête INSERT ... SELECT

INSERT INTO ClientFactures (
    IdFacture,
    IdClient,
    Montant,
    nombreBatiment,
    MontantPaye,
    MontantDu,
    Mois,
    Annees,
    DateEmission,
    EstArrierePreExistant,
    Statut,
    DateCreation
)
SELECT DISTINCT
    f.IdFacture,
    cu.IdClient,
    -- Calculer Montant = facture.Montant × nombreBatiment
    (f.Montant * COALESCE(cu.nombreBatiment, 1)) as Montant,
    cu.nombreBatiment as nombreBatiment,
    -- Calculer MontantPaye depuis la table Paiements
    COALESCE(
        (SELECT SUM(p.MontantPaye)
         FROM Paiements p
         WHERE p.IdFacture = f.IdFacture
           AND p.IdClient = cu.IdClient
           AND p.Statut IN ('Validé', 'true', 'True', 'TRUE')
        ),
        0
    ) as MontantPaye,
    -- Calculer MontantDu = Montant - MontantPaye
    (f.Montant * COALESCE(cu.nombreBatiment, 1)) - 
    COALESCE(
        (SELECT SUM(p.MontantPaye)
         FROM Paiements p
         WHERE p.IdFacture = f.IdFacture
           AND p.IdClient = cu.IdClient
           AND p.Statut IN ('Validé', 'true', 'True', 'TRUE')
        ),
        0
    ) as MontantDu,
    -- Mois depuis facture.MoisEmission (format "01", "02", etc.)
    LPAD(f.MoisEmission, 2, '0') as Mois,
    f.AnneesEmission as Annees,
    f.DateEmission as DateEmission,
    FALSE as EstArrierePreExistant,
    TRUE as Statut,
    NOW(6) as DateCreation
FROM Factures f
INNER JOIN ClientUsages cu ON cu.IdUsage = f.IdUsage
INNER JOIN Clients c ON c.IdClient = cu.IdClient
WHERE f.Statut = 1
  AND cu.Statut = 1
  AND c.Statut = 1
  -- Éviter les doublons : ne créer que si la ClientFacture n'existe pas déjà
  AND NOT EXISTS (
      SELECT 1
      FROM ClientFactures cf
      WHERE cf.IdFacture = f.IdFacture
        AND cf.IdClient = cu.IdClient
  )
ORDER BY f.IdFacture, cu.IdClient;

-- Afficher le nombre de ClientFactures créées
SELECT 
    '✅ ClientFactures créées:' as Info;
SELECT 
    ROW_COUNT() as Nombre_ClientFactures_Creees;

-- ============================================================================
-- PARTIE 3: VÉRIFICATION POST-MIGRATION
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '✅ VÉRIFICATION POST-MIGRATION' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Compter les ClientFactures créées
SELECT 
    '📊 Statistiques finales:' as Info;
SELECT 
    COUNT(*) as Total_ClientFactures,
    COUNT(DISTINCT IdFacture) as Nombre_Factures_Avec_ClientFactures,
    COUNT(DISTINCT IdClient) as Nombre_Clients_Avec_ClientFactures,
    SUM(CASE WHEN MontantDu > 0 THEN 1 ELSE 0 END) as Nombre_ClientFactures_Avec_Arrieres,
    SUM(MontantDu) as Total_Arrieres
FROM ClientFactures
WHERE IdFacture IS NOT NULL
  AND Statut = 1;

-- Vérifier la cohérence : comparer MontantPaye dans ClientFacture vs SUM(Paiements)
SELECT 
    '🔍 Vérification de cohérence MontantPaye:' as Info;
SELECT 
    cf.IdClientFacture,
    cf.IdFacture,
    cf.IdClient,
    cf.MontantPaye as MontantPaye_ClientFacture,
    COALESCE(
        (SELECT SUM(p.MontantPaye)
         FROM Paiements p
         WHERE p.IdFacture = cf.IdFacture
           AND p.IdClient = cf.IdClient
           AND p.Statut IN ('Validé', 'true', 'True', 'TRUE')
        ),
        0
    ) as MontantPaye_Calcule,
    ABS(cf.MontantPaye - COALESCE(
        (SELECT SUM(p.MontantPaye)
         FROM Paiements p
         WHERE p.IdFacture = cf.IdFacture
           AND p.IdClient = cf.IdClient
           AND p.Statut IN ('Validé', 'true', 'True', 'TRUE')
        ),
        0
    )) as Difference
FROM ClientFactures cf
WHERE cf.IdFacture IS NOT NULL
  AND ABS(cf.MontantPaye - COALESCE(
        (SELECT SUM(p.MontantPaye)
         FROM Paiements p
         WHERE p.IdFacture = cf.IdFacture
           AND p.IdClient = cf.IdClient
           AND p.Statut IN ('Validé', 'true', 'True', 'TRUE')
        ),
        0
    )) > 0.01  -- Tolérance de 0.01 pour les arrondis
LIMIT 10;

-- Afficher un résumé
SELECT 
    CASE 
        WHEN COUNT(*) = 0 THEN '✅ Aucune incohérence détectée'
        ELSE CONCAT('⚠️  ', COUNT(*), ' incohérence(s) détectée(s) - Vérifiez les résultats ci-dessus')
    END as Statut_Cohérence
FROM (
    SELECT 
        cf.IdClientFacture,
        ABS(cf.MontantPaye - COALESCE(
            (SELECT SUM(p.MontantPaye)
             FROM Paiements p
             WHERE p.IdFacture = cf.IdFacture
               AND p.IdClient = cf.IdClient
               AND p.Statut IN ('Validé', 'true', 'True', 'TRUE')
            ),
            0
        )) as Difference
    FROM ClientFactures cf
    WHERE cf.IdFacture IS NOT NULL
) as Incoherences
WHERE Difference > 0.01;

-- ============================================================================
-- PARTIE 4: VALIDATION FINALE
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '📋 VALIDATION FINALE' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Vérifier que toutes les factures actives ont au moins une ClientFacture
SELECT 
    '📊 Factures sans ClientFacture:' as Info;
SELECT 
    f.IdFacture,
    f.NumeroFacture,
    f.MoisEmission,
    f.AnneesEmission,
    f.IdUsage
FROM Factures f
WHERE f.Statut = 1
  AND NOT EXISTS (
      SELECT 1
      FROM ClientFactures cf
      WHERE cf.IdFacture = f.IdFacture
  )
LIMIT 20;

-- Afficher le résumé final
SELECT 
    CASE 
        WHEN COUNT(*) = 0 THEN '✅ Toutes les factures actives ont au moins une ClientFacture'
        ELSE CONCAT('⚠️  ', COUNT(*), ' facture(s) active(s) sans ClientFacture (peut être normal si aucun client n''a l''usage)')
    END as Statut_Migration
FROM Factures f
WHERE f.Statut = 1
  AND NOT EXISTS (
      SELECT 1
      FROM ClientFactures cf
      WHERE cf.IdFacture = f.IdFacture
  );

-- ============================================================================
-- COMMIT OU ROLLBACK
-- ============================================================================

-- Si tout est OK, décommenter la ligne suivante pour valider la transaction
COMMIT;

-- En cas de problème, décommenter la ligne suivante pour annuler
-- ROLLBACK;

SET FOREIGN_KEY_CHECKS = 1;

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '✅ MIGRATION TERMINÉE' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
