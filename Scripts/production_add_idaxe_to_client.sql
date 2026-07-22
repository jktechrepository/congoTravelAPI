-- =====================================================
-- Script SQL de Production : Ajout du champ IdAxe au modèle Client
-- =====================================================
-- Description : Ce script ajoute le champ IdAxe dans la table Clients
--               pour matérialiser la relation entre Client et Axe
-- =====================================================

-- Désactiver temporairement les vérifications de clés étrangères
SET FOREIGN_KEY_CHECKS = 0;

-- =====================================================
-- 1. Ajouter la colonne IdAxe à la table Clients
-- =====================================================

ALTER TABLE `Clients` 
    ADD COLUMN `IdAxe` INT NULL AFTER `IsActif`;

-- =====================================================
-- 2. Créer l'index pour optimiser les requêtes
-- =====================================================

CREATE INDEX `IX_Client_IdAxe` 
    ON `Clients` (`IdAxe`);

-- =====================================================
-- 3. Ajouter la clé étrangère vers la table Axes
-- =====================================================

ALTER TABLE `Clients`
    ADD CONSTRAINT `FK_Clients_Axes_IdAxe`
        FOREIGN KEY (`IdAxe`)
        REFERENCES `Axes` (`IdAxe`)
        ON DELETE SET NULL;

-- =====================================================
-- 4. Notes importantes
-- =====================================================
-- 
-- ⚠️ ATTENTION : 
-- - La colonne IdAxe est nullable, donc les clients existants n'auront pas d'axe assigné
-- - Vous pouvez assigner des axes aux clients existants via l'API ou directement en SQL
-- 
-- Exemple pour assigner un axe à un client :
--   UPDATE Clients SET IdAxe = 1 WHERE IdClient = 1;
-- 
-- =====================================================

-- Réactiver les vérifications de clés étrangères
SET FOREIGN_KEY_CHECKS = 1;

-- =====================================================
-- Script terminé
-- =====================================================
-- Vérifiez que la colonne et la contrainte ont été créées correctement
-- en exécutant les requêtes de diagnostic ci-dessous.
-- =====================================================
