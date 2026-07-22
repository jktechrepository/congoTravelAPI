-- =====================================================
-- Script de Diagnostic : Refactorisation vers le modèle Usage
-- =====================================================
-- Ce script permet de vérifier que la refactorisation a été appliquée correctement
-- =====================================================

-- 1. Vérifier que la table Usages existe
SELECT 
    'Table Usages' AS Verification,
    CASE 
        WHEN COUNT(*) > 0 THEN '✅ Table Usages existe'
        ELSE '❌ Table Usages n''existe pas'
    END AS Statut
FROM information_schema.tables 
WHERE table_schema = DATABASE() 
  AND table_name = 'Usages';

-- 2. Vérifier que la table ClientUsages existe
SELECT 
    'Table ClientUsages' AS Verification,
    CASE 
        WHEN COUNT(*) > 0 THEN '✅ Table ClientUsages existe'
        ELSE '❌ Table ClientUsages n''existe pas'
    END AS Statut
FROM information_schema.tables 
WHERE table_schema = DATABASE() 
  AND table_name = 'ClientUsages';

-- 3. Vérifier que ClientCategorieClients a été supprimée
SELECT 
    'Table ClientCategorieClients supprimée' AS Verification,
    CASE 
        WHEN COUNT(*) = 0 THEN '✅ Table ClientCategorieClients supprimée'
        ELSE '❌ Table ClientCategorieClients existe encore'
    END AS Statut
FROM information_schema.tables 
WHERE table_schema = DATABASE() 
  AND table_name = 'ClientCategorieClients';

-- 4. Vérifier que IdCategorieClient a été supprimé de Clients
SELECT 
    'Colonne IdCategorieClient supprimée de Clients' AS Verification,
    CASE 
        WHEN COUNT(*) = 0 THEN '✅ Colonne IdCategorieClient supprimée'
        ELSE '❌ Colonne IdCategorieClient existe encore'
    END AS Statut
FROM information_schema.columns 
WHERE table_schema = DATABASE() 
  AND table_name = 'Clients' 
  AND column_name = 'IdCategorieClient';

-- 5. Vérifier que IdCategorie a été supprimé de Factures
SELECT 
    'Colonne IdCategorie supprimée de Factures' AS Verification,
    CASE 
        WHEN COUNT(*) = 0 THEN '✅ Colonne IdCategorie supprimée'
        ELSE '❌ Colonne IdCategorie existe encore'
    END AS Statut
FROM information_schema.columns 
WHERE table_schema = DATABASE() 
  AND table_name = 'Factures' 
  AND column_name = 'IdCategorie';

-- 6. Vérifier que IdUsage existe dans Factures
SELECT 
    'Colonne IdUsage dans Factures' AS Verification,
    CASE 
        WHEN COUNT(*) > 0 THEN '✅ Colonne IdUsage existe'
        ELSE '❌ Colonne IdUsage n''existe pas'
    END AS Statut
FROM information_schema.columns 
WHERE table_schema = DATABASE() 
  AND table_name = 'Factures' 
  AND column_name = 'IdUsage';

-- 7. Vérifier que Usage a été supprimé de CategorieClients
SELECT 
    'Colonne Usage supprimée de CategorieClients' AS Verification,
    CASE 
        WHEN COUNT(*) = 0 THEN '✅ Colonne Usage supprimée'
        ELSE '❌ Colonne Usage existe encore'
    END AS Statut
FROM information_schema.columns 
WHERE table_schema = DATABASE() 
  AND table_name = 'CategorieClients' 
  AND column_name = 'Usage';

-- 8. Vérifier les contraintes de clés étrangères
SELECT 
    CONSTRAINT_NAME AS 'Contrainte',
    TABLE_NAME AS 'Table',
    REFERENCED_TABLE_NAME AS 'Table Référencée'
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME IN ('Usages', 'ClientUsages', 'Factures')
  AND REFERENCED_TABLE_NAME IS NOT NULL
ORDER BY TABLE_NAME, CONSTRAINT_NAME;

-- 9. Compter les usages créés
SELECT 
    COUNT(*) AS 'Nombre d''usages',
    COUNT(DISTINCT IdCategorieClient) AS 'Nombre de catégories avec usages'
FROM Usages;

-- 10. Compter les relations ClientUsage
SELECT 
    COUNT(*) AS 'Nombre de relations ClientUsage',
    COUNT(DISTINCT IdClient) AS 'Nombre de clients avec usages',
    COUNT(DISTINCT IdUsage) AS 'Nombre d''usages utilisés'
FROM ClientUsages;

-- 11. Vérifier les factures sans usage (IdUsage = 0)
SELECT 
    COUNT(*) AS 'Factures sans usage (IdUsage = 0)',
    CASE 
        WHEN COUNT(*) = 0 THEN '✅ Toutes les factures ont un usage'
        ELSE CONCAT('⚠️ ', COUNT(*), ' facture(s) sans usage - À corriger')
    END AS Statut
FROM Factures
WHERE IdUsage = 0;
