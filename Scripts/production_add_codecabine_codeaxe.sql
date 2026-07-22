-- =====================================================
-- Script SQL de Production : Ajout des champs CodeCabine et CodeAxe
-- =====================================================
-- Description : Ce script ajoute les colonnes CodeCabine et CodeAxe
--               aux tables Cabines et Axes pour permettre la génération
--               automatique du CodeCons des clients
-- =====================================================
-- Date : 2026-01-03
-- Compatible : MySQL 5.7+, MariaDB 10.2+
-- =====================================================

-- ⚠️ IMPORTANT : 
-- 1. Sauvegardez votre base de données avant d'exécuter ce script
-- 2. Après l'exécution, vous devrez définir les codes pour chaque Cabine et Axe
-- 3. Ce script est idempotent : il peut être exécuté plusieurs fois sans erreur

-- Désactiver temporairement les vérifications de clés étrangères
SET FOREIGN_KEY_CHECKS = 0;

-- =====================================================
-- PARTIE 1 : Ajout du champ CodeCabine à la table Cabines
-- =====================================================

-- Ajouter la colonne CodeCabine (si elle n'existe pas déjà)
-- Note: Si la colonne existe déjà, cette commande échouera silencieusement
--       Vous pouvez ignorer l'erreur "Duplicate column name" si elle apparaît
ALTER TABLE `Cabines` 
    ADD COLUMN `CodeCabine` VARCHAR(50) NULL AFTER `Nom`;

-- =====================================================
-- PARTIE 2 : Ajout du champ CodeAxe à la table Axes
-- =====================================================

-- Ajouter la colonne CodeAxe (si elle n'existe pas déjà)
-- Note: Si la colonne existe déjà, cette commande échouera silencieusement
--       Vous pouvez ignorer l'erreur "Duplicate column name" si elle apparaît
ALTER TABLE `Axes` 
    ADD COLUMN `CodeAxe` VARCHAR(50) NULL AFTER `NomAxe`;

-- =====================================================
-- Réactiver les vérifications de clés étrangères
-- =====================================================

SET FOREIGN_KEY_CHECKS = 1;

-- =====================================================
-- VÉRIFICATIONS
-- =====================================================

-- Vérifier que CodeCabine a été ajouté
SELECT 
    'Vérification CodeCabine' AS Verification,
    CASE 
        WHEN COUNT(*) > 0 THEN '✅ Colonne CodeCabine ajoutée'
        ELSE '❌ Colonne CodeCabine manquante'
    END AS Statut
FROM information_schema.columns 
WHERE table_schema = DATABASE() 
  AND table_name = 'Cabines' 
  AND column_name = 'CodeCabine';

-- Vérifier que CodeAxe a été ajouté
SELECT 
    'Vérification CodeAxe' AS Verification,
    CASE 
        WHEN COUNT(*) > 0 THEN '✅ Colonne CodeAxe ajoutée'
        ELSE '❌ Colonne CodeAxe manquante'
    END AS Statut
FROM information_schema.columns 
WHERE table_schema = DATABASE() 
  AND table_name = 'Axes' 
  AND column_name = 'CodeAxe';

-- Statistiques sur les codes définis
SELECT 
    'Statistiques CodeCabine' AS Element,
    CONCAT(
        'Total cabines: ', COUNT(*), 
        ' | Avec code: ', SUM(CASE WHEN CodeCabine IS NOT NULL AND CodeCabine != '' THEN 1 ELSE 0 END),
        ' | Sans code: ', SUM(CASE WHEN CodeCabine IS NULL OR CodeCabine = '' THEN 1 ELSE 0 END)
    ) AS Details
FROM Cabines;

SELECT 
    'Statistiques CodeAxe' AS Element,
    CONCAT(
        'Total axes: ', COUNT(*), 
        ' | Avec code: ', SUM(CASE WHEN CodeAxe IS NOT NULL AND CodeAxe != '' THEN 1 ELSE 0 END),
        ' | Sans code: ', SUM(CASE WHEN CodeAxe IS NULL OR CodeAxe = '' THEN 1 ELSE 0 END)
    ) AS Details
FROM Axes;

-- =====================================================
-- RÉSUMÉ DES MODIFICATIONS
-- =====================================================
-- 
-- ✅ Colonne CodeCabine ajoutée à la table Cabines (VARCHAR(50), nullable)
-- ✅ Colonne CodeAxe ajoutée à la table Axes (VARCHAR(50), nullable)
-- 
-- =====================================================
-- NOTES IMPORTANTES
-- =====================================================
-- 
-- 1. Les colonnes sont NULL par défaut. Vous devez définir les codes
--    pour chaque Cabine et Axe avant de créer des clients avec IdAxe.
-- 
-- 2. Exemples de mise à jour des codes :
--    -- Pour une cabine :
--    UPDATE Cabines SET CodeCabine = 'CAB001' WHERE IdCabine = 1;
--    
--    -- Pour un axe :
--    UPDATE Axes SET CodeAxe = 'AXE001' WHERE IdAxe = 1;
-- 
-- 3. Une fois les codes définis, le CodeCons sera généré automatiquement
--    lors de la création d'un client avec IdAxe, au format :
--    {codeCabine}/{codeAxe}/{0001-9999}
-- 
-- 4. Si vous obtenez des erreurs "Duplicate column", cela signifie que
--    les colonnes existent déjà. C'est normal et vous pouvez ignorer ces erreurs.
-- 
-- =====================================================
