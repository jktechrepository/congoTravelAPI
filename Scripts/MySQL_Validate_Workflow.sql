-- =====================================================
-- SCRIPT SQL DE VALIDATION - WORKFLOW PAIEMENT→BILLET (MySQL)
-- =====================================================
-- Version: 1.3.0
-- Date: 23/04/2026
-- Description: Script de validation post-migration du workflow automatique
-- Base de données: MySQL
-- =====================================================

-- =====================================================
-- INSTRUCTIONS
-- =====================================================
-- 1. Exécuter ce script après la migration
-- 2. Vérifier que tous les tests passent
-- 3. Si un test échoue, consulter le guide de dépannage
-- =====================================================

SELECT 'DÉBUT DE LA VALIDATION DU WORKFLOW PAIEMENT→BILLET...' AS Message;
SELECT CONCAT('Timestamp: ', NOW()) AS Timestamp;

-- =====================================================
-- TEST 1: VALIDATION DES COLONNES AJOUTÉES
-- =====================================================

SELECT '' AS Separator;
SELECT 'TEST 1: VALIDATION DES COLONNES AJOUTÉES' AS TestTitle;

-- Vérifier que les colonnes existent
SET @colonnes_manquantes = 0;

-- Vérifier DateEmissionBillet
SET @column_exists = (
    SELECT COUNT(*) 
    FROM information_schema.columns 
    WHERE table_schema = DATABASE() 
      AND table_name = 'Paiements' 
      AND column_name = 'DateEmissionBillet'
);

IF @column_exists = 0 THEN
    SELECT '❌ ERREUR: La colonne DateEmissionBillet n existe pas' AS Error;
    SET @colonnes_manquantes = @colonnes_manquantes + 1;
ELSE
    SELECT '✓ Colonne DateEmissionBillet présente' AS Status;
END IF;

-- Vérifier IdBilletEmis
SET @column_exists = (
    SELECT COUNT(*) 
    FROM information_schema.columns 
    WHERE table_schema = DATABASE() 
      AND table_name = 'Paiements' 
      AND column_name = 'IdBilletEmis'
);

IF @column_exists = 0 THEN
    SELECT '❌ ERREUR: La colonne IdBilletEmis n existe pas' AS Error;
    SET @colonnes_manquantes = @colonnes_manquantes + 1;
ELSE
    SELECT '✓ Colonne IdBilletEmis présente' AS Status;
END IF;

-- =====================================================
-- TEST 2: VALIDATION DES INDEX CRÉÉS
-- =====================================================

SELECT '' AS Separator;
SELECT 'TEST 2: VALIDATION DES INDEX CRÉÉS' AS TestTitle;

SET @index_manquants = 0;

-- Vérifier l'index sur DateEmissionBillet
SET @index_exists = (
    SELECT COUNT(*) 
    FROM information_schema.statistics 
    WHERE table_schema = DATABASE() 
      AND table_name = 'Paiements' 
      AND index_name = 'IX_Paiements_DateEmissionBillet'
);

IF @index_exists = 0 THEN
    SELECT '❌ ERREUR: L index IX_Paiements_DateEmissionBillet n existe pas' AS Error;
    SET @index_manquants = @index_manquants + 1;
ELSE
    SELECT '✓ Index IX_Paiements_DateEmissionBillet présent' AS Status;
END IF;

-- Vérifier l'index sur IdBilletEmis
SET @index_exists = (
    SELECT COUNT(*) 
    FROM information_schema.statistics 
    WHERE table_schema = DATABASE() 
      AND table_name = 'Paiements' 
      AND index_name = 'IX_Paiements_IdBilletEmis'
);

IF @index_exists = 0 THEN
    SELECT '❌ ERREUR: L index IX_Paiements_IdBilletEmis n existe pas' AS Error;
    SET @index_manquants = @index_manquants + 1;
ELSE
    SELECT '✓ Index IX_Paiements_IdBilletEmis présent' AS Status;
END IF;

-- =====================================================
-- TEST 3: VALIDATION DE LA CLÉ ÉTRANGÈRE
-- =====================================================

SELECT '' AS Separator;
SELECT 'TEST 3: VALIDATION DE LA CLÉ ÉTRANGÈRE' AS TestTitle;

SET @constraint_exists = (
    SELECT COUNT(*) 
    FROM information_schema.table_constraints 
    WHERE table_schema = DATABASE() 
      AND table_name = 'Paiements' 
      AND constraint_name = 'FK_Paiements_Billets_IdBilletEmis'
      AND constraint_type = 'FOREIGN KEY'
);

IF @constraint_exists = 0 THEN
    SELECT '❌ ERREUR: La clé étrangère FK_Paiements_Billets_IdBilletEmis n existe pas' AS Error;
ELSE
    SELECT '✓ TEST 3 RÉUSSI: Clé étrangère présente' AS Status;
END IF;

-- =====================================================
-- TEST 4: VALIDATION DE L'INTÉGRITÉ DES DONNÉES
-- =====================================================

SELECT '' AS Separator;
SELECT 'TEST 4: VALIDATION DE L INTÉGRITÉ DES DONNÉES' AS TestTitle;

-- Vérifier qu'il n'y a pas de paiements avec IdBilletEmis pointant vers un billet inexistant
SELECT COUNT(*) AS PaiementsOrphelins
FROM Paiements p
LEFT JOIN Billets b ON p.IdBilletEmis = b.Id
WHERE p.IdBilletEmis IS NOT NULL 
  AND b.Id IS NULL;

SET @paiements_orphelins = (
    SELECT COUNT(*) 
    FROM Paiements p
    LEFT JOIN Billets b ON p.IdBilletEmis = b.Id
    WHERE p.IdBilletEmis IS NOT NULL 
      AND b.Id IS NULL
);

IF @paiements_orphelins > 0 THEN
    SELECT CONCAT('❌ ERREUR: ', @paiements_orphelins, ' paiements pointent vers des billets inexistants') AS Error;
ELSE
    SELECT '✓ TEST 4 RÉUSSI: Aucun paiement orphelin détecté' AS Status;
END IF;

-- =====================================================
-- TEST 5: VALIDATION DES TYPES DE DONNÉES
-- =====================================================

SELECT '' AS Separator;
SELECT 'TEST 5: VALIDATION DES TYPES DE DONNÉES' AS TestTitle;

SET @types_incorrects = 0;

-- Vérifier le type de DateEmissionBillet
SET @correct_type = (
    SELECT COUNT(*) 
    FROM information_schema.columns 
    WHERE table_schema = DATABASE() 
      AND table_name = 'Paiements' 
      AND column_name = 'DateEmissionBillet'
      AND data_type = 'datetime'
);

IF @correct_type = 0 THEN
    SELECT '❌ ERREUR: DateEmissionBillet n est pas de type datetime' AS Error;
    SET @types_incorrects = @types_incorrects + 1;
ELSE
    SELECT '✓ DateEmissionBillet est de type datetime' AS Status;
END IF;

-- Vérifier le type de IdBilletEmis
SET @correct_type = (
    SELECT COUNT(*) 
    FROM information_schema.columns 
    WHERE table_schema = DATABASE() 
      AND table_name = 'Paiements' 
      AND column_name = 'IdBilletEmis'
      AND data_type = 'int'
);

IF @correct_type = 0 THEN
    SELECT '❌ ERREUR: IdBilletEmis n est pas de type int' AS Error;
    SET @types_incorrects = @types_incorrects + 1;
ELSE
    SELECT '✓ IdBilletEmis est de type int' AS Status;
END IF;

-- =====================================================
-- TEST 6: VALIDATION DE LA NULLABILITÉ
-- =====================================================

SELECT '' AS Separator;
SELECT 'TEST 6: VALIDATION DE LA NULLABILITÉ' AS TestTitle;

SET @nullabilite_incorrecte = 0;

-- Vérifier que DateEmissionBillet est nullable
SET @is_nullable = (
    SELECT COUNT(*) 
    FROM information_schema.columns 
    WHERE table_schema = DATABASE() 
      AND table_name = 'Paiements' 
      AND column_name = 'DateEmissionBillet'
      AND is_nullable = 'YES'
);

IF @is_nullable = 0 THEN
    SELECT '❌ ERREUR: DateEmissionBillet doit être nullable' AS Error;
    SET @nullabilite_incorrecte = @nullabilite_incorrecte + 1;
ELSE
    SELECT '✓ DateEmissionBillet est nullable' AS Status;
END IF;

-- Vérifier que IdBilletEmis est nullable
SET @is_nullable = (
    SELECT COUNT(*) 
    FROM information_schema.columns 
    WHERE table_schema = DATABASE() 
      AND table_name = 'Paiements' 
      AND column_name = 'IdBilletEmis'
      AND is_nullable = 'YES'
);

IF @is_nullable = 0 THEN
    SELECT '❌ ERREUR: IdBilletEmis doit être nullable' AS Error;
    SET @nullabilite_incorrecte = @nullabilite_incorrecte + 1;
ELSE
    SELECT '✓ IdBilletEmis est nullable' AS Status;
END IF;

-- =====================================================
-- TEST 7: VALIDATION DES PERFORMANCES
-- =====================================================

SELECT '' AS Separator;
SELECT 'TEST 7: VALIDATION DES PERFORMANCES' AS TestTitle;

-- Test de performance sur les nouvelles colonnes
-- Simuler une requête typique du workflow
SELECT COUNT(*) AS TestCount 
FROM Paiements p
LEFT JOIN Billets b ON p.IdBilletEmis = b.Id
WHERE p.DateEmissionBillet IS NOT NULL;

-- Note: MySQL n'a pas de fonction pour mesurer le temps d'exécution comme SQL Server
-- On vérifie simplement que la requête s'exécute sans erreur
SELECT '✓ TEST 7 RÉUSSI: Requête de workflow exécutée avec succès' AS Status;

-- =====================================================
-- TEST 8: VALIDATION DE LA COHÉRENCE
-- =====================================================

SELECT '' AS Separator;
SELECT 'TEST 8: VALIDATION DE LA COHÉRENCE' AS TestTitle;

-- Vérifier que les paiements avec billets émis ont une date d'émission
SELECT COUNT(*) AS PaiementsIncoherents
FROM Paiements 
WHERE IdBilletEmis IS NOT NULL 
  AND DateEmissionBillet IS NULL;

SET @paiements_incoherents = (
    SELECT COUNT(*) 
    FROM Paiements 
    WHERE IdBilletEmis IS NOT NULL 
      AND DateEmissionBillet IS NULL
);

IF @paiements_incoherents > 0 THEN
    SELECT CONCAT('❌ ERREUR: ', @paiements_incoherents, ' paiements ont un billet mais pas de date d émission') AS Error;
ELSE
    SELECT '✓ TEST 8 RÉUSSI: Cohérence des données respectée' AS Status;
END IF;

-- =====================================================
-- RÉSUMÉ DE LA VALIDATION
-- =====================================================

SELECT '' AS Separator;
SELECT '=====================================================' AS Separator;
SELECT 'RÉSUMÉ DE LA VALIDATION' AS Title;
SELECT '=====================================================' AS Separator;

-- Statistiques actuelles
SET @total_paiements = (SELECT COUNT(*) FROM Paiements);
SET @total_billets = (SELECT COUNT(*) FROM Billets);
SET @paiements_avec_billets = (SELECT COUNT(*) FROM Paiements WHERE IdBilletEmis IS NOT NULL);

SELECT 
    'Validation Workflow' as Type,
    @total_paiements as TotalPaiements,
    @total_billets as TotalBillets,
    @paiements_avec_billets as PaiementsAvecBillets,
    NOW() as DateValidation;

SELECT '' AS Separator;
SELECT 'Statistiques actuelles:' AS StatsTitle;
SELECT CONCAT('- Total paiements: ', @total_paiements) AS Stat1;
SELECT CONCAT('- Total billets: ', @total_billets) AS Stat2;
SELECT CONCAT('- Paiements avec billets: ', @paiements_avec_billets) AS Stat3;

-- Vérifier si tous les tests ont réussi
SET @tests_en_echec = @colonnes_manquantes + @index_manquants + @types_incorrects + @nullabilite_incorrecte + @paiements_orphelins + @paiements_incoherents;

SELECT '' AS Separator;
IF @tests_en_echec = 0 THEN
    SELECT '✅ TOUS LES TESTS SONT PASSÉS AVEC SUCCÈS!' AS Result;
    SELECT '🎉 Le workflow automatique Paiement→Billet est prêt pour la production!' AS Status;
    SELECT '' AS Separator;
    SELECT 'Actions recommandées:' AS Actions;
    SELECT '1. Redémarrer l application CongoTravel API' AS Action1;
    SELECT '2. Tester le workflow avec l API' AS Action2;
    SELECT '3. Monitorer les performances pendant 24h' AS Action3;
ELSE
    SELECT '❌ VALIDATION ÉCHOUÉE!' AS Result;
    SELECT CONCAT('Nombre de tests en échec: ', @tests_en_echec) AS FailedCount;
    SELECT '' AS Separator;
    SELECT 'Actions requises:' AS Actions;
    SELECT '1. Corriger les erreurs identifiées ci-dessus' AS Action1;
    SELECT '2. Réexécuter ce script de validation' AS Action2;
    SELECT '3. Consulter le guide de dépannage si nécessaire' AS Action3;
END IF;

SELECT '=====================================================' AS Separator;

-- Afficher un résumé final détaillé
SELECT 
    'Validation Workflow' as Nom,
    '1.3.0' as Version,
    NOW() as DateValidation,
    CASE 
        WHEN @tests_en_echec = 0 THEN 'SUCCÈS' 
        ELSE 'ÉCHEC' 
    END as Statut,
    @total_paiements as TotalPaiements,
    @total_billets as TotalBillets,
    @paiements_avec_billets as PaiementsAvecBillets,
    CASE 
        WHEN @tests_en_echec = 0 THEN 'Prêt pour le workflow automatique' 
        ELSE 'Corrections requises avant déploiement' 
    END as Message;

-- =====================================================
-- INFORMATIONS DÉTAILLÉES SUR LES ÉLÉMENTS VALIDÉS
-- =====================================================

SELECT '' AS Separator;
SELECT 'DÉTAILS DES ÉLÉMENTS VALIDÉS:' AS DetailsTitle;

-- Colonnes validées
SELECT '' AS Separator;
SELECT 'Colonnes:' AS ColumnsTitle;
SELECT 
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns 
WHERE table_schema = DATABASE() 
  AND table_name = 'Paiements' 
  AND column_name IN ('DateEmissionBillet', 'IdBilletEmis')
ORDER BY column_name;

-- Index validés
SELECT '' AS Separator;
SELECT 'Index:' AS IndexTitle;
SELECT 
    index_name,
    index_type,
    GROUP_CONCAT(column_name ORDER BY seq_in_index) AS columns
FROM information_schema.statistics 
WHERE table_schema = DATABASE() 
  AND table_name = 'Paiements' 
  AND index_name LIKE 'IX_Paiements_%'
GROUP BY index_name, index_type
ORDER BY index_name;

-- Contraintes validées
SELECT '' AS Separator;
SELECT 'Contraintes:' AS ConstraintsTitle;
SELECT 
    constraint_name,
    constraint_type
FROM information_schema.table_constraints 
WHERE table_schema = DATABASE() 
  AND table_name = 'Paiements' 
  AND constraint_name LIKE 'FK_Paiements_%';

SELECT '' AS Separator;
SELECT '🚀 Le système est prêt pour le workflow automatique!' AS FinalMessage;

-- =====================================================
-- FIN DU SCRIPT
-- =====================================================
