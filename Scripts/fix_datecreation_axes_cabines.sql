-- =====================================================
-- Script de Correction : DateCreation NULL dans Axes et Cabines
-- =====================================================
-- Description : Ce script corrige les valeurs NULL dans les colonnes
--               DateCreation des tables Axes et Cabines, et ajoute
--               une valeur par défaut pour les futures insertions
-- =====================================================
-- Date : 2026-01-03
-- =====================================================

-- ⚠️ IMPORTANT : 
-- 1. Sauvegardez votre base de données avant d'exécuter ce script
-- 2. Ce script corrige les données existantes et configure les valeurs par défaut

-- Désactiver temporairement les vérifications de clés étrangères
SET FOREIGN_KEY_CHECKS = 0;

-- =====================================================
-- PARTIE 1 : Correction de la table Axes
-- =====================================================

-- 1.1. Mettre à jour toutes les valeurs NULL avec la date actuelle
UPDATE `Axes` 
SET `DateCreation` = NOW() 
WHERE `DateCreation` IS NULL;

-- 1.2. Modifier la colonne pour ajouter une valeur par défaut
ALTER TABLE `Axes` 
    MODIFY COLUMN `DateCreation` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6);

-- =====================================================
-- PARTIE 2 : Correction de la table Cabines
-- =====================================================

-- 2.1. Mettre à jour toutes les valeurs NULL avec la date actuelle
UPDATE `Cabines` 
SET `DateCreation` = NOW() 
WHERE `DateCreation` IS NULL;

-- 2.2. Modifier la colonne pour ajouter une valeur par défaut
ALTER TABLE `Cabines` 
    MODIFY COLUMN `DateCreation` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6);

-- =====================================================
-- Réactiver les vérifications de clés étrangères
-- =====================================================

SET FOREIGN_KEY_CHECKS = 1;

-- =====================================================
-- VÉRIFICATIONS
-- =====================================================

-- Vérifier qu'il n'y a plus de valeurs NULL dans Axes
SELECT 
    'Vérification Axes - DateCreation NULL' AS Verification,
    CASE 
        WHEN COUNT(*) = 0 THEN '✅ Aucune valeur NULL trouvée'
        ELSE CONCAT('❌ ', COUNT(*), ' valeur(s) NULL trouvée(s)')
    END AS Statut
FROM `Axes` 
WHERE `DateCreation` IS NULL;

-- Vérifier qu'il n'y a plus de valeurs NULL dans Cabines
SELECT 
    'Vérification Cabines - DateCreation NULL' AS Verification,
    CASE 
        WHEN COUNT(*) = 0 THEN '✅ Aucune valeur NULL trouvée'
        ELSE CONCAT('❌ ', COUNT(*), ' valeur(s) NULL trouvée(s)')
    END AS Statut
FROM `Cabines` 
WHERE `DateCreation` IS NULL;

-- Statistiques Axes
SELECT 
    'Statistiques Axes' AS Element,
    CONCAT(
        'Total axes: ', COUNT(*), 
        ' | Avec DateCreation: ', SUM(CASE WHEN DateCreation IS NOT NULL THEN 1 ELSE 0 END)
    ) AS Details
FROM `Axes`;

-- Statistiques Cabines
SELECT 
    'Statistiques Cabines' AS Element,
    CONCAT(
        'Total cabines: ', COUNT(*), 
        ' | Avec DateCreation: ', SUM(CASE WHEN DateCreation IS NOT NULL THEN 1 ELSE 0 END)
    ) AS Details
FROM `Cabines`;

-- =====================================================
-- Script terminé
-- =====================================================
-- 
-- ✅ Toutes les valeurs NULL ont été remplacées par la date actuelle
-- ✅ Les colonnes ont maintenant une valeur par défaut (CURRENT_TIMESTAMP)
-- ✅ Les futures insertions auront automatiquement une DateCreation
-- 
-- =====================================================
