-- =====================================================
-- SCRIPT SQL DE VALIDATION - WORKFLOW PAIEMENT→BILLET
-- =====================================================
-- Version: 1.3.0
-- Date: 23/04/2026
-- Description: Script de validation post-migration du workflow automatique
-- =====================================================

-- =====================================================
-- INSTRUCTIONS
-- =====================================================
-- 1. Exécuter ce script après la migration
-- 2. Vérifier que tous les tests passent
-- 3. Si un test échoue, consulter le guide de dépannage
-- =====================================================

PRINT 'DÉBUT DE LA VALIDATION DU WORKFLOW PAIEMENT→BILLET...';
PRINT 'Timestamp: ' + CONVERT(VARCHAR, GETDATE(), 120);

-- =====================================================
-- TEST 1: VALIDATION DES COLONNES AJOUTÉES
-- =====================================================

PRINT '';
PRINT 'TEST 1: VALIDATION DES COLONNES AJOUTÉES';

-- Vérifier que les colonnes existent
DECLARE @ColonnesManquantes INT = 0;

IF NOT EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Paiements' 
      AND COLUMN_NAME = 'DateEmissionBillet'
)
BEGIN
    PRINT '❌ ERREUR: La colonne DateEmissionBillet n''existe pas';
    SET @ColonnesManquantes = @ColonnesManquantes + 1;
END
ELSE
BEGIN
    PRINT '✓ Colonne DateEmissionBillet présente';
END

IF NOT EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Paiements' 
      AND COLUMN_NAME = 'IdBilletEmis'
)
BEGIN
    PRINT '❌ ERREUR: La colonne IdBilletEmis n''existe pas';
    SET @ColonnesManquantes = @ColonnesManquantes + 1;
END
ELSE
BEGIN
    PRINT '✓ Colonne IdBilletEmis présente';
END

IF @ColonnesManquantes > 0
BEGIN
    PRINT '❌ TEST 1 ÉCHOUÉ: Des colonnes sont manquantes';
    GOTO FIN_VALIDATION;
END
ELSE
BEGIN
    PRINT '✓ TEST 1 RÉUSSI: Toutes les colonnes sont présentes';
END

-- =====================================================
-- TEST 2: VALIDATION DES INDEX CRÉÉS
-- =====================================================

PRINT '';
PRINT 'TEST 2: VALIDATION DES INDEX CRÉÉS';

DECLARE @IndexManquants INT = 0;

-- Vérifier l'index sur DateEmissionBillet
IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = 'IX_Paiements_DateEmissionBillet' 
      AND object_id = OBJECT_ID('Paiements')
)
BEGIN
    PRINT '❌ ERREUR: L''index IX_Paiements_DateEmissionBillet n''existe pas';
    SET @IndexManquants = @IndexManquants + 1;
END
ELSE
BEGIN
    PRINT '✓ Index IX_Paiements_DateEmissionBillet présent';
END

-- Vérifier l'index sur IdBilletEmis
IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = 'IX_Paiements_IdBilletEmis' 
      AND object_id = OBJECT_ID('Paiements')
)
BEGIN
    PRINT '❌ ERREUR: L''index IX_Paiements_IdBilletEmis n''existe pas';
    SET @IndexManquants = @IndexManquants + 1;
END
ELSE
BEGIN
    PRINT '✓ Index IX_Paiements_IdBilletEmis présent';
END

IF @IndexManquants > 0
BEGIN
    PRINT '❌ TEST 2 ÉCHOUÉ: Des index sont manquants';
    GOTO FIN_VALIDATION;
END
ELSE
BEGIN
    PRINT '✓ TEST 2 RÉUSSI: Tous les index sont présents';
END

-- =====================================================
-- TEST 3: VALIDATION DE LA CLÉ ÉTRANGÈRE
-- =====================================================

PRINT '';
PRINT 'TEST 3: VALIDATION DE LA CLÉ ÉTRANGÈRE';

IF NOT EXISTS (
    SELECT 1 
    FROM sys.foreign_keys 
    WHERE name = 'FK_Paiements_Billets_IdBilletEmis' 
      AND parent_object_id = OBJECT_ID('Paiements')
)
BEGIN
    PRINT '❌ ERREUR: La clé étrangère FK_Paiements_Billets_IdBilletEmis n''existe pas';
    GOTO FIN_VALIDATION;
END
ELSE
BEGIN
    PRINT '✓ TEST 3 RÉUSSI: Clé étrangère présente';
END

-- =====================================================
-- TEST 4: VALIDATION DE L'INTÉGRITÉ DES DONNÉES
-- =====================================================

PRINT '';
PRINT 'TEST 4: VALIDATION DE L''INTÉGRITÉ DES DONNÉES';

-- Vérifier qu'il n'y a pas de paiements avec IdBilletEmis pointant vers un billet inexistant
DECLARE @PaiementsOrphelins INT;
SELECT @PaiementsOrphelins = COUNT(*) 
FROM Paiements p
LEFT JOIN Billets b ON p.IdBilletEmis = b.Id
WHERE p.IdBilletEmis IS NOT NULL 
  AND b.Id IS NULL;

IF @PaiementsOrphelins > 0
BEGIN
    PRINT '❌ ERREUR: ' + CAST(@PaiementsOrphelins AS VARCHAR) + ' paiements pointent vers des billets inexistants';
    GOTO FIN_VALIDATION;
END
ELSE
BEGIN
    PRINT '✓ TEST 4 RÉUSSI: Aucun paiement orphelin détecté';
END

-- =====================================================
-- TEST 5: VALIDATION DES TYPES DE DONNÉES
-- =====================================================

PRINT '';
PRINT 'TEST 5: VALIDATION DES TYPES DE DONNÉES';

DECLARE @TypesIncorrects INT = 0;

-- Vérifier le type de DateEmissionBillet
IF NOT EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Paiements' 
      AND COLUMN_NAME = 'DateEmissionBillet'
      AND DATA_TYPE = 'datetime'
)
BEGIN
    PRINT '❌ ERREUR: DateEmissionBillet n''est pas de type datetime';
    SET @TypesIncorrects = @TypesIncorrects + 1;
END
ELSE
BEGIN
    PRINT '✓ DateEmissionBillet est de type datetime';
END

-- Vérifier le type de IdBilletEmis
IF NOT EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Paiements' 
      AND COLUMN_NAME = 'IdBilletEmis'
      AND DATA_TYPE = 'int'
)
BEGIN
    PRINT '❌ ERREUR: IdBilletEmis n''est pas de type int';
    SET @TypesIncorrects = @TypesIncorrects + 1;
END
ELSE
BEGIN
    PRINT '✓ IdBilletEmis est de type int';
END

IF @TypesIncorrects > 0
BEGIN
    PRINT '❌ TEST 5 ÉCHOUÉ: Types de données incorrects';
    GOTO FIN_VALIDATION;
END
ELSE
BEGIN
    PRINT '✓ TEST 5 RÉUSSI: Types de données corrects';
END

-- =====================================================
-- TEST 6: VALIDATION DE LA NULLABILITÉ
-- =====================================================

PRINT '';
PRINT 'TEST 6: VALIDATION DE LA NULLABILITÉ';

DECLARE @NullabiliteIncorrecte INT = 0;

-- Vérifier que DateEmissionBillet est nullable
IF NOT EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Paiements' 
      AND COLUMN_NAME = 'DateEmissionBillet'
      AND IS_NULLABLE = 'YES'
)
BEGIN
    PRINT '❌ ERREUR: DateEmissionBillet doit être nullable';
    SET @NullabiliteIncorrecte = @NullabiliteIncorrecte + 1;
END
ELSE
BEGIN
    PRINT '✓ DateEmissionBillet est nullable';
END

-- Vérifier que IdBilletEmis est nullable
IF NOT EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Paiements' 
      AND COLUMN_NAME = 'IdBilletEmis'
      AND IS_NULLABLE = 'YES'
)
BEGIN
    PRINT '❌ ERREUR: IdBilletEmis doit être nullable';
    SET @NullabiliteIncorrecte = @NullabiliteIncorrecte + 1;
END
ELSE
BEGIN
    PRINT '✓ IdBilletEmis est nullable';
END

IF @NullabiliteIncorrecte > 0
BEGIN
    PRINT '❌ TEST 6 ÉCHOUÉ: Nullabilité incorrecte';
    GOTO FIN_VALIDATION;
END
ELSE
BEGIN
    PRINT '✓ TEST 6 RÉUSSI: Nullabilité correcte';
END

-- =====================================================
-- TEST 7: VALIDATION DES PERFORMANCES
-- =====================================================

PRINT '';
PRINT 'TEST 7: VALIDATION DES PERFORMANCES';

-- Test de performance sur les nouvelles colonnes
DECLARE @StartTime DATETIME = GETDATE();
DECLARE @TestCount INT;

-- Simuler une requête typique du workflow
SELECT @TestCount = COUNT(*) 
FROM Paiements p
LEFT JOIN Billets b ON p.IdBilletEmis = b.Id
WHERE p.DateEmissionBillet IS NOT NULL;

DECLARE @ElapsedTime INT = DATEDIFF(MILLISECOND, @StartTime, GETDATE());

PRINT 'Temps d''exécution de la requête de workflow: ' + CAST(@ElapsedTime AS VARCHAR) + 'ms';

IF @ElapsedTime > 1000 -- Plus d'1 seconde
BEGIN
    PRINT '⚠️ ATTENTION: Performance lente détectée (> 1s)';
    PRINT 'Considérez l''ajout d''index supplémentaires';
END
ELSE
BEGIN
    PRINT '✓ TEST 7 RÉUSSI: Performance acceptable';
END

-- =====================================================
-- TEST 8: VALIDATION DE LA COHÉRENCE
-- =====================================================

PRINT '';
PRINT 'TEST 8: VALIDATION DE LA COHÉRENCE';

-- Vérifier que les paiements avec billets émis ont une date d'émission
DECLARE @PaiementsIncoherents INT;
SELECT @PaiementsIncoherents = COUNT(*) 
FROM Paiements 
WHERE IdBilletEmis IS NOT NULL 
  AND DateEmissionBillet IS NULL;

IF @PaiementsIncoherents > 0
BEGIN
    PRINT '❌ ERREUR: ' + CAST(@PaiementsIncoherents AS VARCHAR) + ' paiements ont un billet mais pas de date d''émission';
    GOTO FIN_VALIDATION;
END
ELSE
BEGIN
    PRINT '✓ TEST 8 RÉUSSI: Cohérence des données respectée';
END

-- =====================================================
-- RÉSUMÉ DE LA VALIDATION
-- =====================================================

PRINT '';
PRINT '=====================================================';
PRINT 'RÉSUMÉ DE LA VALIDATION';
PRINT '=====================================================';

-- Statistiques actuelles
DECLARE @TotalPaiements INT = (SELECT COUNT(*) FROM Paiements);
DECLARE @TotalBillets INT = (SELECT COUNT(*) FROM Billets);
DECLARE @PaiementsAvecBillets INT = (SELECT COUNT(*) FROM Paiements WHERE IdBilletEmis IS NOT NULL);

SELECT 
    'Validation Workflow' as Type,
    @TotalPaiements as TotalPaiements,
    @TotalBillets as TotalBillets,
    @PaiementsAvecBillets as PaiementsAvecBillets,
    GETDATE() as DateValidation;

PRINT '';
PRINT 'Statistiques actuelles:';
PRINT '- Total paiements: ' + CAST(@TotalPaiements AS VARCHAR);
PRINT '- Total billets: ' + CAST(@TotalBillets AS VARCHAR);
PRINT '- Paiements avec billets: ' + CAST(@PaiementsAvecBillets AS VARCHAR);

PRINT '';
PRINT '✅ TOUS LES TESTS SONT PASSÉS AVEC SUCCÈS!';
PRINT '🎉 Le workflow automatique Paiement→Billet est prêt pour la production!';
PRINT '';
PRINT 'Actions recommandées:';
PRINT '1. Redémarrer l''application CongoTravel API';
PRINT '2. Tester le workflow avec l''API';
PRINT '3. Monitorer les performances pendant 24h';
PRINT '';
PRINT '=====================================================';

GOTO FIN_VALIDATION_SUCCESS;

-- =====================================================
-- GESTION DES ERREURS
-- =====================================================

FIN_VALIDATION:
PRINT '';
PRINT '=====================================================';
PRINT 'VALIDATION ÉCHOUÉE!';
PRINT '=====================================================';
PRINT '';
PRINT 'Actions requises:';
PRINT '1. Corriger les erreurs identifiées ci-dessus';
PRINT '2. Réexécuter ce script de validation';
PRINT '3. Consulter le guide de dépannage si nécessaire';
PRINT '';
PRINT 'Documentation de référence:';
PRINT '- /Scripts/README_Workflow_Automatique_Deploiement.md';
PRINT '- /Scripts/Production_Rollback_Workflow.sql';
PRINT '';
PRINT '=====================================================';
RETURN;

FIN_VALIDATION_SUCCESS:
PRINT '';
PRINT '🚀 Le système est prêt pour le workflow automatique!';

-- =====================================================
-- FIN DU SCRIPT
-- =====================================================
