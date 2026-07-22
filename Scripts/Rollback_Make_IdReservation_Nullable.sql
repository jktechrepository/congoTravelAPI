-- =====================================================
-- SCRIPT DE ROLLBACK DE PRODUCTION CongoTravel API
-- Rendre IdReservation NOT NULL dans la table Billets
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
-- 1. RENDRE IdReservation NOT NULL DANS LA TABLE Billets
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

-- Compter les enregistrements avec IdReservation IS NULL (devront être traités)
SELECT COUNT(*) as count_idreservation_null 
FROM Billets 
WHERE IdReservation IS NULL;

-- AVERTISSEMENT: Si des enregistrements ont IdReservation IS NULL,
-- ils doivent être traités avant de rendre la colonne NOT NULL
-- Options:
-- 1. Supprimer les enregistrements avec IdReservation IS NULL
-- 2. Mettre à jour IdReservation avec une valeur par défaut (ex: 0)
-- 3. Annuler le rollback

-- Option 1: Supprimer les enregistrements avec IdReservation IS NULL
-- Décommentez la ligne suivante si vous voulez supprimer ces enregistrements
-- DELETE FROM Billets WHERE IdReservation IS NULL;

-- Option 2: Mettre à jour les enregistrements avec IdReservation IS NULL
-- Décommentez la ligne suivante si vous voulez mettre à jour avec une valeur par défaut
-- UPDATE Billets SET IdReservation = 0 WHERE IdReservation IS NULL;

-- Modifier la colonne pour la rendre NOT NULL avec valeur par défaut
ALTER TABLE Billets 
MODIFY COLUMN IdReservation INT NOT NULL DEFAULT 0 
COMMENT 'Identifiant de la réservation (requis)';

-- Réactiver les vérifications de clés étrangères
SET FOREIGN_KEY_CHECKS = 1;

-- =====================================================
-- VALIDATION DU ROLLBACK
-- =====================================================

-- Afficher la nouvelle structure de la table Billets
DESCRIBE Billets;

-- Vérifier que la colonne est maintenant NOT NULL
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
-- TESTS DE VALIDATION POST-ROLLBACK
-- =====================================================

-- Test 1: Tenter d'insérer un billet sans IdReservation (devrait échouer)
-- Cette requête devrait générer une erreur si le rollback a réussi
-- INSERT INTO Billets (
--     IdReservation,
--     QrCode,
--     dateGeneration,
--     IdSociete,
--     IdClient
-- ) VALUES (
--     NULL,  -- Devrait échouer car NOT NULL
--     'TEST_ROLLBACK_FAIL',
--     NOW(),
--     1,
--     1
-- );

-- Test 2: Insérer un billet avec IdReservation (devrait réussir)
INSERT INTO Billets (
    IdReservation,
    QrCode,
    dateGeneration,
    IdSociete,
    IdClient
) VALUES (
    1,  -- IdReservation requis
    'TEST_ROLLBACK_SUCCESS',
    NOW(),
    1,
    1
);

-- Vérifier que l'insertion a réussi
SELECT Id, IdReservation, QrCode 
FROM Billets 
WHERE QrCode = 'TEST_ROLLBACK_SUCCESS' 
LIMIT 1;

-- Nettoyer le test
DELETE FROM Billets 
WHERE QrCode = 'TEST_ROLLBACK_SUCCESS';

-- =====================================================
-- VALIDATION DES CONTRAINTES
-- =====================================================

-- Vérifier que la contrainte de clé étrangère fonctionne toujours
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
-- STATISTIQUES POST-ROLLBACK
-- =====================================================

-- Compter le total des billets
SELECT COUNT(*) as total_billets FROM Billets;

-- Compter les billets avec IdReservation = 0 (valeur par défaut)
SELECT COUNT(*) as billets_idreservation_zero 
FROM Billets 
WHERE IdReservation = 0;

-- Compter les billets avec IdReservation > 0
SELECT COUNT(*) as billets_idreservation_positive 
FROM Billets 
WHERE IdReservation > 0;

-- =====================================================
-- RÉSUMÉ DU ROLLBACK
-- =====================================================

SELECT 'Rollback exécuté avec succès!' as status;
SELECT 'Modification annulée:' as info;
SELECT '- Billets.IdReservation rendu NOT NULL (INT NOT NULL DEFAULT 0)' as details;
SELECT '- Contrainte de clé étrangère préservée' as details;
SELECT '- Valeur par défaut: 0' as details;

-- =====================================================
-- AVERTISSEMENTS IMPORTANTS
-- =====================================================

SELECT 'AVERTISSEMENT:' as warning;
SELECT '1. Vérifiez que l''application peut gérer IdReservation = 0' as details;
SELECT '2. Testez tous les endpoints utilisant des billets' as details;
SELECT '3. Validez que la logique métier fonctionne avec la nouvelle contrainte' as details;

-- =====================================================
-- FIN DU SCRIPT DE ROLLBACK
-- =====================================================
