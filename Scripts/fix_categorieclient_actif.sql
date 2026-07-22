-- =====================================================
-- Script de Diagnostic et Correction : Actif dans CategorieClients
-- =====================================================
-- Description : Ce script vérifie et corrige les valeurs NULL
--               dans la colonne Actif de la table CategorieClients
-- =====================================================

-- =====================================================
-- 1. DIAGNOSTIC : Vérifier l'état actuel
-- =====================================================

-- Compter les catégories par statut Actif
SELECT 
    'Diagnostic Actif' AS Element,
    CASE 
        WHEN Actif IS NULL THEN 'NULL'
        WHEN Actif = 1 THEN 'true'
        WHEN Actif = 0 THEN 'false'
        ELSE 'Autre'
    END AS StatutActif,
    COUNT(*) AS Nombre
FROM CategorieClients
GROUP BY Actif;

-- Afficher toutes les catégories avec leur statut Actif
SELECT 
    IdCategorie,
    NomCategorie,
    IdSociete,
    CASE 
        WHEN Actif IS NULL THEN 'NULL'
        WHEN Actif = 1 THEN 'true'
        WHEN Actif = 0 THEN 'false'
    END AS Actif,
    DateCreation
FROM CategorieClients
ORDER BY NomCategorie;

-- =====================================================
-- 2. CORRECTION : Mettre à jour les valeurs NULL
-- =====================================================

-- Mettre à jour toutes les valeurs NULL avec true (actif par défaut)
UPDATE CategorieClients 
SET Actif = true 
WHERE Actif IS NULL;

-- =====================================================
-- 3. VÉRIFICATION : Confirmer la correction
-- =====================================================

-- Vérifier qu'il n'y a plus de valeurs NULL
SELECT 
    'Vérification Actif NULL' AS Verification,
    CASE 
        WHEN COUNT(*) = 0 THEN '✅ Aucune valeur NULL trouvée'
        ELSE CONCAT('❌ ', COUNT(*), ' valeur(s) NULL trouvée(s)')
    END AS Statut
FROM CategorieClients 
WHERE Actif IS NULL;

-- Statistiques finales
SELECT 
    'Statistiques finales' AS Element,
    CONCAT(
        'Total: ', COUNT(*), 
        ' | Actifs (true): ', SUM(CASE WHEN Actif = 1 THEN 1 ELSE 0 END),
        ' | Inactifs (false): ', SUM(CASE WHEN Actif = 0 THEN 1 ELSE 0 END),
        ' | NULL: ', SUM(CASE WHEN Actif IS NULL THEN 1 ELSE 0 END)
    ) AS Details
FROM CategorieClients;

-- =====================================================
-- Script terminé
-- =====================================================
-- 
-- ✅ Toutes les valeurs NULL ont été remplacées par true
-- ✅ Les catégories sont maintenant toutes actives par défaut
-- 
-- =====================================================
