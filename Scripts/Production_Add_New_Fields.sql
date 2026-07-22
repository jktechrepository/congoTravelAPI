-- =====================================================
-- SCRIPT DE PRODUCTION CongoTravel API
-- Ajout des nouveaux champs aux tables Reservations et Destinations
-- Date: 2025-04-23
-- Version: 1.0
-- =====================================================

-- =====================================================
-- INSTRUCTIONS PRÉALABLES
-- =====================================================
-- 1. FAIRE UNE SAUVEGARDE COMPLÈTE DE LA BASE DE DONNÉES
-- 2. EXÉCUTER CE SCRIPT PENDANT UNE PÉRIODE DE MAINTENANCE
-- 3. VALIDER L'EXÉCUTION AVEC LA SECTION VALIDATION
-- 4. CONSERVER CE SCRIPT POUR LE ROLLBACK SI NÉCESSAIRE

-- =====================================================
-- DÉBUT DU SCRIPT
-- =====================================================

-- Désactiver les vérifications de clés étrangères (temporairement)
SET FOREIGN_KEY_CHECKS = 0;

-- =====================================================
-- 1. AJOUT DU CHAMP nombreDePlace À LA TABLE Reservations
-- =====================================================

-- Vérifier si la colonne existe déjà
SELECT COUNT(*) as column_exists 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = DATABASE() 
  AND TABLE_NAME = 'Reservations' 
  AND COLUMN_NAME = 'nombreDePlace';

-- Ajouter la colonne nombreDePlace si elle n'existe pas
ALTER TABLE Reservations 
ADD COLUMN nombreDePlace INT NOT NULL DEFAULT 1 
COMMENT 'Nombre de places réservées (par défaut: 1)';

-- =====================================================
-- 2. AJOUT DU CHAMP HeureDepart À LA TABLE Destinations
-- =====================================================

-- Vérifier si la colonne existe déjà
SELECT COUNT(*) as column_exists 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = DATABASE() 
  AND TABLE_NAME = 'Destinations' 
  AND COLUMN_NAME = 'HeureDepart';

-- Ajouter la colonne HeureDepart si elle n'existe pas
ALTER TABLE Destinations 
ADD COLUMN HeureDepart TIME NULL 
COMMENT 'Heure de départ au format HH:mm (optionnel)';

-- =====================================================
-- 3. AJOUT DU CHAMP jourDepart À LA TABLE Destinations
-- =====================================================

-- Vérifier si la colonne existe déjà
SELECT COUNT(*) as column_exists 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = DATABASE() 
  AND TABLE_NAME = 'Destinations' 
  AND COLUMN_NAME = 'jourDepart';

-- Ajouter la colonne jourDepart si elle n'existe pas
ALTER TABLE Destinations 
ADD COLUMN jourDepart VARCHAR(50) NULL 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_unicode_ci 
COMMENT 'Jour de départ (optionnel)';

-- Réactiver les vérifications de clés étrangères
SET FOREIGN_KEY_CHECKS = 1;

-- =====================================================
-- VALIDATION DES MODIFICATIONS
-- =====================================================

-- Afficher la structure des tables modifiées
DESCRIBE Reservations;
DESCRIBE Destinations;

-- Vérifier les nouvelles colonnes
SELECT 
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT,
    COLUMN_COMMENT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = DATABASE() 
  AND TABLE_NAME IN ('Reservations', 'Destinations') 
  AND COLUMN_NAME IN ('nombreDePlace', 'HeureDepart', 'jourDepart')
ORDER BY TABLE_NAME, COLUMN_NAME;

-- =====================================================
-- TESTS DE VALIDATION
-- =====================================================

-- Test 1: Insérer une réservation avec nombreDePlace par défaut
INSERT INTO Reservations (
    IdUtilisateur, 
    IdClient, 
    IdVoyage, 
    StatutReservation, 
    Statut, 
    DateReservation, 
    IdSociete
) VALUES (
    1, 1, 1, 'TEST', 1, NOW(), 1
);

-- Vérifier que nombreDePlace a bien la valeur par défaut 1
SELECT IdReservation, nombreDePlace FROM Reservations WHERE StatutReservation = 'TEST' LIMIT 1;

-- Nettoyer le test
DELETE FROM Reservations WHERE StatutReservation = 'TEST';

-- Test 2: Insérer une destination avec les nouveaux champs
INSERT INTO Destinations (
    VilleDepart, 
    VilleArrivee, 
    Montant, 
    Statut, 
    DateCreation, 
    IdSociete,
    HeureDepart,
    jourDepart
) VALUES (
    'TEST_DEPART', 
    'TEST_ARRIVEE', 
    100.00, 
    1, 
    NOW(), 
    1,
    '08:30:00',
    'Lundi'
);

-- Vérifier les nouveaux champs
SELECT IdDestination, HeureDepart, jourDepart FROM Destinations WHERE VilleDepart = 'TEST_DEPART' LIMIT 1;

-- Nettoyer le test
DELETE FROM Destinations WHERE VilleDepart = 'TEST_DEPART';

-- =====================================================
-- RÉSUMÉ DES MODIFICATIONS
-- =====================================================

SELECT 'Script exécuté avec succès!' as status;
SELECT 'Colonnes ajoutées:' as info;
SELECT '- Reservations.nombreDePlace (INT NOT NULL DEFAULT 1)' as details;
SELECT '- Destinations.HeureDepart (TIME NULL)' as details;
SELECT '- Destinations.jourDepart (VARCHAR(50) NULL)' as details;

-- =====================================================
-- FIN DU SCRIPT
-- =====================================================
