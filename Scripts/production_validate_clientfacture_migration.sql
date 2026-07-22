-- ============================================================================
-- Script de Validation : Vérifier la migration des ClientFactures
-- Date : 2025-01-05
-- Description : Valide la cohérence des données après migration
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '🔍 VALIDATION DE LA MIGRATION ClientFactures' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- ============================================================================
-- 1. VÉRIFICATION DE L'EXISTENCE DE LA TABLE
-- ============================================================================

SELECT 
    '1️⃣  Vérification de l''existence de la table:' as Section;
SELECT 
    CASE 
        WHEN COUNT(*) > 0 THEN '✅ Table ClientFactures existe'
        ELSE '❌ ERREUR: Table ClientFactures n''existe pas'
    END as Statut
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'ClientFactures';

-- ============================================================================
-- 2. STATISTIQUES GÉNÉRALES
-- ============================================================================

SELECT 
    '2️⃣  Statistiques générales:' as Section;
SELECT 
    COUNT(*) as Total_ClientFactures,
    COUNT(DISTINCT IdFacture) as Nombre_Factures_Avec_ClientFactures,
    COUNT(DISTINCT IdClient) as Nombre_Clients_Avec_ClientFactures,
    COUNT(CASE WHEN EstArrierePreExistant = 1 THEN 1 END) as Nombre_Arrieres_PreExistants,
    COUNT(CASE WHEN MontantDu > 0 THEN 1 END) as Nombre_ClientFactures_Avec_Arrieres,
    SUM(MontantDu) as Total_Arrieres,
    AVG(MontantDu) as Moyenne_Arrieres
FROM ClientFactures
WHERE Statut = 1;

-- ============================================================================
-- 3. VÉRIFICATION DE COHÉRENCE : MontantPaye
-- ============================================================================

SELECT 
    '3️⃣  Vérification de cohérence MontantPaye:' as Section;
SELECT 
    COUNT(*) as Total_ClientFactures_Verifiees,
    COUNT(CASE 
        WHEN ABS(cf.MontantPaye - COALESCE(
            (SELECT SUM(p.MontantPaye)
             FROM Paiements p
             WHERE p.IdFacture = cf.IdFacture
               AND p.IdClient = cf.IdClient
               AND p.Statut IN ('Validé', 'true', 'True', 'TRUE')
            ),
            0
        )) <= 0.01 THEN 1
    END) as ClientFactures_Coherentes,
    COUNT(CASE 
        WHEN ABS(cf.MontantPaye - COALESCE(
            (SELECT SUM(p.MontantPaye)
             FROM Paiements p
             WHERE p.IdFacture = cf.IdFacture
               AND p.IdClient = cf.IdClient
               AND p.Statut IN ('Validé', 'true', 'True', 'TRUE')
            ),
            0
        )) > 0.01 THEN 1
    END) as ClientFactures_Incoherentes
FROM ClientFactures cf
WHERE cf.IdFacture IS NOT NULL
  AND cf.Statut = 1;

-- Afficher les incohérences détectées (limité à 10)
SELECT 
    '⚠️  Incohérences détectées (top 10):' as Info;
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
  AND cf.Statut = 1
  AND ABS(cf.MontantPaye - COALESCE(
        (SELECT SUM(p.MontantPaye)
         FROM Paiements p
         WHERE p.IdFacture = cf.IdFacture
           AND p.IdClient = cf.IdClient
           AND p.Statut IN ('Validé', 'true', 'True', 'TRUE')
        ),
        0
    )) > 0.01
ORDER BY Difference DESC
LIMIT 10;

-- ============================================================================
-- 4. VÉRIFICATION DE COHÉRENCE : MontantDu
-- ============================================================================

SELECT 
    '4️⃣  Vérification de cohérence MontantDu:' as Section;
SELECT 
    COUNT(*) as Total_ClientFactures_Verifiees,
    COUNT(CASE 
        WHEN ABS(cf.MontantDu - (cf.Montant - cf.MontantPaye)) <= 0.01 THEN 1
    END) as ClientFactures_Coherentes,
    COUNT(CASE 
        WHEN ABS(cf.MontantDu - (cf.Montant - cf.MontantPaye)) > 0.01 THEN 1
    END) as ClientFactures_Incoherentes
FROM ClientFactures cf
WHERE cf.Montant IS NOT NULL
  AND cf.MontantPaye IS NOT NULL
  AND cf.MontantDu IS NOT NULL
  AND cf.Statut = 1;

-- ============================================================================
-- 5. VÉRIFICATION : Factures sans ClientFacture
-- ============================================================================

SELECT 
    '5️⃣  Factures actives sans ClientFacture:' as Section;
SELECT 
    COUNT(*) as Nombre_Factures_Sans_ClientFacture
FROM Factures f
WHERE f.Statut = 1
  AND NOT EXISTS (
      SELECT 1
      FROM ClientFactures cf
      WHERE cf.IdFacture = f.IdFacture
        AND cf.Statut = 1
  );

-- Afficher les factures sans ClientFacture (limité à 10)
SELECT 
    '📋 Liste des factures sans ClientFacture (top 10):' as Info;
SELECT 
    f.IdFacture,
    f.NumeroFacture,
    f.MoisEmission,
    f.AnneesEmission,
    f.IdUsage,
    f.Montant,
    (SELECT COUNT(*) 
     FROM ClientUsages cu 
     WHERE cu.IdUsage = f.IdUsage 
       AND cu.Statut = 1) as Nombre_Clients_Avec_Usage
FROM Factures f
WHERE f.Statut = 1
  AND NOT EXISTS (
      SELECT 1
      FROM ClientFactures cf
      WHERE cf.IdFacture = f.IdFacture
        AND cf.Statut = 1
  )
LIMIT 10;

-- ============================================================================
-- 6. VÉRIFICATION : Doublons potentiels
-- ============================================================================

SELECT 
    '6️⃣  Vérification des doublons:' as Section;
SELECT 
    COUNT(*) as Nombre_Doublons_Potentiels
FROM (
    SELECT 
        IdFacture,
        IdClient,
        COUNT(*) as Nombre
    FROM ClientFactures
    WHERE IdFacture IS NOT NULL
      AND Statut = 1
    GROUP BY IdFacture, IdClient
    HAVING COUNT(*) > 1
) as Doublons;

-- Afficher les doublons détectés
SELECT 
    '⚠️  Doublons détectés:' as Info;
SELECT 
    cf.IdFacture,
    cf.IdClient,
    COUNT(*) as Nombre_Doublons,
    GROUP_CONCAT(cf.IdClientFacture ORDER BY cf.IdClientFacture) as Ids_ClientFacture
FROM ClientFactures cf
WHERE cf.IdFacture IS NOT NULL
  AND cf.Statut = 1
GROUP BY cf.IdFacture, cf.IdClient
HAVING COUNT(*) > 1
LIMIT 10;

-- ============================================================================
-- 7. RÉSUMÉ FINAL
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '📊 RÉSUMÉ FINAL' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

SELECT 
    '✅ Migration validée' as Statut,
    (SELECT COUNT(*) FROM ClientFactures WHERE Statut = 1) as Total_ClientFactures,
    (SELECT COUNT(DISTINCT IdFacture) FROM ClientFactures WHERE IdFacture IS NOT NULL AND Statut = 1) as Factures_Migrees,
    (SELECT COUNT(*) FROM Factures WHERE Statut = 1) as Total_Factures_Actives,
    CASE 
        WHEN (SELECT COUNT(*) FROM Factures WHERE Statut = 1) = 
             (SELECT COUNT(DISTINCT IdFacture) FROM ClientFactures WHERE IdFacture IS NOT NULL AND Statut = 1)
        THEN '✅ Toutes les factures ont été migrées'
        ELSE CONCAT('⚠️  ', 
                   (SELECT COUNT(*) FROM Factures WHERE Statut = 1) - 
                   (SELECT COUNT(DISTINCT IdFacture) FROM ClientFactures WHERE IdFacture IS NOT NULL AND Statut = 1),
                   ' facture(s) non migrée(s)')
    END as Statut_Migration;
