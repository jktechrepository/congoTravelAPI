-- =====================================================
-- Script de Correction : DateCreation NULL dans Axes
-- =====================================================
-- Description : Ce script corrige les valeurs NULL dans la colonne
--               DateCreation de la table Axes
-- =====================================================

-- Désactiver temporairement les vérifications de clés étrangères
SET FOREIGN_KEY_CHECKS = 0;

-- =====================================================
-- 1. Mettre à jour toutes les valeurs NULL avec la date actuelle
-- =====================================================

UPDATE `Axes` 
SET `DateCreation` = NOW() 
WHERE `DateCreation` IS NULL;

-- =====================================================
-- 2. S'assurer que la colonne a une valeur par défaut
-- =====================================================

-- Modifier la colonne pour ajouter une valeur par défaut
ALTER TABLE `Axes` 
    MODIFY COLUMN `DateCreation` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6);

-- =====================================================
-- 3. Vérification
-- =====================================================

-- Vérifier qu'il n'y a plus de valeurs NULL
SELECT 
    'Vérification DateCreation NULL' AS Verification,
    CASE 
        WHEN COUNT(*) = 0 THEN '✅ Aucune valeur NULL trouvée'
        ELSE CONCAT('❌ ', COUNT(*), ' valeur(s) NULL trouvée(s)')
    END AS Statut
FROM `Axes` 
WHERE `DateCreation` IS NULL;

-- Afficher le nombre d'axes mis à jour
SELECT 
    COUNT(*) AS 'Axes avec DateCreation définie'
FROM `Axes` 
WHERE `DateCreation` IS NOT NULL;

-- =====================================================
-- Réactiver les vérifications de clés étrangères
-- =====================================================

SET FOREIGN_KEY_CHECKS = 1;

-- =====================================================
-- Script terminé
-- =====================================================
