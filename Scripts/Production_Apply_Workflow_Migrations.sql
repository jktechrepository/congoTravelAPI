-- =====================================================
-- SCRIPT SQL DE PRODUCTION - WORKFLOW PAIEMENT→BILLET
-- =====================================================
-- Version: 1.3.0
-- Date: 23/04/2026
-- Description: Appliquer les migrations pour le workflow automatique d'émission de billets
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

PRINT 'DÉBUT DE LA MIGRATION WORKFLOW PAIEMENT→BILLET...';
PRINT 'Timestamp: ' + CONVERT(VARCHAR, GETDATE(), 120);

-- =====================================================
-- SECTION 1: VÉRIFICATIONS PRÉALABLES
-- =====================================================

PRINT '';
PRINT 'SECTION 1: VÉRIFICATIONS PRÉALABLES';

-- Vérifier si la table Paiements existe
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Paiements')
BEGIN
    PRINT 'ERREUR: La table Paiements n''existe pas. Arrêt du script.';
    RETURN;
END
ELSE
BEGIN
    PRINT '✓ Table Paiements trouvée';
END

-- Vérifier si les colonnes existent déjà
IF EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Paiements' 
      AND COLUMN_NAME IN ('DateEmissionBillet', 'IdBilletEmis')
)
BEGIN
    PRINT 'ATTENTION: Les colonnes DateEmissionBillet et/ou IdBilletEmis existent déjà.';
    PRINT 'Vérification de l''état actuel...';
    
    SELECT 
        COLUMN_NAME,
        DATA_TYPE,
        IS_NULLABLE,
        COLUMN_DEFAULT
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Paiements' 
      AND COLUMN_NAME IN ('DateEmissionBillet', 'IdBilletEmis');
    
    -- Si les colonnes existent déjà, on passe à la section 2
    GOTO SECTION_2;
END
ELSE
BEGIN
    PRINT '✓ Colonnes à ajouter non trouvées (normal)';
END

-- =====================================================
-- SECTION 2: AJOUT DES COLONNES À LA TABLE PAIEMENTS
-- =====================================================

SECTION_2:
PRINT '';
PRINT 'SECTION 2: AJOUT DES COLONNES À LA TABLE PAIEMENTS';

-- Ajouter la colonne DateEmissionBillet
IF NOT EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Paiements' 
      AND COLUMN_NAME = 'DateEmissionBillet'
)
BEGIN
    PRINT 'Ajout de la colonne DateEmissionBillet...';
    
    ALTER TABLE Paiements 
    ADD DateEmissionBillet DATETIME NULL;
    
    PRINT '✓ Colonne DateEmissionBillet ajoutée avec succès';
END
ELSE
BEGIN
    PRINT '✓ Colonne DateEmissionBillet existe déjà';
END

-- Ajouter la colonne IdBilletEmis
IF NOT EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Paiements' 
      AND COLUMN_NAME = 'IdBilletEmis'
)
BEGIN
    PRINT 'Ajout de la colonne IdBilletEmis...';
    
    ALTER TABLE Paiements 
    ADD IdBilletEmis INT NULL;
    
    PRINT '✓ Colonne IdBilletEmis ajoutée avec succès';
END
ELSE
BEGIN
    PRINT '✓ Colonne IdBilletEmis existe déjà';
END

-- =====================================================
-- SECTION 3: CRÉATION DES INDEX DE PERFORMANCE
-- =====================================================

PRINT '';
PRINT 'SECTION 3: CRÉATION DES INDEX DE PERFORMANCE';

-- Index sur DateEmissionBillet pour les requêtes de monitoring
IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = 'IX_Paiements_DateEmissionBillet' 
      AND object_id = OBJECT_ID('Paiements')
)
BEGIN
    PRINT 'Création de l''index IX_Paiements_DateEmissionBillet...';
    
    CREATE NONCLUSTERED INDEX IX_Paiements_DateEmissionBillet
    ON Paiements (DateEmissionBillet);
    
    PRINT '✓ Index IX_Paiements_DateEmissionBillet créé';
END
ELSE
BEGIN
    PRINT '✓ Index IX_Paiements_DateEmissionBillet existe déjà';
END

-- Index sur IdBilletEmis pour les jointures avec la table Billets
IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = 'IX_Paiements_IdBilletEmis' 
      AND object_id = OBJECT_ID('Paiements')
)
BEGIN
    PRINT 'Création de l''index IX_Paiements_IdBilletEmis...';
    
    CREATE NONCLUSTERED INDEX IX_Paiements_IdBilletEmis
    ON Paiements (IdBilletEmis);
    
    PRINT '✓ Index IX_Paiements_IdBilletEmis créé';
END
ELSE
BEGIN
    PRINT '✓ Index IX_Paiements_IdBilletEmis existe déjà';
END

-- =====================================================
-- SECTION 4: VÉRIFICATION DE LA TABLE BILLETS
-- =====================================================

PRINT '';
PRINT 'SECTION 4: VÉRIFICATION DE LA TABLE BILLETS';

-- Vérifier si la table Billets existe
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Billets')
BEGIN
    PRINT 'ERREUR: La table Billets n''existe pas. Le workflow ne peut pas fonctionner.';
    PRINT 'Veuillez créer la table Billets avant de continuer.';
    RETURN;
END
ELSE
BEGIN
    PRINT '✓ Table Billets trouvée';
    
    -- Vérifier les colonnes nécessaires
    IF EXISTS (
        SELECT 1 
        FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_NAME = 'Billets' 
          AND COLUMN_NAME = 'QrCode'
    )
    BEGIN
        PRINT '✓ Colonne QrCode trouvée dans la table Billets';
    END
    ELSE
    BEGIN
        PRINT 'ATTENTION: La colonne QrCode n''existe pas dans la table Billets';
        PRINT 'Le workflow peut ne pas fonctionner correctement.';
    END
END

-- =====================================================
-- SECTION 5: CRÉATION DE LA CLÉ ÉTRANGÈRE
-- =====================================================

PRINT '';
PRINT 'SECTION 5: CRÉATION DE LA CLÉ ÉTRANGÈRE';

-- Créer la clé étrangère entre Paiements.IdBilletEmis et Billets.Id
IF NOT EXISTS (
    SELECT 1 
    FROM sys.foreign_keys 
    WHERE name = 'FK_Paiements_Billets_IdBilletEmis' 
      AND parent_object_id = OBJECT_ID('Paiements')
)
BEGIN
    PRINT 'Création de la clé étrangère FK_Paiements_Billets_IdBilletEmis...';
    
    ALTER TABLE Paiements
    ADD CONSTRAINT FK_Paiements_Billets_IdBilletEmis
    FOREIGN KEY (IdBilletEmis) REFERENCES Billets(Id)
    ON DELETE SET NULL
    ON UPDATE CASCADE;
    
    PRINT '✓ Clé étrangère FK_Paiements_Billets_IdBilletEmis créée';
END
ELSE
BEGIN
    PRINT '✓ Clé étrangère FK_Paiements_Billets_IdBilletEmis existe déjà';
END

-- =====================================================
-- SECTION 6: VALIDATION DE LA MIGRATION
-- =====================================================

PRINT '';
PRINT 'SECTION 6: VALIDATION DE LA MIGRATION';

-- Vérifier l'état final de la table Paiements
PRINT 'État final de la table Paiements:';

SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Paiements' 
  AND COLUMN_NAME IN ('DateEmissionBillet', 'IdBilletEmis')
ORDER BY COLUMN_NAME;

-- Vérifier les index créés
PRINT '';
PRINT 'Index créés pour la table Paiements:';

SELECT 
    i.name AS IndexName,
    i.type_desc AS IndexType,
    STRING_AGG(c.name, ', ') AS Columns
FROM sys.indexes i
JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.object_id = OBJECT_ID('Paiements')
  AND i.name LIKE 'IX_Paiements_%'
GROUP BY i.name, i.type_desc
ORDER BY i.name;

-- Vérifier les contraintes
PRINT '';
PRINT 'Contraintes créées pour la table Paiements:';

SELECT 
    name AS ConstraintName,
    type_desc AS ConstraintType,
    is_disabled AS IsDisabled
FROM sys.foreign_keys
WHERE parent_object_id = OBJECT_ID('Paiements')
  AND name LIKE 'FK_Paiements_%';

-- =====================================================
-- SECTION 7: STATISTIQUES DE VALIDATION
-- =====================================================

PRINT '';
PRINT 'SECTION 7: STATISTIQUES DE VALIDATION';

-- Compter les paiements existants
DECLARE @TotalPaiements INT;
SELECT @TotalPaiements = COUNT(*) FROM Paiements;
PRINT 'Nombre total de paiements existants: ' + CAST(@TotalPaiements AS VARCHAR);

-- Compter les billets existants
DECLARE @TotalBillets INT;
SELECT @TotalBillets = COUNT(*) FROM Billets;
PRINT 'Nombre total de billets existants: ' + CAST(@TotalBillets AS VARCHAR);

-- Vérifier les paiements avec billets émis (devrait être 0 au début)
DECLARE @PaiementsAvecBillets INT;
SELECT @PaiementsAvecBillets = COUNT(*) FROM Paiements WHERE IdBilletEmis IS NOT NULL;
PRINT 'Paiements avec billets émis (devrait être 0): ' + CAST(@PaiementsAvecBillets AS VARCHAR);

-- =====================================================
-- SECTION 8: CRÉATION D'UN LOG DE MIGRATION
-- =====================================================

PRINT '';
PRINT 'SECTION 8: CRÉATION D''UN LOG DE MIGRATION';

-- Créer une table de log si elle n'existe pas
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MigrationLogs')
BEGIN
    CREATE TABLE MigrationLogs (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        MigrationName VARCHAR(255) NOT NULL,
        Version VARCHAR(50) NOT NULL,
        DateExecution DATETIME NOT NULL DEFAULT GETDATE(),
        Statut VARCHAR(50) NOT NULL,
        Message TEXT NULL,
        Utilisateur VARCHAR(100) NULL,
        NombrePaiementsAvant INT NULL,
        NombreBilletsAvant INT NULL,
        NombrePaiementsApres INT NULL,
        NombreBilletsApres INT NULL
    );
    
    PRINT '✓ Table MigrationLogs créée';
END

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
    SUSER_NAME(),
    @TotalPaiements,
    @TotalBillets,
    (SELECT COUNT(*) FROM Paiements),
    (SELECT COUNT(*) FROM Billets)
);

PRINT '✓ Log de migration enregistré';

-- =====================================================
-- SECTION 9: INSTRUCTIONS POST-MIGRATION
-- =====================================================

PRINT '';
PRINT 'SECTION 9: INSTRUCTIONS POST-MIGRATION';
PRINT '';
PRINT '=====================================================';
PRINT 'MIGRATION TERMINÉE AVEC SUCCÈS!';
PRINT '=====================================================';
PRINT '';
PRINT 'Actions requises après la migration:';
PRINT '1. Redémarrer l''application CongoTravel API';
PRINT '2. Vérifier les logs de démarrage';
PRINT '3. Tester le workflow avec un paiement complet';
PRINT '4. Vérifier qu''un billet est émis automatiquement';
PRINT '5. Monitorer les performances pendant 24h';
PRINT '';
PRINT 'Scripts de validation disponibles:';
PRINT '- /Scripts/Production_Validate_Workflow.sql';
PRINT '- /Scripts/Production_Monitor_Workflow.sql';
PRINT '';
PRINT 'En cas de problème, voir le guide de rollback:';
PRINT '- /Scripts/Production_Rollback_Workflow.sql';
PRINT '';
PRINT 'Timestamp final: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '=====================================================';

-- =====================================================
-- FIN DU SCRIPT
-- =====================================================

-- Afficher un résumé final
SELECT 
    'Migration Workflow Paiement→Billet' as Nom,
    '1.3.0' as Version,
    GETDATE() as DateExecution,
    'SUCCÈS' as Statut,
    @TotalPaiements as TotalPaiements,
    @TotalBillets as TotalBillets,
    'Prêt pour le workflow automatique' as Message;

PRINT '';
PRINT '🎉 Le workflow automatique Paiement→Billet est maintenant prêt!';
