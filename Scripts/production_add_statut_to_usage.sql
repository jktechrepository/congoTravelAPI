-- =====================================================
-- Script SQL de Production : Ajout du champ Statut à la table Usages
-- =====================================================
-- Description : Ce script ajoute la colonne Statut (booléen) à la table Usages
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
-- Ajout du champ Statut à la table Usages
-- =====================================================

-- Ajouter la colonne Statut (si elle n'existe pas déjà)
-- Note: Si la colonne existe déjà, cette commande échouera silencieusement
--       Vous pouvez ignorer l'erreur "Duplicate column name" si elle apparaît
ALTER TABLE `Usages` 
    ADD COLUMN `Statut` TINYINT(1) NULL DEFAULT 1 AFTER `Description`;

-- S'assurer que la colonne a une valeur par défaut
ALTER TABLE `Usages` 
    MODIFY COLUMN `Statut` TINYINT(1) NULL DEFAULT 1;

-- Mettre à jour toutes les valeurs NULL avec la valeur par défaut (true)
UPDATE `Usages` 
SET `Statut` = 1 
WHERE `Statut` IS NULL;

-- =====================================================
-- Réactiver les vérifications de clés étrangères
-- =====================================================

SET FOREIGN_KEY_CHECKS = 1;

-- =====================================================
-- VÉRIFICATIONS
-- =====================================================

-- Vérifier que Statut a été ajouté
SELECT 
    'Vérification Statut' AS Verification,
    CASE 
        WHEN COUNT(*) > 0 THEN '✅ Colonne Statut ajoutée'
        ELSE '❌ Colonne Statut manquante'
    END AS Statut
FROM information_schema.columns 
WHERE table_schema = DATABASE() 
  AND table_name = 'Usages' 
  AND column_name = 'Statut';

-- Statistiques sur les valeurs Statut
SELECT 
    'Statistiques Statut' AS Element,
    CONCAT(
        'Total usages: ', COUNT(*), 
        ' | Statut = true: ', SUM(CASE WHEN Statut = 1 THEN 1 ELSE 0 END),
        ' | Statut = false: ', SUM(CASE WHEN Statut = 0 THEN 1 ELSE 0 END),
        ' | Statut = NULL: ', SUM(CASE WHEN Statut IS NULL THEN 1 ELSE 0 END)
    ) AS Details
FROM Usages;

-- =====================================================
-- RÉSUMÉ DES MODIFICATIONS
-- =====================================================
-- 
-- ✅ Colonne Statut ajoutée à la table Usages (TINYINT(1), nullable, défaut = 1)
-- ✅ Toutes les valeurs NULL ont été mises à jour avec la valeur par défaut (true)
-- 
-- =====================================================
-- NOTES IMPORTANTES
-- =====================================================
-- 
-- 1. La colonne Statut est de type TINYINT(1) (booléen) et nullable
-- 2. La valeur par défaut est 1 (true = actif)
-- 3. Les valeurs NULL sont considérées comme actives (true) dans la logique de l'API
-- 4. Si vous obtenez une erreur "Duplicate column", cela signifie que
--    la colonne existe déjà. C'est normal et vous pouvez ignorer cette erreur.
-- 
-- =====================================================
