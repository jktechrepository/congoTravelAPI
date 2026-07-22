-- =====================================================
-- SCRIPT DE ROLLBACK DE PRODUCTION CongoTravel API
-- Suppression des nouveaux champs ajoutés
-- Date: 2025-04-23
-- Version: 1.0
-- =====================================================

-- =====================================================
-- INSTRUCTIONS PRÉALABLES
-- =====================================================
-- 1. UTILISER CE SCRIPT SEULEMENT EN CAS DE PROBLÈME
-- 2. FAIRE UNE SAUVEGARDE AVANT LE ROLLBACK
-- 3. EXÉCUTER PENDANT UNE PÉRIODE DE MAINTENANCE
-- 4. VALIDER AVEC LA SECTION VALIDATION

-- =====================================================
-- DÉBUT DU SCRIPT DE ROLLBACK
-- =====================================================

-- Désactiver les vérifications de clés étrangères (temporairement)
SET FOREIGN_KEY_CHECKS = 0;

-- =====================================================
-- 1. SUPPRESSION DU CHAMP nombreDePlace DE LA TABLE Reservations
-- =====================================================

-- Vérifier si la colonne existe avant suppression
SELECT COUNT(*) as column_exists 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = DATABASE() 
  AND TABLE_NAME = 'Reservations' 
  AND COLUMN_NAME = 'nombreDePlace';

-- Supprimer la colonne nombreDePlace si elle existe
ALTER TABLE Reservations 
DROP COLUMN IF EXISTS nombreDePlace;

-- =====================================================
-- 2. SUPPRESSION DU CHAMP HeureDepart DE LA TABLE Destinations
-- =====================================================

-- Vérifier si la colonne existe avant suppression
SELECT COUNT(*) as column_exists 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = DATABASE() 
  AND TABLE_NAME = 'Destinations' 
  AND COLUMN_NAME = 'HeureDepart';

-- Supprimer la colonne HeureDepart si elle existe
ALTER TABLE Destinations 
DROP COLUMN IF EXISTS HeureDepart;

-- =====================================================
-- 3. SUPPRESSION DU CHAMP jourDepart DE LA TABLE Destinations
-- =====================================================

-- Vérifier si la colonne existe avant suppression
SELECT COUNT(*) as column_exists 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = DATABASE() 
  AND TABLE_NAME = 'Destinations' 
  AND COLUMN_NAME = 'jourDepart';

-- Supprimer la colonne jourDepart si elle existe
ALTER TABLE Destinations 
DROP COLUMN IF EXISTS jourDepart;

-- Réactiver les vérifications de clés étrangères
SET FOREIGN_KEY_CHECKS = 1;

-- =====================================================
-- VALIDATION DU ROLLBACK
-- =====================================================

-- Afficher la structure des tables après rollback
DESCRIBE Reservations;
DESCRIBE Destinations;

-- Vérifier que les colonnes ont bien été supprimées
SELECT 
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = DATABASE() 
  AND TABLE_NAME IN ('Reservations', 'Destinations') 
  AND COLUMN_NAME IN ('nombreDePlace', 'HeureDepart', 'jourDepart')
ORDER BY TABLE_NAME, COLUMN_NAME;

-- =====================================================
-- RÉSUMÉ DU ROLLBACK
-- =====================================================

SELECT 'Rollback exécuté avec succès!' as status;
SELECT 'Colonnes supprimées:' as info;
SELECT '- Reservations.nombreDePlace' as details;
SELECT '- Destinations.HeureDepart' as details;
SELECT '- Destinations.jourDepart' as details;

-- =====================================================
-- FIN DU SCRIPT DE ROLLBACK
-- =====================================================
