-- =====================================================
-- SCRIPT SQL DE ROLLBACK - WORKFLOW PAIEMENT→BILLET (MySQL)
-- =====================================================
-- Version: 1.3.0
-- Date: 23/04/2026
-- Description: Script de rollback pour le workflow automatique d'émission de billets
-- ATTENTION: Ce script supprime les colonnes ajoutées lors de la migration
-- Base de données: MySQL
-- =====================================================

-- =====================================================
-- INSTRUCTIONS IMPORTANTES
-- =====================================================
-- 1. FAIRE UN BACKUP COMPLET AVANT D'EXÉCUTER CE ROLLBACK
-- 2. ARRÊTER L'APPLICATION CONGOTRAVEL API AVANT L'EXÉCUTION
-- 3. CE SCRIPT EST DESTRUCTIF - IL SUPPRIME DES DONNÉES
-- 4. UTILISER SEULEMENT EN CAS DE PROBLÈME CRITIQUE
-- =====================================================

SELECT 'DÉBUT DU ROLLBACK WORKFLOW PAIEMENT→BILLET...' AS Message;
SELECT CONCAT('Timestamp: ', NOW()) AS Timestamp;
SELECT '⚠️  ATTENTION: CE SCRIPT EST DESTRUCTIF!' AS Warning;

-- =====================================================
-- SECTION 1: CONFIRMATION DE SÉCURITÉ
-- =====================================================

SELECT '' AS Separator;
SELECT 'SECTION 1: CONFIRMATION DE SÉCURITÉ' AS Section;

-- Vérifier que l'utilisateur est conscient des risques
SELECT '⚠️  CE ROLLBACK VA:' AS Risks;
SELECT '   - Supprimer les colonnes DateEmissionBillet et IdBilletEmis' AS Risk1;
SELECT '   - Supprimer les index associés' AS Risk2;
SELECT '   - Supprimer la clé étrangère' AS Risk3;
SELECT '   - PERDRE LA RÉFÉRENCE AUX BILLETS ÉMIS AUTOMATIQUEMENT' AS Risk4;
SELECT '' AS Separator;
SELECT 'ATTENDEZ 10 SECONDES PUIS APPUYEZ SUR ENTRÉE POUR CONTINUER...' AS Pause;

-- =====================================================
-- SECTION 2: VÉRIFICATIONS PRÉALABLES
-- =====================================================

SELECT '' AS Separator;
SELECT 'SECTION 2: VÉRIFICATIONS PRÉALABLES' AS Section;

-- Compter les paiements avec billets émis avant le rollback
SET @paiements_avec_billets = (
    SELECT COUNT(*) 
    FROM Paiements 
    WHERE IdBilletEmis IS NOT NULL
);

IF @paiements_avec_billets > 0 THEN
    SELECT '⚠️  ATTENTION: Des paiements ont des billets émis!' AS Warning;
    SELECT CONCAT('Nombre de paiements concernés: ', @paiements_avec_billets) AS Count;
    SELECT 'Ces références seront perdues après le rollback.' AS Loss;
    SELECT 'Considérez sauvegarder ces données avant de continuer.' AS Backup;
    
    -- Afficher les paiements concernés
    SELECT 'Paiements avec billets émis:' AS Title;
    SELECT 
        p.IdPaiement,
        p.DateCreation,
        p.DateEmissionBillet,
        p.IdBilletEmis,
        b.QrCode
    FROM Paiements p
    JOIN Billets b ON p.IdBilletEmis = b.Id
    WHERE p.IdBilletEmis IS NOT NULL
    LIMIT 10;
    
    IF @paiements_avec_billets > 10 THEN
        SELECT CONCAT('... et ', @paiements_avec_billets - 10, ' autres paiements') AS More;
    END IF;
ELSE
    SELECT '✓ Aucun paiement avec billet émis détecté' AS Status;
END IF;

-- =====================================================
-- SECTION 3: SAUVEGARDE DES DONNÉES CRITIQUES
-- =====================================================

SELECT '' AS Separator;
SELECT 'SECTION 3: SAUVEGARDE DES DONNÉES CRITIQUES' AS Section;

-- Créer une table de sauvegarde si elle n'existe pas
SET @backup_table_exists = (
    SELECT COUNT(*) 
    FROM information_schema.tables 
    WHERE table_schema = DATABASE() 
      AND table_name = 'Paiements_Billets_Backup'
);

IF @backup_table_exists = 0 THEN
    CREATE TABLE Paiements_Billets_Backup (
        IdBackup INT AUTO_INCREMENT PRIMARY KEY,
        DateRollback DATETIME DEFAULT CURRENT_TIMESTAMP,
        IdPaiement INT,
        DateEmissionBillet DATETIME,
        IdBilletEmis INT,
        QrCode VARCHAR(255),
        DateGeneration DATETIME,
        IdReservation INT,
        IdClient INT,
        IdSociete INT
    );
    
    SELECT '✓ Table de sauvegarde Paiements_Billets_Backup créée' AS Status;
END IF;

-- Sauvegarder les paiements avec billets émis
IF @paiements_avec_billets > 0 THEN
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
    
    SELECT '✓ Données sauvegardées dans Paiements_Billets_Backup' AS Status;
    SELECT CONCAT('  - ', @paiements_avec_billets, ' enregistrements sauvegardés') AS Count;
END IF;

-- =====================================================
-- SECTION 4: SUPPRESSION DE LA CLÉ ÉTRANGÈRE
-- =====================================================

SELECT '' AS Separator;
SELECT 'SECTION 4: SUPPRESSION DE LA CLÉ ÉTRANGÈRE' AS Section;

SET @constraint_exists = (
    SELECT COUNT(*) 
    FROM information_schema.table_constraints 
    WHERE table_schema = DATABASE() 
      AND table_name = 'Paiements' 
      AND constraint_name = 'FK_Paiements_Billets_IdBilletEmis'
      AND constraint_type = 'FOREIGN KEY'
);

IF @constraint_exists > 0 THEN
    SELECT 'Suppression de la clé étrangère FK_Paiements_Billets_IdBilletEmis...' AS Action;
    
    ALTER TABLE Paiements
    DROP FOREIGN KEY FK_Paiements_Billets_IdBilletEmis;
    
    SELECT '✓ Clé étrangère supprimée' AS Status;
ELSE
    SELECT '✓ Clé étrangère non trouvée (déjà supprimée)' AS Status;
END IF;

-- =====================================================
-- SECTION 5: SUPPRESSION DES INDEX
-- =====================================================

SELECT '' AS Separator;
SELECT 'SECTION 5: SUPPRESSION DES INDEX' AS Section;

-- Supprimer l'index sur DateEmissionBillet
SET @index_exists = (
    SELECT COUNT(*) 
    FROM information_schema.statistics 
    WHERE table_schema = DATABASE() 
      AND table_name = 'Paiements' 
      AND index_name = 'IX_Paiements_DateEmissionBillet'
);

IF @index_exists > 0 THEN
    SELECT 'Suppression de l index IX_Paiements_DateEmissionBillet...' AS Action;
    
    DROP INDEX IX_Paiements_DateEmissionBillet ON Paiements;
    
    SELECT '✓ Index IX_Paiements_DateEmissionBillet supprimé' AS Status;
ELSE
    SELECT '✓ Index IX_Paiements_DateEmissionBillet non trouvé (déjà supprimé)' AS Status;
END IF;

-- Supprimer l'index sur IdBilletEmis
SET @index_exists = (
    SELECT COUNT(*) 
    FROM information_schema.statistics 
    WHERE table_schema = DATABASE() 
      AND table_name = 'Paiements' 
      AND index_name = 'IX_Paiements_IdBilletEmis'
);

IF @index_exists > 0 THEN
    SELECT 'Suppression de l index IX_Paiements_IdBilletEmis...' AS Action;
    
    DROP INDEX IX_Paiements_IdBilletEmis ON Paiements;
    
    SELECT '✓ Index IX_Paiements_IdBilletEmis supprimé' AS Status;
ELSE
    SELECT '✓ Index IX_Paiements_IdBilletEmis non trouvé (déjà supprimé)' AS Status;
END IF;

-- =====================================================
-- SECTION 6: SUPPRESSION DES COLONNES
-- =====================================================

SELECT '' AS Separator;
SELECT 'SECTION 6: SUPPRESSION DES COLONNES' AS Section;

-- Supprimer la colonne DateEmissionBillet
SET @column_exists = (
    SELECT COUNT(*) 
    FROM information_schema.columns 
    WHERE table_schema = DATABASE() 
      AND table_name = 'Paiements' 
      AND column_name = 'DateEmissionBillet'
);

IF @column_exists > 0 THEN
    SELECT 'Suppression de la colonne DateEmissionBillet...' AS Action;
    
    ALTER TABLE Paiements
    DROP COLUMN DateEmissionBillet;
    
    SELECT '✓ Colonne DateEmissionBillet supprimée' AS Status;
ELSE
    SELECT '✓ Colonne DateEmissionBillet non trouvée (déjà supprimée)' AS Status;
END IF;

-- Supprimer la colonne IdBilletEmis
SET @column_exists = (
    SELECT COUNT(*) 
    FROM information_schema.columns 
    WHERE table_schema = DATABASE() 
      AND table_name = 'Paiements' 
      AND column_name = 'IdBilletEmis'
);

IF @column_exists > 0 THEN
    SELECT 'Suppression de la colonne IdBilletEmis...' AS Action;
    
    ALTER TABLE Paiements
    DROP COLUMN IdBilletEmis;
    
    SELECT '✓ Colonne IdBilletEmis supprimée' AS Status;
ELSE
    SELECT '✓ Colonne IdBilletEmis non trouvée (déjà supprimée)' AS Status;
END IF;

-- =====================================================
-- SECTION 7: NETTOYAGE DES BILLETS AUTOMATIQUES
-- =====================================================

SELECT '' AS Separator;
SELECT 'SECTION 7: NETTOYAGE DES BILLETS AUTOMATIQUES' AS Section;

-- Optionnel: Supprimer les billets créés automatiquement
-- ATTENTION: Cette opération est destructive
SET @billets_automatiques = (
    SELECT COUNT(*) 
    FROM Billets b
    WHERE b.DateGeneration >= DATE_SUB(NOW(), INTERVAL 1 DAY)
      AND EXISTS (
        SELECT 1 
        FROM Paiements_Billets_Backup backup 
        WHERE backup.IdBilletEmis = b.Id
      )
);

IF @billets_automatiques > 0 THEN
    SELECT CONCAT('⚠️  ', @billets_automatiques, ' billets automatiques détectés') AS Warning;
    SELECT 'ATTENDEZ 5 SECONDES POUR CONFIRMER LA SUPPRESSION...' AS Pause;
    
    -- Supprimer les billets automatiques (optionnel - décommenter si nécessaire)
    /*
    DELETE FROM Billets
    WHERE Id IN (
        SELECT IdBilletEmis 
        FROM Paiements_Billets_Backup 
        WHERE DateRollback >= DATE_SUB(NOW(), INTERVAL 1 DAY)
    );
    
    SELECT '✓ Billets automatiques supprimés' AS Status;
    */
    
    SELECT 'ℹ️  Suppression des billets automatiques commentée (manuelle)' AS Info;
ELSE
    SELECT '✓ Aucun billet automatique récent détecté' AS Status;
END IF;

-- =====================================================
-- SECTION 8: VALIDATION DU ROLLBACK
-- =====================================================

SELECT '' AS Separator;
SELECT 'SECTION 8: VALIDATION DU ROLLBACK' AS Section;

-- Vérifier que les colonnes ont été supprimées
SET @colonnes_restantes = (
    SELECT COUNT(*) 
    FROM information_schema.columns 
    WHERE table_schema = DATABASE() 
      AND table_name = 'Paiements' 
      AND column_name IN ('DateEmissionBillet', 'IdBilletEmis')
);

-- Vérifier que les index ont été supprimés
SET @index_restants = (
    SELECT COUNT(*) 
    FROM information_schema.statistics 
    WHERE table_schema = DATABASE() 
      AND table_name = 'Paiements' 
      AND index_name IN ('IX_Paiements_DateEmissionBillet', 'IX_Paiements_IdBilletEmis')
);

-- Vérifier que la clé étrangère a été supprimée
SET @constraint_restante = (
    SELECT COUNT(*) 
    FROM information_schema.table_constraints 
    WHERE table_schema = DATABASE() 
      AND table_name = 'Paiements' 
      AND constraint_name = 'FK_Paiements_Billets_IdBilletEmis'
      AND constraint_type = 'FOREIGN KEY'
);

IF @colonnes_restantes > 0 THEN
    SELECT '❌ ERREUR: Des colonnes du workflow sont toujours présentes' AS Error;
ELSE
    SELECT '✓ Toutes les colonnes du workflow ont été supprimées' AS Status;
END IF;

IF @index_restants > 0 THEN
    SELECT '❌ ERREUR: Des index du workflow sont toujours présents' AS Error;
ELSE
    SELECT '✓ Tous les index du workflow ont été supprimés' AS Status;
END IF;

IF @constraint_restante > 0 THEN
    SELECT '❌ ERREUR: La clé étrangère du workflow est toujours présente' AS Error;
ELSE
    SELECT '✓ La clé étrangère du workflow a été supprimée' AS Status;
END IF;

-- =====================================================
-- SECTION 9: LOG DU ROLLBACK
-- =====================================================

SELECT '' AS Separator;
SELECT 'SECTION 9: LOG DU ROLLBACK' AS Section;

-- Créer une table de log si elle n'existe pas
SET @log_table_exists = (
    SELECT COUNT(*) 
    FROM information_schema.tables 
    WHERE table_schema = DATABASE() 
      AND table_name = 'RollbackLogs'
);

IF @log_table_exists = 0 THEN
    CREATE TABLE RollbackLogs (
        Id INT AUTO_INCREMENT PRIMARY KEY,
        RollbackName VARCHAR(255) NOT NULL,
        Version VARCHAR(50) NOT NULL,
        DateExecution DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
        Statut VARCHAR(50) NOT NULL,
        Message TEXT,
        Utilisateur VARCHAR(100),
        ColonnesSupprimees INT,
        IndexSupprimes INT,
        PaiementsAvecBillets INT,
        DonneesSauvegardees INT
    );
    
    SELECT '✓ Table RollbackLogs créée' AS Status;
END IF;

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
    CASE 
        WHEN @colonnes_restantes = 0 AND @index_restants = 0 THEN 'SUCCÈS' 
        ELSE 'ÉCHEC_PARTIEL' 
    END,
    CASE 
        WHEN @colonnes_restantes = 0 AND @index_restants = 0 
        THEN 'Rollback effectué avec succès'
        ELSE 'Rollback partiel - des éléments restent présents'
    END,
    CURRENT_USER(),
    2, -- DateEmissionBillet, IdBilletEmis
    2, -- IX_Paiements_DateEmissionBillet, IX_Paiements_IdBilletEmis
    @paiements_avec_billets,
    CASE WHEN @paiements_avec_billets > 0 THEN @paiements_avec_billets ELSE 0 END
);

SELECT '✓ Log de rollback enregistré' AS Status;

-- =====================================================
-- SECTION 10: INSTRUCTIONS POST-ROLLBACK
-- =====================================================

SELECT '' AS Separator;
SELECT 'SECTION 10: INSTRUCTIONS POST-ROLLBACK' AS Section;
SELECT '' AS Separator;

IF @colonnes_restantes = 0 AND @index_restants = 0 THEN
    SELECT '=====================================================' AS Separator;
    SELECT 'ROLLBACK TERMINÉ AVEC SUCCÈS!' AS Status;
    SELECT '=====================================================' AS Separator;
    SELECT '' AS Separator;
    SELECT 'Actions requises après le rollback:' AS Actions;
    SELECT '1. Redémarrer l application CongoTravel API' AS Action1;
    SELECT '2. Vérifier que l application fonctionne sans le workflow' AS Action2;
    SELECT '3. Tester la création de paiements (sans émission automatique)' AS Action3;
    SELECT '4. Monitorer les erreurs dans les logs' AS Action4;
    SELECT '' AS Separator;
    SELECT 'Données sauvegardées dans Paiements_Billets_Backup:' AS Backup;
    SELECT CONCAT('  - ', @paiements_avec_billets, ' enregistrements') AS Count;
    SELECT '  - Table disponible pour restauration si nécessaire' AS Availability;
    SELECT '' AS Separator;
    SELECT 'Pour réappliquer le workflow:' AS Reapply;
    SELECT '  - Exécuter /Scripts/MySQL_Apply_Workflow_Migrations.sql' AS Script;
    SELECT '' AS Separator;
    SELECT CONCAT('Timestamp final: ', NOW()) AS Timestamp;
    SELECT '=====================================================' AS Separator;
ELSE
    SELECT '=====================================================' AS Separator;
    SELECT 'ROLLBACK PARTIEL - DES ÉLÉMENTS RESTENT PRÉSENTS!' AS Status;
    SELECT '=====================================================' AS Separator;
    SELECT '' AS Separator;
    SELECT 'Éléments restants:' AS Remaining;
    SELECT CONCAT('  - Colonnes: ', @colonnes_restantes) AS Columns;
    SELECT CONCAT('  - Index: ', @index_restants) AS Indexes;
    SELECT CONCAT('  - Contraintes: ', @constraint_restante) AS Constraints;
    SELECT '' AS Separator;
    SELECT 'Actions requises:' AS Actions;
    SELECT '1. Vérifier manuellement les éléments restants' AS Action1;
    SELECT '2. Corriger les problèmes identifiés' AS Action2;
    SELECT '3. Réexécuter ce script si nécessaire' AS Action3;
    SELECT '' AS Separator;
    SELECT '=====================================================' AS Separator;
END IF;

-- Afficher un résumé final
SELECT 
    'Rollback Workflow' as Nom,
    '1.3.0' as Version,
    NOW() as DateExecution,
    CASE 
        WHEN @colonnes_restantes = 0 AND @index_restants = 0 
        THEN 'SUCCÈS' 
        ELSE 'ÉCHEC_PARTIEL' 
    END as Statut,
    @paiements_avec_billets as PaiementsAvecBillets,
    @colonnes_restantes as ColonnesRestantes,
    @index_restants as IndexRestants,
    CASE 
        WHEN @colonnes_restantes = 0 AND @index_restants = 0 
        THEN 'Système restauré à l état précédent' 
        ELSE 'Restauration partielle - intervention manuelle requise' 
    END as Message;

SELECT '' AS Separator;
SELECT '🔄 Rollback terminé. Le système est revenu à l état précédent.' AS FinalMessage;

-- =====================================================
-- FIN DU SCRIPT
-- =====================================================
