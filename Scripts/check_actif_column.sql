-- =====================================================
-- Script de Diagnostic : Vérification de la colonne Actif
-- =====================================================
-- Description : Ce script vérifie si la colonne Actif existe
--               dans la table CategorieClients et affiche ses valeurs
-- =====================================================

-- Vérifier si la colonne Actif existe
SELECT 
    'Vérification colonne Actif' AS Verification,
    CASE 
        WHEN COUNT(*) > 0 THEN '✅ Colonne Actif existe'
        ELSE '❌ Colonne Actif n''existe pas'
    END AS Statut
FROM information_schema.columns 
WHERE table_schema = DATABASE() 
  AND table_name = 'CategorieClients' 
  AND column_name = 'Actif';

-- Afficher les valeurs Actif pour toutes les catégories
SELECT 
    IdCategorie,
    NomCategorie,
    Actif,
    CASE 
        WHEN Actif IS NULL THEN 'NULL'
        WHEN Actif = 1 THEN 'true'
        WHEN Actif = 0 THEN 'false'
        ELSE 'Autre'
    END AS ActifStatus
FROM CategorieClients
ORDER BY NomCategorie;

-- Statistiques
SELECT 
    'Statistiques Actif' AS Element,
    CONCAT(
        'Total: ', COUNT(*), 
        ' | Actif=true: ', SUM(CASE WHEN Actif = 1 THEN 1 ELSE 0 END),
        ' | Actif=false: ', SUM(CASE WHEN Actif = 0 THEN 1 ELSE 0 END),
        ' | Actif=NULL: ', SUM(CASE WHEN Actif IS NULL THEN 1 ELSE 0 END)
    ) AS Details
FROM CategorieClients;
