-- =====================================================
-- SCRIPT SQL DE ROLLBACK - WORKFLOW PAIEMENT→BILLET
-- =====================================================
-- Version: 1.3.0
-- Date: 23/04/2026
-- Description: Script de rollback pour le workflow automatique d'émission de billets
-- ATTENTION: Ce script supprime les colonnes ajoutées lors de la migration
-- =====================================================

-- =====================================================
-- INSTRUCTIONS IMPORTANTES
-- =====================================================
-- 1. FAIRE UN BACKUP COMPLET AVANT D'EXÉCUTER CE ROLLBACK
-- 2. ARRÊTER L'APPLICATION CONGOTRAVEL API AVANT L'EXÉCUTION
-- 3. CE SCRIPT EST DESTRUCTIF - IL SUPPRIME DES DONNÉES
-- 4. UTILISER SEULEMENT EN CAS DE PROBLÈME CRITIQUE
-- =====================================================

PRINT 'DÉBUT DU ROLLBACK WORKFLOW PAIEMENT→BILLET...';
PRINT 'Timestamp: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '⚠️  ATTENTION: CE SCRIPT EST DESTRUCTIF!';

-- =====================================================
-- SECTION 1: CONFIRMATION DE SÉCURITÉ
-- =====================================================

PRINT '';
PRINT 'SECTION 1: CONFIRMATION DE SÉCURITÉ';

-- Vérifier que l'utilisateur est conscient des risques
PRINT '⚠️  CE ROLLBACK VA:';
PRINT '   - Supprimer les colonnes DateEmissionBillet et IdBilletEmis';
PRINT '   - Supprimer les index associés';
PRINT '   - Supprimer la clé étrangère';
PRINT '   - PERDRE LA RÉFÉRENCE AUX BILLETS ÉMIS AUTOMATIQUEMENT';
PRINT '';
PRINT 'ATTENDEZ 10 SECONDES PUIS APPUYEZ SUR ENTRÉE POUR CONTINUER...';
-- WAITFOR DELAY '00:00:10'; -- Décommenter pour une pause automatique

-- =====================================================
-- SECTION 2: VÉRIFICATIONS PRÉALABLES
-- =====================================================

PRINT '';
PRINT 'SECTION 2: VÉRIFICATIONS PRÉALABLES';

-- Compter les paiements avec billets émis avant le rollback
DECLARE @PaiementsAvecBillets INT;
SELECT @PaiementsAvecBillets = COUNT(*) FROM Paiements WHERE IdBilletEmis IS NOT NULL;

IF @PaiementsAvecBillets > 0
BEGIN
    PRINT '⚠️  ATTENTION: ' + CAST(@PaiementsAvecBillets AS VARCHAR) + ' paiements ont des billets émis!';
    PRINT 'Ces références seront perdues après le rollback.';
    PRINT 'Considérez sauvegarder ces données avant de continuer.';
    
    -- Afficher les paiements concernés
    PRINT 'Paiements avec billets émis:';
    SELECT TOP 10 
        p.IdPaiement,
        p.DateCreation,
        p.DateEmissionBillet,
        p.IdBilletEmis,
        b.QrCode
    FROM Paiements p
    JOIN Billets b ON p.IdBilletEmis = b.Id
    WHERE p.IdBilletEmis IS NOT NULL;
    
    IF @PaiementsAvecBillets > 10
    BEGIN
        PRINT '... et ' + CAST(@PaiementsAvecBillets - 10 AS VARCHAR) + ' autres paiements';
    END
END
ELSE
BEGIN
    PRINT '✓ Aucun paiement avec billet émis détecté';
END

-- =====================================================
-- SECTION 3: SAUVEGARDE DES DONNÉES CRITIQUES
-- =====================================================

PRINT '';
PRINT 'SECTION 3: SAUVEGARDE DES DONNÉES CRITIQUES';

-- Créer une table de sauvegarde si elle n'existe pas
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Paiements_Billets_Backup')
BEGIN
    CREATE TABLE Paiements_Billets_Backup (
        IdBackup INT IDENTITY(1,1) PRIMARY KEY,
        DateRollback DATETIME DEFAULT GETDATE(),
        IdPaiement INT,
        DateEmissionBillet DATETIME,
        IdBilletEmis INT,
        QrCode VARCHAR(255),
        DateGeneration DATETIME,
        IdReservation INT,
        IdClient INT,
        IdSociete INT
    );
    
    PRINT '✓ Table de sauvegarde Paiements_Billets_Backup créée';
END

-- Sauvegarder les paiements avec billets émis
IF @PaiementsAvecBillets > 0
BEGIN
    INSERT INTO Paiements_Billets_Backup (
        IdPaiement,
        DateEmissionBillet,
        IdBilletEmis,
        QrCode,
        DateGeneration,
        IdReservation,
        IdClient,
        IdSociete
    )
    SELECT 
        p.IdPaiement,
        p.DateEmissionBillet,
        p.IdBilletEmis,
        b.QrCode,
        b.DateGeneration,
        b.IdReservation,
        b.IdClient,
        b.IdSociete
    FROM Paiements p
    JOIN Billets b ON p.IdBilletEmis = b.Id
    WHERE p.IdBilletEmis IS NOT NULL;
    
    PRINT '✓ Données sauvegardées dans Paiements_Billets_Backup';
    PRINT '  - ' + CAST(@PaiementsAvecBillets AS VARCHAR) + ' enregistrements sauvegardés';
END

-- =====================================================
-- SECTION 4: SUPPRESSION DE LA CLÉ ÉTRANGÈRE
-- =====================================================

PRINT '';
PRINT 'SECTION 4: SUPPRESSION DE LA CLÉ ÉTRANGÈRE';

IF EXISTS (
    SELECT 1 
    FROM sys.foreign_keys 
    WHERE name = 'FK_Paiements_Billets_IdBilletEmis' 
      AND parent_object_id = OBJECT_ID('Paiements')
)
BEGIN
    PRINT 'Suppression de la clé étrangère FK_Paiements_Billets_IdBilletEmis...';
    
    ALTER TABLE Paiements
    DROP CONSTRAINT FK_Paiements_Billets_IdBilletEmis;
    
    PRINT '✓ Clé étrangère supprimée';
END
ELSE
BEGIN
    PRINT '✓ Clé étrangère non trouvée (déjà supprimée)';
END

-- =====================================================
-- SECTION 5: SUPPRESSION DES INDEX
-- =====================================================

PRINT '';
PRINT 'SECTION 5: SUPPRESSION DES INDEX';

-- Supprimer l'index sur DateEmissionBillet
IF EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = 'IX_Paiements_DateEmissionBillet' 
      AND object_id = OBJECT_ID('Paiements')
)
BEGIN
    PRINT 'Suppression de l''index IX_Paiements_DateEmissionBillet...';
    
    DROP INDEX IX_Paiements_DateEmissionBillet ON Paiements;
    
    PRINT '✓ Index IX_Paiements_DateEmissionBillet supprimé';
END
ELSE
BEGIN
    PRINT '✓ Index IX_Paiements_DateEmissionBillet non trouvé (déjà supprimé)';
END

-- Supprimer l'index sur IdBilletEmis
IF EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = 'IX_Paiements_IdBilletEmis' 
      AND object_id = OBJECT_ID('Paiements')
)
BEGIN
    PRINT 'Suppression de l''index IX_Paiements_IdBilletEmis...';
    
    DROP INDEX IX_Paiements_IdBilletEmis ON Paiements;
    
    PRINT '✓ Index IX_Paiements_IdBilletEmis supprimé';
END
ELSE
BEGIN
    PRINT '✓ Index IX_Paiements_IdBilletEmis non trouvé (déjà supprimé)';
END

-- =====================================================
-- SECTION 6: SUPPRESSION DES COLONNES
-- =====================================================

PRINT '';
PRINT 'SECTION 6: SUPPRESSION DES COLONNES';

-- Supprimer la colonne DateEmissionBillet
IF EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Paiements' 
      AND COLUMN_NAME = 'DateEmissionBillet'
)
BEGIN
    PRINT 'Suppression de la colonne DateEmissionBillet...';
    
    ALTER TABLE Paiements
    DROP COLUMN DateEmissionBillet;
    
    PRINT '✓ Colonne DateEmissionBillet supprimée';
END
ELSE
BEGIN
    PRINT '✓ Colonne DateEmissionBillet non trouvée (déjà supprimée)';
END

-- Supprimer la colonne IdBilletEmis
IF EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Paiements' 
      AND COLUMN_NAME = 'IdBilletEmis'
)
BEGIN
    PRINT 'Suppression de la colonne IdBilletEmis...';
    
    ALTER TABLE Paiements
    DROP COLUMN IdBilletEmis;
    
    PRINT '✓ Colonne IdBilletEmis supprimée';
END
ELSE
BEGIN
    PRINT '✓ Colonne IdBilletEmis non trouvée (déjà supprimée)';
END

-- =====================================================
-- SECTION 7: NETTOYAGE DES BILLETS AUTOMATIQUES
-- =====================================================

PRINT '';
PRINT 'SECTION 7: NETTOYAGE DES BILLETS AUTOMATIQUES';

-- Optionnel: Supprimer les billets créés automatiquement
-- ATTENTION: Cette opération est destructive
DECLARE @BilletsAutomatiques INT;
SELECT @BilletsAutomatiques = COUNT(*) 
FROM Billets b
WHERE b.DateGeneration >= DATEADD(day, -1, GETDATE())
  AND EXISTS (
    SELECT 1 
    FROM Paiements_Billets_Backup backup 
    WHERE backup.IdBilletEmis = b.Id
  );

IF @BilletsAutomatiques > 0
BEGIN
    PRINT '⚠️  ' + CAST(@BilletsAutomatiques AS VARCHAR) + ' billets automatiques détectés';
    PRINT 'ATTENDEZ 5 SECONDES POUR CONFIRMER LA SUPPRESSION...';
    -- WAITFOR DELAY '00:00:05'; -- Décommenter pour une pause automatique
    
    -- Supprimer les billets automatiques (optionnel - décommenter si nécessaire)
    /*
    DELETE FROM Billets
    WHERE Id IN (
        SELECT IdBilletEmis 
        FROM Paiements_Billets_Backup 
        WHERE DateRollback >= DATEADD(day, -1, GETDATE())
    );
    
    PRINT '✓ Billets automatiques supprimés';
    */
    
    PRINT 'ℹ️  Suppression des billets automatiques commentée (manuelle)';
END
ELSE
BEGIN
    PRINT '✓ Aucun billet automatique récent détecté';
END

-- =====================================================
-- SECTION 8: VALIDATION DU ROLLBACK
-- =====================================================

PRINT '';
PRINT 'SECTION 8: VALIDATION DU ROLLBACK';

-- Vérifier que les colonnes ont été supprimées
DECLARE @ColonnesRestantes INT = 0;

IF EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Paiements' 
      AND COLUMN_NAME IN ('DateEmissionBillet', 'IdBilletEmis')
)
BEGIN
    PRINT '❌ ERREUR: Des colonnes du workflow sont toujours présentes';
    SET @ColonnesRestantes = @ColonnesRestantes + 1;
END
ELSE
BEGIN
    PRINT '✓ Toutes les colonnes du workflow ont été supprimées';
END

-- Vérifier que les index ont été supprimés
DECLARE @IndexRestants INT = 0;

IF EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE object_id = OBJECT_ID('Paiements')
      AND name IN ('IX_Paiements_DateEmissionBillet', 'IX_Paiements_IdBilletEmis')
)
BEGIN
    PRINT '❌ ERREUR: Des index du workflow sont toujours présents';
    SET @IndexRestants = @IndexRestants + 1;
END
ELSE
BEGIN
    PRINT '✓ Tous les index du workflow ont été supprimés';
END

-- Vérifier que la clé étrangère a été supprimée
IF EXISTS (
    SELECT 1 
    FROM sys.foreign_keys 
    WHERE name = 'FK_Paiements_Billets_IdBilletEmis' 
      AND parent_object_id = OBJECT_ID('Paiements')
)
BEGIN
    PRINT '❌ ERREUR: La clé étrangère du workflow est toujours présente';
    SET @ColonnesRestantes = @ColonnesRestantes + 1;
END
ELSE
BEGIN
    PRINT '✓ La clé étrangère du workflow a été supprimée';
END

-- =====================================================
-- SECTION 9: LOG DU ROLLBACK
-- =====================================================

PRINT '';
PRINT 'SECTION 9: LOG DU ROLLBACK';

-- Créer une table de log si elle n'existe pas
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RollbackLogs')
BEGIN
    CREATE TABLE RollbackLogs (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        RollbackName VARCHAR(255) NOT NULL,
        Version VARCHAR(50) NOT NULL,
        DateExecution DATETIME NOT NULL DEFAULT GETDATE(),
        Statut VARCHAR(50) NOT NULL,
        Message TEXT NULL,
        Utilisateur VARCHAR(100) NULL,
        ColonnesSupprimees INT NULL,
        IndexSupprimes INT NULL,
        PaiementsAvecBillets INT NULL,
        DonneesSauvegardees INT NULL
    );
    
    PRINT '✓ Table RollbackLogs créée';
END

-- Insérer le log de rollback
INSERT INTO RollbackLogs (
    RollbackName, 
    Version, 
    Statut, 
    Message, 
    Utilisateur,
    ColonnesSupprimees,
    IndexSupprimes,
    PaiementsAvecBillets,
    DonneesSauvegardees
) VALUES (
    'Workflow Paiement→Billet',
    '1.3.0',
    CASE WHEN @ColonnesRestantes = 0 AND @IndexRestants = 0 THEN 'SUCCÈS' ELSE 'ÉCHEC_PARTIEL' END,
    CASE 
        WHEN @ColonnesRestantes = 0 AND @IndexRestants = 0 
        THEN 'Rollback effectué avec succès'
        ELSE 'Rollback partiel - des éléments restent présents'
    END,
    SUSER_NAME(),
    2, -- DateEmissionBillet, IdBilletEmis
    2, -- IX_Paiements_DateEmissionBillet, IX_Paiements_IdBilletEmis
    @PaiementsAvecBillets,
    CASE WHEN @PaiementsAvecBillets > 0 THEN @PaiementsAvecBillets ELSE 0 END
);

PRINT '✓ Log de rollback enregistré';

-- =====================================================
-- SECTION 10: INSTRUCTIONS POST-ROLLBACK
-- =====================================================

PRINT '';
PRINT 'SECTION 10: INSTRUCTIONS POST-ROLLBACK';
PRINT '';

IF @ColonnesRestantes = 0 AND @IndexRestants = 0
BEGIN
    PRINT '=====================================================';
    PRINT 'ROLLBACK TERMINÉ AVEC SUCCÈS!';
    PRINT '=====================================================';
    PRINT '';
    PRINT 'Actions requises après le rollback:';
    PRINT '1. Redémarrer l''application CongoTravel API';
    PRINT '2. Vérifier que l''application fonctionne sans le workflow';
    PRINT '3. Tester la création de paiements (sans émission automatique)';
    PRINT '4. Monitorer les erreurs dans les logs';
    PRINT '';
    PRINT 'Données sauvegardées dans Paiements_Billets_Backup:';
    PRINT '  - ' + CAST(@PaiementsAvecBillets AS VARCHAR) + ' enregistrements';
    PRINT '  - Table disponible pour restauration si nécessaire';
    PRINT '';
    PRINT 'Pour réappliquer le workflow:';
    PRINT '  - Exécuter /Scripts/Production_Apply_Workflow_Migrations.sql';
    PRINT '';
    PRINT 'Timestamp final: ' + CONVERT(VARCHAR, GETDATE(), 120);
    PRINT '=====================================================';
END
ELSE
BEGIN
    PRINT '=====================================================';
    PRINT 'ROLLBACK PARTIEL - DES ÉLÉMENTS RESTENT PRÉSENTS!';
    PRINT '=====================================================';
    PRINT '';
    PRINT 'Éléments restants:';
    PRINT '  - Colonnes: ' + CAST(@ColonnesRestantes AS VARCHAR);
    PRINT '  - Index: ' + CAST(@IndexRestants AS VARCHAR);
    PRINT '';
    PRINT 'Actions requises:';
    PRINT '1. Vérifier manuellement les éléments restants';
    PRINT '2. Corriger les problèmes identifiés';
    PRINT '3. Réexécuter ce script si nécessaire';
    PRINT '';
    PRINT '=====================================================';
END

-- Afficher un résumé final
SELECT 
    'Rollback Workflow' as Nom,
    '1.3.0' as Version,
    GETDATE() as DateExecution,
    CASE 
        WHEN @ColonnesRestantes = 0 AND @IndexRestants = 0 
        THEN 'SUCCÈS' 
        ELSE 'ÉCHEC_PARTIEL' 
    END as Statut,
    @PaiementsAvecBillets as PaiementsAvecBillets,
    @ColonnesRestantes as ColonnesRestantes,
    @IndexRestants as IndexRestants,
    CASE 
        WHEN @ColonnesRestantes = 0 AND @IndexRestants = 0 
        THEN 'Système restauré à l''état précédent' 
        ELSE 'Restauration partielle - intervention manuelle requise' 
    END as Message;

PRINT '';
PRINT '🔄 Rollback terminé. Le système est revenu à l''état précédent.';

-- =====================================================
-- FIN DU SCRIPT
-- =====================================================
