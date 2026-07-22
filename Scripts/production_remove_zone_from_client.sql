-- =====================================================
-- Script SQL de Production : Retrait du champ Zone du modèle Client
-- =====================================================
-- Description : Ce script supprime la colonne Zone de la table Clients
-- =====================================================

-- Désactiver temporairement les vérifications de clés étrangères
SET FOREIGN_KEY_CHECKS = 0;

-- =====================================================
-- 1. Supprimer la colonne Zone de la table Clients
-- =====================================================

ALTER TABLE `Clients` 
    DROP COLUMN IF EXISTS `Zone`;

-- =====================================================
-- Script terminé
-- =====================================================
-- Vérifiez que la colonne a été supprimée correctement
-- en exécutant les requêtes de diagnostic ci-dessous.
-- =====================================================

-- Réactiver les vérifications de clés étrangères
SET FOREIGN_KEY_CHECKS = 1;

-- =====================================================
-- Vérification
-- =====================================================
-- Vérifier que la colonne Zone n'existe plus
SELECT 
    'Colonne Zone supprimée de Clients' AS Verification,
    CASE 
        WHEN COUNT(*) = 0 THEN '✅ Colonne Zone supprimée'
        ELSE '❌ Colonne Zone existe encore'
    END AS Statut
FROM information_schema.columns 
WHERE table_schema = DATABASE() 
  AND table_name = 'Clients' 
  AND column_name = 'Zone';
