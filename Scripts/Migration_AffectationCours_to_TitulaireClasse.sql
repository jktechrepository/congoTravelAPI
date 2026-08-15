-- ═══════════════════════════════════════════════════════════════════════════════
-- 📝 SCRIPT DE MIGRATION : AffectationCours → TitulaireClasse
-- ═══════════════════════════════════════════════════════════════════════════════
--
-- OBJECTIF :
-- Migrer les affectations existantes de Maternelle/Primaire depuis AffectationCours
-- vers le nouveau système TitulaireClasse pour simplifier la gestion.
--
-- CONTEXTE :
-- Dans le système congolais :
-- - MATERNELLE/PRIMAIRE : 1 enseignant pour tous les cours → TitulaireClasse
-- - SECONDAIRE : Plusieurs enseignants spécialisés → AffectationCours
--
-- DATE : 27 janvier 2025
-- VERSION : 1.0.0
--
-- ⚠️  IMPORTANT : Exécuter ce script APRÈS avoir appliqué la migration EF Core
-- ═══════════════════════════════════════════════════════════════════════════════

-- ═══════════════════════════════════════════════════════════════════════════════
-- ÉTAPE 1 : VÉRIFICATIONS PRÉLIMINAIRES
-- ═══════════════════════════════════════════════════════════════════════════════

PRINT '═══════════════════════════════════════════════════════════════';
PRINT '🔍 ÉTAPE 1 : VÉRIFICATIONS PRÉLIMINAIRES';
PRINT '═══════════════════════════════════════════════════════════════';
PRINT '';

-- Vérifier l'existence de la table TitulairesClasses
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TitulairesClasses')
BEGIN
    PRINT '❌ ERREUR : La table TitulairesClasses n''existe pas !';
    PRINT '💡 Solution : Exécutez d''abord : dotnet ef database update';
    RETURN;
END
ELSE
BEGIN
    PRINT '✅ Table TitulairesClasses trouvée';
END

-- Vérifier que le champ NiveauEnseignement existe dans Direction
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID('Directions') 
               AND name = 'NiveauEnseignement')
BEGIN
    PRINT '❌ ERREUR : Le champ NiveauEnseignement n''existe pas dans Directions !';
    PRINT '💡 Solution : Exécutez d''abord : dotnet ef database update';
    RETURN;
END
ELSE
BEGIN
    PRINT '✅ Champ NiveauEnseignement trouvé dans Directions';
END

PRINT '';

-- ═══════════════════════════════════════════════════════════════════════════════
-- ÉTAPE 2 : REMPLIR LE CHAMP NiveauEnseignement SI VIDE
-- ═══════════════════════════════════════════════════════════════════════════════

PRINT '═══════════════════════════════════════════════════════════════';
PRINT '📝 ÉTAPE 2 : REMPLISSAGE DU CHAMP NiveauEnseignement';
PRINT '═══════════════════════════════════════════════════════════════';
PRINT '';

-- Compter les directions sans NiveauEnseignement
DECLARE @DirectionsSansNiveau INT;
SELECT @DirectionsSansNiveau = COUNT(*) 
FROM Directions 
WHERE NiveauEnseignement IS NULL OR NiveauEnseignement = '';

PRINT CONCAT('📊 Directions sans NiveauEnseignement : ', @DirectionsSansNiveau);

IF @DirectionsSansNiveau > 0
BEGIN
    PRINT '';
    PRINT '⚠️  ATTENTION : Vous devez remplir manuellement le champ NiveauEnseignement';
    PRINT '   pour chaque direction avant de continuer la migration.';
    PRINT '';
    PRINT '   Valeurs possibles : MATERNELLE, PRIMAIRE, SECONDAIRE';
    PRINT '';
    PRINT '   Exemple :';
    PRINT '   UPDATE Directions SET NiveauEnseignement = ''MATERNELLE'' WHERE IdDirection = 1;';
    PRINT '   UPDATE Directions SET NiveauEnseignement = ''PRIMAIRE'' WHERE IdDirection = 2;';
    PRINT '   UPDATE Directions SET NiveauEnseignement = ''SECONDAIRE'' WHERE IdDirection = 3;';
    PRINT '';
    PRINT '❌ Migration interrompue. Veuillez remplir NiveauEnseignement.';
    RETURN;
END
ELSE
BEGIN
    PRINT '✅ Toutes les directions ont un NiveauEnseignement';
END

PRINT '';

-- ═══════════════════════════════════════════════════════════════════════════════
-- ÉTAPE 3 : ANALYSE DES DONNÉES À MIGRER
-- ═══════════════════════════════════════════════════════════════════════════════

PRINT '═══════════════════════════════════════════════════════════════';
PRINT '📊 ÉTAPE 3 : ANALYSE DES DONNÉES';
PRINT '═══════════════════════════════════════════════════════════════';
PRINT '';

-- Statistiques des affectations existantes
DECLARE @AffectationsTotal INT;
DECLARE @AffectationsMaternelle INT;
DECLARE @AffectationsPrimaire INT;
DECLARE @AffectationsSecondaire INT;

SELECT @AffectationsTotal = COUNT(*) FROM AffectationsCours WHERE Statut = 1;

SELECT @AffectationsMaternelle = COUNT(DISTINCT ac.IdAgent)
FROM AffectationsCours ac
INNER JOIN Cours co ON ac.IdCours = co.IdCours
INNER JOIN Classes c ON co.IdClasse = c.IdClasse
INNER JOIN Directions d ON c.IdDirection = d.IdDirection
WHERE d.NiveauEnseignement = 'MATERNELLE' AND ac.Statut = 1;

SELECT @AffectationsPrimaire = COUNT(DISTINCT ac.IdAgent)
FROM AffectationsCours ac
INNER JOIN Cours co ON ac.IdCours = co.IdCours
INNER JOIN Classes c ON co.IdClasse = c.IdClasse
INNER JOIN Directions d ON c.IdDirection = d.IdDirection
WHERE d.NiveauEnseignement = 'PRIMAIRE' AND ac.Statut = 1;

SELECT @AffectationsSecondaire = COUNT(DISTINCT ac.IdAgent)
FROM AffectationsCours ac
INNER JOIN Cours co ON ac.IdCours = co.IdCours
INNER JOIN Classes c ON co.IdClasse = c.IdClasse
INNER JOIN Directions d ON c.IdDirection = d.IdDirection
WHERE d.NiveauEnseignement = 'SECONDAIRE' AND ac.Statut = 1;

PRINT CONCAT('📊 Total AffectationsCours actives    : ', @AffectationsTotal);
PRINT CONCAT('📊 Enseignants en MATERNELLE (à migrer): ', @AffectationsMaternelle);
PRINT CONCAT('📊 Enseignants en PRIMAIRE (à migrer)  : ', @AffectationsPrimaire);
PRINT CONCAT('📊 Enseignants en SECONDAIRE (conservé): ', @AffectationsSecondaire);
PRINT '';

-- ═══════════════════════════════════════════════════════════════════════════════
-- ÉTAPE 4 : MIGRATION DES TITULAIRES (MATERNELLE/PRIMAIRE)
-- ═══════════════════════════════════════════════════════════════════════════════

PRINT '═══════════════════════════════════════════════════════════════';
PRINT '🚀 ÉTAPE 4 : MIGRATION VERS TITULAIRECLASSE';
PRINT '═══════════════════════════════════════════════════════════════';
PRINT '';

BEGIN TRY
    BEGIN TRANSACTION;

    -- Créer les titulaires à partir des affectations existantes
    INSERT INTO TitulairesClasses (
        IdAgent,
        IdClasse,
        IdAnneeScolaire,
        DateDebut,
        DateFin,
        Statut,
        Commentaire,
        DateCreation
    )
    SELECT DISTINCT 
        ac.IdAgent,
        c.IdClasse,
        ac.IdAnneeScolaire,
        MIN(ac.DateAffectation) as DateDebut,
        NULL as DateFin, -- Toujours actif pour le moment
        1 as Statut,
        'Migration automatique depuis AffectationCours' as Commentaire,
        GETDATE() as DateCreation
    FROM AffectationsCours ac
    INNER JOIN Cours co ON ac.IdCours = co.IdCours
    INNER JOIN Classes c ON co.IdClasse = c.IdClasse
    INNER JOIN Directions d ON c.IdDirection = d.IdDirection
    WHERE d.NiveauEnseignement IN ('MATERNELLE', 'PRIMAIRE')
      AND ac.Statut = 1
      AND NOT EXISTS (
          -- Éviter les doublons si le script est ré-exécuté
          SELECT 1 FROM TitulairesClasses tc
          WHERE tc.IdAgent = ac.IdAgent
            AND tc.IdClasse = c.IdClasse
            AND tc.IdAnneeScolaire = ac.IdAnneeScolaire
      )
    GROUP BY ac.IdAgent, c.IdClasse, ac.IdAnneeScolaire;

    DECLARE @TitulairesInseres INT = @@ROWCOUNT;
    PRINT CONCAT('✅ ', @TitulairesInseres, ' titulaires créés dans TitulairesClasses');

    COMMIT TRANSACTION;
    PRINT '✅ Transaction validée avec succès';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '❌ ERREUR lors de la migration !';
    PRINT CONCAT('   Message : ', ERROR_MESSAGE());
    RETURN;
END CATCH

PRINT '';

-- ═══════════════════════════════════════════════════════════════════════════════
-- ÉTAPE 5 : DÉSACTIVATION DES ANCIENNES AFFECTATIONS (OPTIONNEL)
-- ═══════════════════════════════════════════════════════════════════════════════

PRINT '═══════════════════════════════════════════════════════════════';
PRINT '⚠️  ÉTAPE 5 : NETTOYAGE (OPTIONNEL)';
PRINT '═══════════════════════════════════════════════════════════════';
PRINT '';
PRINT '⚠️  ATTENTION : Cette étape va désactiver les anciennes AffectationCours';
PRINT '   pour Maternelle/Primaire (elles sont maintenant gérées par TitulaireClasse).';
PRINT '';
PRINT '💡 Décommentez cette section si vous souhaitez nettoyer les anciennes données.';
PRINT '   (RECOMMANDÉ après vérification que la migration s''est bien passée)';
PRINT '';

/*
-- ⚠️ DÉCOMMENTER CETTE SECTION POUR NETTOYER LES ANCIENNES AFFECTATIONS

BEGIN TRY
    BEGIN TRANSACTION;

    -- Désactiver les AffectationsCours pour Maternelle/Primaire
    UPDATE ac
    SET Statut = 0,
        DateFinAffectation = GETDATE(),
        Commentaire = CONCAT(ISNULL(Commentaire, ''), ' [MIGRÉ vers TitulaireClasse le ', CONVERT(VARCHAR, GETDATE(), 103), ']')
    FROM AffectationsCours ac
    INNER JOIN Cours co ON ac.IdCours = co.IdCours
    INNER JOIN Classes c ON co.IdClasse = c.IdClasse
    INNER JOIN Directions d ON c.IdDirection = d.IdDirection
    WHERE d.NiveauEnseignement IN ('MATERNELLE', 'PRIMAIRE')
      AND ac.Statut = 1;

    DECLARE @AffectationsDesactivees INT = @@ROWCOUNT;
    PRINT CONCAT('✅ ', @AffectationsDesactivees, ' anciennes affectations désactivées');

    COMMIT TRANSACTION;
    PRINT '✅ Nettoyage terminé avec succès';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '❌ ERREUR lors du nettoyage !';
    PRINT CONCAT('   Message : ', ERROR_MESSAGE());
END CATCH
*/

PRINT '';

-- ═══════════════════════════════════════════════════════════════════════════════
-- ÉTAPE 6 : VÉRIFICATIONS POST-MIGRATION
-- ═══════════════════════════════════════════════════════════════════════════════

PRINT '═══════════════════════════════════════════════════════════════';
PRINT '✔️  ÉTAPE 6 : VÉRIFICATIONS POST-MIGRATION';
PRINT '═══════════════════════════════════════════════════════════════';
PRINT '';

-- Compter les titulaires créés
DECLARE @TitulairesCrees INT;
SELECT @TitulairesCrees = COUNT(*) FROM TitulairesClasses WHERE Statut = 1;
PRINT CONCAT('📊 Total titulaires actifs dans TitulairesClasses : ', @TitulairesCrees);

-- Vérifier les classes sans titulaire (Maternelle/Primaire uniquement)
DECLARE @ClassesSansTitulaire INT;
SELECT @ClassesSansTitulaire = COUNT(*)
FROM Classes c
INNER JOIN Directions d ON c.IdDirection = d.IdDirection
WHERE d.NiveauEnseignement IN ('MATERNELLE', 'PRIMAIRE')
  AND c.Statut = 1
  AND NOT EXISTS (
      SELECT 1 FROM TitulairesClasses tc
      WHERE tc.IdClasse = c.IdClasse AND tc.Statut = 1
  );

IF @ClassesSansTitulaire > 0
BEGIN
    PRINT CONCAT('⚠️  ', @ClassesSansTitulaire, ' classes MATERNELLE/PRIMAIRE sans titulaire');
    PRINT '   Vérifiez ces classes manuellement :';
    PRINT '';
    
    SELECT 
        c.IdClasse,
        c.NomClasse,
        d.NomDirection,
        d.NiveauEnseignement
    FROM Classes c
    INNER JOIN Directions d ON c.IdDirection = d.IdDirection
    WHERE d.NiveauEnseignement IN ('MATERNELLE', 'PRIMAIRE')
      AND c.Statut = 1
      AND NOT EXISTS (
          SELECT 1 FROM TitulairesClasses tc
          WHERE tc.IdClasse = c.IdClasse AND tc.Statut = 1
      );
END
ELSE
BEGIN
    PRINT '✅ Toutes les classes MATERNELLE/PRIMAIRE ont un titulaire';
END

PRINT '';

-- ═══════════════════════════════════════════════════════════════════════════════
-- ÉTAPE 7 : RAPPORT FINAL
-- ═══════════════════════════════════════════════════════════════════════════════

PRINT '═══════════════════════════════════════════════════════════════';
PRINT '📋 RAPPORT FINAL DE MIGRATION';
PRINT '═══════════════════════════════════════════════════════════════';
PRINT '';
PRINT '✅ Migration terminée avec succès !';
PRINT '';
PRINT '📊 RÉSUMÉ :';
PRINT CONCAT('   • Titulaires créés                  : ', @TitulairesCrees);
PRINT CONCAT('   • Classes sans titulaire (à vérifier): ', @ClassesSansTitulaire);
PRINT '';
PRINT '🎯 PROCHAINES ÉTAPES :';
PRINT '   1. Vérifier les données migrées via l''API :';
PRINT '      GET /api/TitulaireClasse/paged';
PRINT '';
PRINT '   2. Tester la création d''un nouveau titulaire :';
PRINT '      POST /api/TitulaireClasse';
PRINT '';
PRINT '   3. Si tout fonctionne, décommenter l''ÉTAPE 5 pour nettoyer';
PRINT '      les anciennes AffectationCours de Maternelle/Primaire.';
PRINT '';
PRINT '═══════════════════════════════════════════════════════════════';
PRINT '🎉 MIGRATION COMPLÈTE !';
PRINT '═══════════════════════════════════════════════════════════════';

