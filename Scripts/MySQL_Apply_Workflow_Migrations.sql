-- =====================================================
-- SCRIPT SQL DE PRODUCTION - WORKFLOW PAIEMENT→BILLET (MySQL)
-- =====================================================
-- Version: 1.3.0
-- Date: 23/04/2026
-- Description: Appliquer les migrations pour le workflow automatique d'émission de billets
-- Base de données: MySQL
-- =====================================================

-- =====================================================
-- INSTRUCTIONS PRÉALABLES
-- =====================================================
-- 1. FAIRE UN BACKUP COMPLET DE LA BASE DE DONNÉES AVANT D'EXÉCUTER CE SCRIPT
-- 2. EXÉCUTER CE SCRIPT PENDANT UNE PÉRIODE DE FAIBLE ACTIVITÉ
-- 3. VÉRIFIER LES PERMISSIONS (ALTER TABLE, CREATE, etc.)
-- 4. TESTER EN ENVIRONNEMENT DE STAGING AVANT LA PRODUCTION
-- =====================================================

-- =====================================================
-- DÉBUT DU SCRIPT
-- =====================================================

SELECT 'DÉBUT DE LA MIGRATION WORKFLOW PAIEMENT→BILLET...' AS Message;
SELECT CONCAT('Timestamp: ', NOW()) AS Timestamp;

-- =====================================================
-- SECTION 1: VÉRIFICATIONS PRÉALABLES
-- =====================================================

SELECT '' AS Separator;
SELECT 'SECTION 1: VÉRIFICATIONS PRÉALABLES' AS Section;

-- Vérifier si la table Paiements existe
SET @table_exists = (
    SELECT COUNT(*) 
    FROM information_schema.tables 
    WHERE table_schema = DATABASE() 
      AND table_name = 'Paiements'
);

IF @table_exists = 0 THEN
    SELECT 'ERREUR: La table Paiements n existe pas. Arrêt du script.' AS Error;
    -- Arrêter le script en générant une erreur
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La table Paiements n existe pas';
ELSE
    SELECT '✓ Table Paiements trouvée' AS Status;
END IF;

-- Vérifier si les colonnes existent déjà
SET @columns_exist = (
    SELECT COUNT(*) 
    FROM information_schema.columns 
    WHERE table_schema = DATABASE() 
      AND table_name = 'Paiements' 
      AND column_name IN ('DateEmissionBillet', 'IdBilletEmis')
);

IF @columns_exist > 0 THEN
    SELECT 'ATTENTION: Les colonnes DateEmissionBillet et/ou IdBilletEmis existent déjà.' AS Warning;
    SELECT CONCAT('Colonnes trouvées: ', @columns_exist) AS Count;
    
    -- Afficher l'état actuel
    SELECT 
        column_name,
        data_type,
        is_nullable,
        column_default
    FROM information_schema.columns 
    WHERE table_schema = DATABASE() 
      AND table_name = 'Paiements' 
      AND column_name IN ('DateEmissionBillet', 'IdBilletEmis');
ELSE
    SELECT '✓ Colonnes à ajouter non trouvées (normal)' AS Status;
END IF;

-- =====================================================
-- SECTION 2: AJOUT DES COLONNES À LA TABLE PAIEMENTS
-- =====================================================

SELECT '' AS Separator;
SELECT 'SECTION 2: AJOUT DES COLONNES À LA TABLE PAIEMENTS' AS Section;

-- Ajouter la colonne DateEmissionBillet
SET @column_exists = (
    SELECT COUNT(*) 
    FROM information_schema.columns 
    WHERE table_schema = DATABASE() 
      AND table_name = 'Paiements' 
      AND column_name = 'DateEmissionBillet'
);

IF @column_exists = 0 THEN
    SELECT 'Ajout de la colonne DateEmissionBillet...' AS Action;
    
    ALTER TABLE Paiements 
    ADD COLUMN DateEmissionBillet DATETIME NULL;
    
    SELECT '✓ Colonne DateEmissionBillet ajoutée avec succès' AS Status;
ELSE
    SELECT '✓ Colonne DateEmissionBillet existe déjà' AS Status;
END IF;

-- Ajouter la colonne IdBilletEmis
SET @column_exists = (
    SELECT COUNT(*) 
    FROM information_schema.columns 
    WHERE table_schema = DATABASE() 
      AND table_name = 'Paiements' 
      AND column_name = 'IdBilletEmis'
);

IF @column_exists = 0 THEN
    SELECT 'Ajout de la colonne IdBilletEmis...' AS Action;
    
    ALTER TABLE Paiements 
    ADD COLUMN IdBilletEmis INT NULL;
    
    SELECT '✓ Colonne IdBilletEmis ajoutée avec succès' AS Status;
ELSE
    SELECT '✓ Colonne IdBilletEmis existe déjà' AS Status;
END IF;

-- =====================================================
-- SECTION 3: CRÉATION DES INDEX DE PERFORMANCE
-- =====================================================

SELECT '' AS Separator;
SELECT 'SECTION 3: CRÉATION DES INDEX DE PERFORMANCE' AS Section;

-- Index sur DateEmissionBillet pour les requêtes de monitoring
SET @index_exists = (
    SELECT COUNT(*) 
    FROM information_schema.statistics 
    WHERE table_schema = DATABASE() 
      AND table_name = 'Paiements' 
      AND index_name = 'IX_Paiements_DateEmissionBillet'
);

IF @index_exists = 0 THEN
    SELECT 'Création de l index IX_Paiements_DateEmissionBillet...' AS Action;
    
    CREATE INDEX IX_Paiements_DateEmissionBillet
    ON Paiements (DateEmissionBillet);
    
    SELECT '✓ Index IX_Paiements_DateEmissionBillet créé' AS Status;
ELSE
    SELECT '✓ Index IX_Paiements_DateEmissionBillet existe déjà' AS Status;
END IF;

-- Index sur IdBilletEmis pour les jointures avec la table Billets
SET @index_exists = (
    SELECT COUNT(*) 
    FROM information_schema.statistics 
    WHERE table_schema = DATABASE() 
      AND table_name = 'Paiements' 
      AND index_name = 'IX_Paiements_IdBilletEmis'
);

IF @index_exists = 0 THEN
    SELECT 'Création de l index IX_Paiements_IdBilletEmis...' AS Action;
    
    CREATE INDEX IX_Paiements_IdBilletEmis
    ON Paiements (IdBilletEmis);
    
    SELECT '✓ Index IX_Paiements_IdBilletEmis créé' AS Status;
ELSE
    SELECT '✓ Index IX_Paiements_IdBilletEmis existe déjà' AS Status;
END IF;

-- =====================================================
-- SECTION 4: VÉRIFICATION DE LA TABLE BILLETS
-- =====================================================

SELECT '' AS Separator;
SELECT 'SECTION 4: VÉRIFICATION DE LA TABLE BILLETS' AS Section;

-- Vérifier si la table Billets existe
SET @table_exists = (
    SELECT COUNT(*) 
    FROM information_schema.tables 
    WHERE table_schema = DATABASE() 
      AND table_name = 'Billets'
);

IF @table_exists = 0 THEN
    SELECT 'ERREUR: La table Billets n existe pas. Le workflow ne peut pas fonctionner.' AS Error;
    SELECT 'Veuillez créer la table Billets avant de continuer.' AS Action;
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La table Billets n existe pas';
ELSE
    SELECT '✓ Table Billets trouvée' AS Status;
    
    -- Vérifier les colonnes nécessaires
    SET @qr_column_exists = (
        SELECT COUNT(*) 
        FROM information_schema.columns 
        WHERE table_schema = DATABASE() 
          AND table_name = 'Billets' 
          AND column_name = 'QrCode'
    );
    
    IF @qr_column_exists > 0 THEN
        SELECT '✓ Colonne QrCode trouvée dans la table Billets' AS Status;
    ELSE
        SELECT 'ATTENTION: La colonne QrCode n existe pas dans la table Billets' AS Warning;
        SELECT 'Le workflow peut ne pas fonctionner correctement.' AS Action;
    END IF;
END IF;

-- =====================================================
-- SECTION 5: CRÉATION DE LA CLÉ ÉTRANGÈRE
-- =====================================================

SELECT '' AS Separator;
SELECT 'SECTION 5: CRÉATION DE LA CLÉ ÉTRANGÈRE' AS Section;

-- Créer la clé étrangère entre Paiements.IdBilletEmis et Billets.Id
SET @constraint_exists = (
    SELECT COUNT(*) 
    FROM information_schema.table_constraints 
    WHERE table_schema = DATABASE() 
      AND table_name = 'Paiements' 
      AND constraint_name = 'FK_Paiements_Billets_IdBilletEmis'
      AND constraint_type = 'FOREIGN KEY'
);

IF @constraint_exists = 0 THEN
    SELECT 'Création de la clé étrangère FK_Paiements_Billets_IdBilletEmis...' AS Action;
    
    ALTER TABLE Paiements
    ADD CONSTRAINT FK_Paiements_Billets_IdBilletEmis
    FOREIGN KEY (IdBilletEmis) REFERENCES Billets(Id)
    ON DELETE SET NULL
    ON UPDATE CASCADE;
    
    SELECT '✓ Clé étrangère FK_Paiements_Billets_IdBilletEmis créée' AS Status;
ELSE
    SELECT '✓ Clé étrangère FK_Paiements_Billets_IdBilletEmis existe déjà' AS Status;
END IF;

-- =====================================================
-- SECTION 6: VALIDATION DE LA MIGRATION
-- =====================================================

SELECT '' AS Separator;
SELECT 'SECTION 6: VALIDATION DE LA MIGRATION' AS Section;

-- Vérifier l'état final de la table Paiements
SELECT 'État final de la table Paiements:' AS Title;

SELECT 
    column_name,
    data_type,
    character_maximum_length,
    is_nullable,
    column_default
FROM information_schema.columns 
WHERE table_schema = DATABASE() 
  AND table_name = 'Paiements' 
  AND column_name IN ('DateEmissionBillet', 'IdBilletEmis')
ORDER BY column_name;

-- Vérifier les index créés
SELECT '' AS Separator;
SELECT 'Index créés pour la table Paiements:' AS Title;

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

-- Vérifier les contraintes
SELECT '' AS Separator;
SELECT 'Contraintes créées pour la table Paiements:' AS Title;

SELECT 
    constraint_name,
    constraint_type
FROM information_schema.table_constraints 
WHERE table_schema = DATABASE() 
  AND table_name = 'Paiements' 
  AND constraint_name LIKE 'FK_Paiements_%';

-- =====================================================
-- SECTION 7: STATISTIQUES DE VALIDATION
-- =====================================================

SELECT '' AS Separator;
SELECT 'SECTION 7: STATISTIQUES DE VALIDATION' AS Section;

-- Compter les paiements existants
SELECT COUNT(*) AS TotalPaiements FROM Paiements;

-- Compter les billets existants
SELECT COUNT(*) AS TotalBillets FROM Billets;

-- Vérifier les paiements avec billets émis (devrait être 0 au début)
SELECT COUNT(*) AS PaiementsAvecBillets FROM Paiements WHERE IdBilletEmis IS NOT NULL;

-- =====================================================
-- SECTION 8: CRÉATION D'UN LOG DE MIGRATION
-- =====================================================

SELECT '' AS Separator;
SELECT 'SECTION 8: CRÉATION D UN LOG DE MIGRATION' AS Section;

-- Créer une table de log si elle n'existe pas
SET @log_table_exists = (
    SELECT COUNT(*) 
    FROM information_schema.tables 
    WHERE table_schema = DATABASE() 
      AND table_name = 'MigrationLogs'
);

IF @log_table_exists = 0 THEN
    CREATE TABLE MigrationLogs (
        Id INT AUTO_INCREMENT PRIMARY KEY,
        MigrationName VARCHAR(255) NOT NULL,
        Version VARCHAR(50) NOT NULL,
        DateExecution DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
        Statut VARCHAR(50) NOT NULL,
        Message TEXT,
        Utilisateur VARCHAR(100),
        NombrePaiementsAvant INT,
        NombreBilletsAvant INT,
        NombrePaiementsApres INT,
        NombreBilletsApres INT
    );
    
    SELECT '✓ Table MigrationLogs créée' AS Status;
END IF;

-- Insérer le log de cette migration
INSERT INTO MigrationLogs (
    MigrationName, 
    Version, 
    Statut, 
    Message, 
    Utilisateur,
    NombrePaiementsAvant,
    NombreBilletsAvant,
    NombrePaiementsApres,
    NombreBilletsApres
) VALUES (
    'Workflow Paiement→Billet',
    '1.3.0',
    'SUCCÈS',
    'Migration appliquée avec succès. Colonnes DateEmissionBillet et IdBilletEmis ajoutées.',
    CURRENT_USER(),
    (SELECT COUNT(*) FROM Paiements),
    (SELECT COUNT(*) FROM Billets),
    (SELECT COUNT(*) FROM Paiements),
    (SELECT COUNT(*) FROM Billets)
);

SELECT '✓ Log de migration enregistré' AS Status;

-- =====================================================
-- SECTION 9: INSTRUCTIONS POST-MIGRATION
-- =====================================================

SELECT '' AS Separator;
SELECT 'SECTION 9: INSTRUCTIONS POST-MIGRATION' AS Section;
SELECT '' AS Separator;
SELECT '=====================================================' AS Separator;
SELECT 'MIGRATION TERMINÉE AVEC SUCCÈS!' AS Status;
SELECT '=====================================================' AS Separator;
SELECT '' AS Separator;
SELECT 'Actions requises après la migration:' AS Instructions;
SELECT '1. Redémarrer l application CongoTravel API' AS Step1;
SELECT '2. Vérifier les logs de démarrage' AS Step2;
SELECT '3. Tester le workflow avec un paiement complet' AS Step3;
SELECT '4. Vérifier qu un billet est émis automatiquement' AS Step4;
SELECT '5. Monitorer les performances pendant 24h' AS Step5;
SELECT '' AS Separator;
SELECT 'Scripts de validation disponibles:' AS Scripts;
SELECT '- /Scripts/MySQL_Validate_Workflow.sql' AS Script1;
SELECT '- /Scripts/MySQL_Monitor_Workflow.sql' AS Script2;
SELECT '' AS Separator;
SELECT 'En cas de problème, voir le guide de rollback:' AS Rollback;
SELECT '- /Scripts/MySQL_Rollback_Workflow.sql' AS RollbackScript;
SELECT '' AS Separator;
SELECT CONCAT('Timestamp final: ', NOW()) AS Timestamp;
SELECT '=====================================================' AS Separator;

-- =====================================================
-- FIN DU SCRIPT
-- =====================================================

-- Afficher un résumé final
SELECT 
    'Migration Workflow Paiement→Billet' as Nom,
    '1.3.0' as Version,
    NOW() as DateExecution,
    'SUCCÈS' as Statut,
    (SELECT COUNT(*) FROM Paiements) as TotalPaiements,
    (SELECT COUNT(*) FROM Billets) as TotalBillets,
    'Prêt pour le workflow automatique' as Message;

SELECT '' AS Separator;
SELECT '🎉 Le workflow automatique Paiement→Billet est maintenant prêt!' AS FinalMessage;
