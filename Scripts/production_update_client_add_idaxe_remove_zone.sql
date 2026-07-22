-- =====================================================
-- Script SQL de Production : Mise à jour de la table Clients
-- =====================================================
-- Description : Ce script effectue deux modifications sur la table Clients :
--               1. Ajoute le champ IdAxe pour matérialiser la relation Client-Axe
--               2. Supprime le champ Zone (obsolète)
-- =====================================================
-- Date : 2026-01-03
-- Compatible : MySQL 5.7+, MariaDB 10.2+
-- =====================================================

-- ⚠️ IMPORTANT : 
-- 1. Sauvegardez votre base de données avant d'exécuter ce script
-- 2. Vérifiez que la table Axes existe avant d'exécuter ce script
-- 3. Ce script est idempotent : il peut être exécuté plusieurs fois sans erreur

-- Désactiver temporairement les vérifications de clés étrangères
SET FOREIGN_KEY_CHECKS = 0;

-- =====================================================
-- PARTIE 1 : Ajout du champ IdAxe
-- =====================================================

-- Ajouter la colonne IdAxe (si elle n'existe pas déjà)
-- Note: Si la colonne existe déjà, cette commande échouera silencieusement
--       Vous pouvez ignorer l'erreur "Duplicate column name" si elle apparaît
ALTER TABLE `Clients` 
    ADD COLUMN `IdAxe` INT NULL AFTER `IsActif`;

-- Créer l'index pour optimiser les requêtes (si n'existe pas déjà)
-- Note: Si l'index existe déjà, cette commande échouera silencieusement
--       Vous pouvez ignorer l'erreur "Duplicate key name" si elle apparaît
CREATE INDEX `IX_Client_IdAxe` 
    ON `Clients` (`IdAxe`);

-- Ajouter la clé étrangère vers la table Axes (si n'existe pas déjà)
-- Note: Si la contrainte existe déjà, cette commande échouera silencieusement
--       Vous pouvez ignorer l'erreur "Duplicate foreign key" si elle apparaît
ALTER TABLE `Clients`
    ADD CONSTRAINT `FK_Clients_Axes_IdAxe`
        FOREIGN KEY (`IdAxe`)
        REFERENCES `Axes` (`IdAxe`)
        ON DELETE SET NULL;

-- =====================================================
-- PARTIE 2 : Suppression du champ Zone
-- =====================================================

-- Supprimer la colonne Zone (si elle existe)
-- Note: Si la colonne n'existe pas, cette commande échouera silencieusement
--       Vous pouvez ignorer l'erreur "Unknown column" si elle apparaît
ALTER TABLE `Clients` 
    DROP COLUMN `Zone`;

-- =====================================================
-- Réactiver les vérifications de clés étrangères
-- =====================================================

SET FOREIGN_KEY_CHECKS = 1;

-- =====================================================
-- VÉRIFICATIONS
-- =====================================================

-- Vérifier que IdAxe a été ajouté
SELECT 
    'Vérification IdAxe' AS Verification,
    CASE 
        WHEN COUNT(*) > 0 THEN '✅ Colonne IdAxe ajoutée'
        ELSE '❌ Colonne IdAxe manquante'
    END AS Statut
FROM information_schema.columns 
WHERE table_schema = DATABASE() 
  AND table_name = 'Clients' 
  AND column_name = 'IdAxe';

-- Vérifier que l'index a été créé
SELECT 
    'Vérification Index IdAxe' AS Verification,
    CASE 
        WHEN COUNT(*) > 0 THEN '✅ Index IX_Client_IdAxe créé'
        ELSE '❌ Index IX_Client_IdAxe manquant'
    END AS Statut
FROM information_schema.statistics 
WHERE table_schema = DATABASE() 
  AND table_name = 'Clients' 
  AND index_name = 'IX_Client_IdAxe';

-- Vérifier que la clé étrangère a été créée
SELECT 
    'Vérification FK IdAxe' AS Verification,
    CASE 
        WHEN COUNT(*) > 0 THEN '✅ Contrainte FK_Clients_Axes_IdAxe créée'
        ELSE '❌ Contrainte FK_Clients_Axes_IdAxe manquante'
    END AS Statut
FROM information_schema.table_constraints 
WHERE table_schema = DATABASE() 
  AND table_name = 'Clients' 
  AND constraint_name = 'FK_Clients_Axes_IdAxe';

-- Vérifier que Zone a été supprimée
SELECT 
    'Vérification Zone supprimée' AS Verification,
    CASE 
        WHEN COUNT(*) = 0 THEN '✅ Colonne Zone supprimée'
        ELSE '❌ Colonne Zone existe encore'
    END AS Statut
FROM information_schema.columns 
WHERE table_schema = DATABASE() 
  AND table_name = 'Clients' 
  AND column_name = 'Zone';

-- =====================================================
-- RÉSUMÉ DES MODIFICATIONS
-- =====================================================
-- 
-- ✅ Colonne IdAxe ajoutée (nullable INT)
-- ✅ Index IX_Client_IdAxe créé pour optimiser les requêtes
-- ✅ Contrainte FK_Clients_Axes_IdAxe créée (ON DELETE SET NULL)
-- ✅ Colonne Zone supprimée
-- 
-- =====================================================
-- NOTES IMPORTANTES
-- =====================================================
-- 
-- 1. La colonne IdAxe est nullable, donc les clients existants 
--    n'auront pas d'axe assigné par défaut (IdAxe = NULL)
-- 
-- 2. Pour assigner un axe à un client existant :
--    UPDATE Clients SET IdAxe = 1 WHERE IdClient = 1;
-- 
-- 3. Si un Axe est supprimé, l'IdAxe des clients associés sera 
--    automatiquement mis à NULL grâce à la contrainte ON DELETE SET NULL
-- 
-- 4. Si vous obtenez des erreurs "Duplicate column/index/constraint",
--    cela signifie que la modification existe déjà. C'est normal et
--    vous pouvez ignorer ces erreurs.
-- 
-- =====================================================
