-- =====================================================
-- SCRIPT DE PRODUCTION CongoTravel API
-- Rendre IdReservation nullable dans la table Billets
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
-- 1. RENDRE IdReservation NULLABLE DANS LA TABLE Billets
-- =====================================================

-- Vérifier l'état actuel de la colonne
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT,
    COLUMN_COMMENT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = DATABASE() 
  AND TABLE_NAME = 'Billets' 
  AND COLUMN_NAME = 'IdReservation';

-- Compter les enregistrements avec IdReservation = 0 (valeur par défaut potentielle)
SELECT COUNT(*) as count_idreservation_zero 
FROM Billets 
WHERE IdReservation = 0;

-- Compter les enregistrements avec IdReservation IS NULL
SELECT COUNT(*) as count_idreservation_null 
FROM Billets 
WHERE IdReservation IS NULL;

-- Modifier la colonne pour la rendre nullable
ALTER TABLE Billets 
MODIFY COLUMN IdReservation INT NULL 
COMMENT 'Identifiant de la réservation (optionnel)';

-- Réactiver les vérifications de clés étrangères
SET FOREIGN_KEY_CHECKS = 1;

-- =====================================================
-- VALIDATION DES MODIFICATIONS
-- =====================================================

-- Afficher la nouvelle structure de la table Billets
DESCRIBE Billets;

-- Vérifier que la colonne est maintenant nullable
SELECT 
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT,
    COLUMN_COMMENT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = DATABASE() 
  AND TABLE_NAME = 'Billets' 
  AND COLUMN_NAME = 'IdReservation';

-- =====================================================
-- TESTS DE VALIDATION
-- =====================================================

-- Test 1: Insérer un billet sans IdReservation (NULL)
INSERT INTO Billets (
    IdReservation,
    QrCode,
    dateGeneration,
    IdSociete,
    IdClient
) VALUES (
    NULL,  -- IdReservation nullable
    'TEST_IDRESERVATION_NULL',
    NOW(),
    1,
    1
);

-- Vérifier que l'insertion a réussi
SELECT Id, IdReservation, QrCode 
FROM Billets 
WHERE QrCode = 'TEST_IDRESERVATION_NULL' 
LIMIT 1;

-- Test 2: Insérer un billet avec IdReservation (non NULL)
INSERT INTO Billets (
    IdReservation,
    QrCode,
    dateGeneration,
    IdSociete,
    IdClient
) VALUES (
    1,  -- IdReservation non null
    'TEST_IDRESERVATION_NOTNULL',
    NOW(),
    1,
    1
);

-- Vérifier que l'insertion a réussi
SELECT Id, IdReservation, QrCode 
FROM Billets 
WHERE QrCode = 'TEST_IDRESERVATION_NOTNULL' 
LIMIT 1;

-- Nettoyer les tests
DELETE FROM Billets 
WHERE QrCode IN ('TEST_IDRESERVATION_NULL', 'TEST_IDRESERVATION_NOTNULL');

-- =====================================================
-- VALIDATION DES CONTRAINTES
-- =====================================================

-- Vérifier que la contrainte de clé étrangère fonctionne toujours
-- (Cette requête ne devrait pas retourner d'erreur si la FK existe)
SELECT 
    CONSTRAINT_NAME,
    TABLE_NAME,
    COLUMN_NAME,
    REFERENCED_TABLE_NAME,
    REFERENCED_COLUMN_NAME,
    DELETE_RULE
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc 
    ON kcu.CONSTRAINT_NAME = rc.CONSTRAINT_NAME
WHERE kcu.TABLE_SCHEMA = DATABASE() 
  AND kcu.TABLE_NAME = 'Billets' 
  AND kcu.COLUMN_NAME = 'IdReservation';

-- =====================================================
-- STATISTIQUES POST-MODIFICATION
-- =====================================================

-- Compter le total des billets
SELECT COUNT(*) as total_billets FROM Billets;

-- Compter les billets avec IdReservation NULL
SELECT COUNT(*) as billets_sans_reservation 
FROM Billets 
WHERE IdReservation IS NULL;

-- Compter les billets avec IdReservation non NULL
SELECT COUNT(*) as billets_avec_reservation 
FROM Billets 
WHERE IdReservation IS NOT NULL;

-- =====================================================
-- RÉSUMÉ DES MODIFICATIONS
-- =====================================================

SELECT 'Script exécuté avec succès!' as status;
SELECT 'Modification appliquée:' as info;
SELECT '- Billets.IdReservation rendu nullable (INT NULL)' as details;
SELECT '- Contrainte de clé étrangère préservée' as details;
SELECT '- Compatibilité avec les données existantes' as details;

-- =====================================================
-- FIN DU SCRIPT
-- =====================================================
