-- =====================================================
-- Script SQL de Production : Renommer Actif en Statut dans CategorieClients
-- =====================================================
-- Description : Ce script renomme la colonne Actif en Statut dans la table CategorieClients
--               Si la colonne Actif n'existe pas, elle crée la colonne Statut
-- =====================================================
-- Date : 2026-01-03
-- Compatible : MySQL 5.7+, MariaDB 10.2+
-- =====================================================

-- ⚠️ IMPORTANT : 
-- 1. Sauvegardez votre base de données avant d'exécuter ce script
-- 2. Ce script est idempotent : il peut être exécuté plusieurs fois sans erreur

-- Désactiver temporairement les vérifications de clés étrangères
SET FOREIGN_KEY_CHECKS = 0;

-- =====================================================
-- Vérifier si la colonne Actif existe
-- =====================================================

SET @actif_exists = (
    SELECT COUNT(*) 
    FROM information_schema.columns 
    WHERE table_schema = DATABASE() 
      AND table_name = 'CategorieClients' 
      AND column_name = 'Actif'
);

SET @statut_exists = (
    SELECT COUNT(*) 
    FROM information_schema.columns 
    WHERE table_schema = DATABASE() 
      AND table_name = 'CategorieClients' 
      AND column_name = 'Statut'
);

-- =====================================================
-- Cas 1 : Actif existe, Statut n'existe pas -> Renommer
-- =====================================================

SET @sql_rename = IF(
    @actif_exists > 0 AND @statut_exists = 0,
    'ALTER TABLE `CategorieClients` CHANGE COLUMN `Actif` `Statut` TINYINT(1) NULL DEFAULT 1;',
    'SELECT "Renommage non nécessaire (Actif n''existe pas ou Statut existe déjà)" AS Message;'
);

PREPARE stmt FROM @sql_rename;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- =====================================================
-- Cas 2 : Ni Actif ni Statut n'existent -> Créer Statut
-- =====================================================

SET @sql_create = IF(
    @actif_exists = 0 AND @statut_exists = 0,
    'ALTER TABLE `CategorieClients` ADD COLUMN `Statut` TINYINT(1) NULL DEFAULT 1 AFTER `Description`;',
    'SELECT "Création non nécessaire (Statut existe déjà)" AS Message;'
);

PREPARE stmt FROM @sql_create;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- =====================================================
-- Cas 3 : Actif et Statut existent tous les deux -> Supprimer Actif
-- =====================================================

SET @sql_drop_actif = IF(
    @actif_exists > 0 AND @statut_exists > 0,
    'ALTER TABLE `CategorieClients` DROP COLUMN `Actif`;',
    'SELECT "Suppression non nécessaire" AS Message;'
);

PREPARE stmt FROM @sql_drop_actif;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- =====================================================
-- Réactiver les vérifications de clés étrangères
-- =====================================================

SET FOREIGN_KEY_CHECKS = 1;

-- =====================================================
-- VÉRIFICATIONS
-- =====================================================

-- Vérifier que Statut existe
SELECT 
    'Vérification Statut' AS Verification,
    CASE 
        WHEN COUNT(*) > 0 THEN '✅ Colonne Statut existe'
        ELSE '❌ Colonne Statut manquante'
    END AS Statut
FROM information_schema.columns 
WHERE table_schema = DATABASE() 
  AND table_name = 'CategorieClients' 
  AND column_name = 'Statut';

-- Vérifier qu'Actif n'existe plus
SELECT 
    'Vérification Actif supprimé' AS Verification,
    CASE 
        WHEN COUNT(*) = 0 THEN '✅ Colonne Actif supprimée'
        ELSE '⚠️ Colonne Actif existe encore'
    END AS Statut
FROM information_schema.columns 
WHERE table_schema = DATABASE() 
  AND table_name = 'CategorieClients' 
  AND column_name = 'Actif';

-- Statistiques sur les valeurs Statut
SELECT 
    'Statistiques Statut' AS Element,
    CONCAT(
        'Total catégories: ', COUNT(*), 
        ' | Statut = true: ', SUM(CASE WHEN Statut = 1 THEN 1 ELSE 0 END),
        ' | Statut = false: ', SUM(CASE WHEN Statut = 0 THEN 1 ELSE 0 END),
        ' | Statut = NULL: ', SUM(CASE WHEN Statut IS NULL THEN 1 ELSE 0 END)
    ) AS Details
FROM CategorieClients;

-- =====================================================
-- RÉSUMÉ DES MODIFICATIONS
-- =====================================================
-- 
-- ✅ Colonne Actif renommée en Statut (si Actif existait)
-- ✅ Colonne Statut créée (si ni Actif ni Statut n'existaient)
-- ✅ Colonne Actif supprimée (si Actif et Statut existaient tous les deux)
-- 
-- =====================================================
-- NOTES IMPORTANTES
-- =====================================================
-- 
-- 1. La colonne Statut est de type TINYINT(1) (booléen) et nullable
-- 2. La valeur par défaut est 1 (true = actif)
-- 3. Les valeurs NULL sont considérées comme actives (true) dans la logique de l'API
-- 
-- =====================================================
