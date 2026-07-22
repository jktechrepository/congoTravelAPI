-- =====================================================
-- Script SQL de Production : Refactorisation vers le modèle Usage
-- =====================================================
-- Description : Ce script adapte la base de données pour le nouveau modèle
--               où les clients sont liés aux usages (via ClientUsage) au lieu
--               des catégories (via ClientCategorieClient).
-- 
-- IMPORTANT : Ce script est conçu pour une NOUVELLE BASE DE DONNÉES VIDE.
--             Si vous avez des données existantes, vous devez d'abord les migrer.
-- =====================================================

-- Désactiver temporairement les vérifications de clés étrangères
SET FOREIGN_KEY_CHECKS = 0;

-- =====================================================
-- 1. Supprimer les anciennes structures
-- =====================================================

-- Supprimer la table ClientCategorieClients (remplacée par ClientUsages)
DROP TABLE IF EXISTS `ClientCategorieClients`;

-- Supprimer les colonnes obsolètes
ALTER TABLE `Clients` DROP COLUMN IF EXISTS `IdCategorieClient`;
ALTER TABLE `Factures` DROP COLUMN IF EXISTS `IdCategorie`;
ALTER TABLE `CategorieClients` DROP COLUMN IF EXISTS `Usage`;

-- Supprimer les index obsolètes
DROP INDEX IF EXISTS `IX_Facture_Mois_Annee_Categorie` ON `Factures`;
DROP INDEX IF EXISTS `IX_Factures_IdCategorie` ON `Factures`;
DROP INDEX IF EXISTS `IX_Clients_IdCategorieClient` ON `Clients`;

-- Supprimer les clés étrangères obsolètes
ALTER TABLE `Clients` DROP FOREIGN KEY IF EXISTS `FK_Clients_CategorieClients_IdCategorieClient`;
ALTER TABLE `Factures` DROP FOREIGN KEY IF EXISTS `FK_Factures_CategorieClients_IdCategorie`;

-- =====================================================
-- 2. Créer la table Usages
-- =====================================================

CREATE TABLE IF NOT EXISTS `Usages` (
    `IdUsage` INT NOT NULL AUTO_INCREMENT,
    `Libelle` VARCHAR(200) NOT NULL,
    `Description` VARCHAR(500) NULL,
    `IdCategorieClient` INT NOT NULL,
    `DateCreation` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`IdUsage`),
    INDEX `IX_Usages_IdCategorieClient` (`IdCategorieClient`),
    INDEX `IX_Usage_Libelle_IdCategorieClient` (`Libelle`, `IdCategorieClient`),
    CONSTRAINT `FK_Usages_CategorieClients_IdCategorieClient` 
        FOREIGN KEY (`IdCategorieClient`) 
        REFERENCES `CategorieClients` (`IdCategorie`) 
        ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =====================================================
-- 3. Créer la table ClientUsages (remplace ClientCategorieClients)
-- =====================================================

CREATE TABLE IF NOT EXISTS `ClientUsages` (
    `IdClientUsage` INT NOT NULL AUTO_INCREMENT,
    `IdClient` INT NOT NULL,
    `IdUsage` INT NOT NULL,
    `nombreBatiment` INT NOT NULL DEFAULT 1,
    `DateAttribution` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`IdClientUsage`),
    INDEX `IX_ClientUsage_IdClient` (`IdClient`),
    INDEX `IX_ClientUsage_IdUsage` (`IdUsage`),
    UNIQUE INDEX `IX_ClientUsage_Client_Usage_Unique` (`IdClient`, `IdUsage`),
    CONSTRAINT `FK_ClientUsages_Clients_IdClient` 
        FOREIGN KEY (`IdClient`) 
        REFERENCES `Clients` (`IdClient`) 
        ON DELETE CASCADE,
    CONSTRAINT `FK_ClientUsages_Usages_IdUsage` 
        FOREIGN KEY (`IdUsage`) 
        REFERENCES `Usages` (`IdUsage`) 
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =====================================================
-- 4. Modifier la table Factures pour utiliser IdUsage
-- =====================================================

-- Ajouter la colonne IdUsage
ALTER TABLE `Factures` 
    ADD COLUMN `IdUsage` INT NOT NULL DEFAULT 0 AFTER `IdFacture`;

-- Créer l'index pour IdUsage
CREATE INDEX `IX_Factures_IdUsage` ON `Factures` (`IdUsage`);

-- Créer l'index composite pour mois/année/usage
CREATE INDEX `IX_Facture_Mois_Annee_Usage` 
    ON `Factures` (`MoisEmission`, `AnneesEmission`, `IdUsage`);

-- Ajouter la clé étrangère vers Usages
ALTER TABLE `Factures`
    ADD CONSTRAINT `FK_Factures_Usages_IdUsage`
        FOREIGN KEY (`IdUsage`)
        REFERENCES `Usages` (`IdUsage`)
        ON DELETE RESTRICT;

-- =====================================================
-- 5. Notes importantes
-- =====================================================
-- 
-- ⚠️ ATTENTION : 
-- - La colonne IdUsage dans Factures a une valeur par défaut de 0.
-- - Vous devez mettre à jour toutes les factures existantes avec un IdUsage valide
--   AVANT de retirer la valeur par défaut.
-- 
-- - Pour retirer la valeur par défaut après migration des données :
--   ALTER TABLE `Factures` MODIFY COLUMN `IdUsage` INT NOT NULL;
-- 
-- =====================================================

-- Réactiver les vérifications de clés étrangères
SET FOREIGN_KEY_CHECKS = 1;

-- =====================================================
-- Script terminé
-- =====================================================
-- Vérifiez que toutes les tables et contraintes ont été créées correctement
-- en exécutant les requêtes de diagnostic ci-dessous.
-- =====================================================
