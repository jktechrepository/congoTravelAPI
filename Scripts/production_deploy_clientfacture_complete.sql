-- ============================================================================
-- SCRIPT SQL COMPLET DE PRODUCTION : Déploiement ClientFacture
-- Date : 2025-01-05
-- Description : Script unique pour appliquer tous les changements en production
--               - Création de la table ClientFactures
--               - Migration des données existantes
--               - Validation de la migration
-- 
-- ⚠️  IMPORTANT : 
-- 1. FAITES UN BACKUP COMPLET de votre base de données AVANT d'exécuter ce script
-- 2. Testez ce script sur une base de données de test d'abord
-- 3. Vérifiez que vous êtes connecté à la bonne base de données
-- 4. Exécutez pendant une période de faible activité si possible
-- 5. Ce script est IDEMPOTENT - peut être exécuté plusieurs fois sans erreur
--
-- DURÉE ESTIMÉE: 10-30 minutes selon le volume de données
-- ============================================================================

SET FOREIGN_KEY_CHECKS = 0;
SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET AUTOCOMMIT = 0;
SET time_zone = "+00:00";

-- ============================================================================
-- PARTIE 1: CRÉATION DE LA TABLE ClientFactures
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '📋 PARTIE 1: CRÉATION DE LA TABLE ClientFactures' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Vérifier si la table existe déjà
SELECT 
    CASE 
        WHEN COUNT(*) > 0 THEN '⚠️  Table ClientFactures existe déjà - Passage à la migration'
        ELSE '✅ Création de la table ClientFactures'
    END as Statut_Table
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'ClientFactures';

-- Créer la table si elle n'existe pas
CREATE TABLE IF NOT EXISTS `ClientFactures` (
    `IdClientFacture` INT AUTO_INCREMENT PRIMARY KEY,
    `IdFacture` INT NULL COMMENT 'NULL pour arriérés pré-existants',
    `IdClient` INT NOT NULL COMMENT 'Obligatoire',
    `Montant` DECIMAL(18,2) NULL COMMENT 'Montant total (déjà multiplié par nombreBatiment)',
    `nombreBatiment` INT NULL COMMENT 'Snapshot du nombre de bâtiments au moment de la facture',
    `MontantPaye` DECIMAL(18,2) NULL DEFAULT 0 COMMENT 'Montant déjà payé (pré-calculé)',
    `MontantDu` DECIMAL(18,2) NULL COMMENT 'Montant restant dû (pré-calculé)',
    `Mois` VARCHAR(20) NULL COMMENT 'Mois d''émission (format: "01", "02", ..., "12" ou "Janvier", etc.)',
    `Annees` INT NULL COMMENT 'Année d''émission (2000-2100)',
    `DateEmission` DATETIME(6) NULL COMMENT 'Date d''émission (plus fiable que Mois/Annees)',
    `EstArrierePreExistant` TINYINT(1) NOT NULL DEFAULT FALSE COMMENT 'Flag pour arriérés pré-existants',
    `Description` VARCHAR(500) NULL COMMENT 'Description/libellé pour les arriérés pré-existants',
    `Statut` TINYINT(1) NOT NULL DEFAULT TRUE COMMENT 'Statut actif/inactif (soft delete)',
    `DateCreation` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) COMMENT 'Date de création',
    `DateModification` DATETIME(6) NULL COMMENT 'Date de dernière modification',
    
    -- Contraintes de clés étrangères
    CONSTRAINT `FK_ClientFactures_Clients_IdClient` 
        FOREIGN KEY (`IdClient`) 
        REFERENCES `Clients` (`IdClient`) 
        ON DELETE RESTRICT 
        ON UPDATE CASCADE,
    
    CONSTRAINT `FK_ClientFactures_Factures_IdFacture` 
        FOREIGN KEY (`IdFacture`) 
        REFERENCES `Factures` (`IdFacture`) 
        ON DELETE SET NULL 
        ON UPDATE CASCADE,
    
    -- Index pour optimiser les requêtes
    INDEX `IX_ClientFacture_IdClient` (`IdClient`),
    INDEX `IX_ClientFacture_IdFacture` (`IdFacture`),
    INDEX `IX_ClientFacture_Client_Mois_Annees` (`IdClient`, `Mois`, `Annees`),
    INDEX `IX_ClientFacture_MontantDu` (`MontantDu`),
    INDEX `IX_ClientFacture_DateEmission` (`DateEmission`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Table de liaison Client-Facture pour gérer les arriérés et optimiser les calculs';

-- Ajouter les commentaires sur les colonnes
ALTER TABLE `ClientFactures` 
    MODIFY COLUMN `IdFacture` INT NULL COMMENT 'NULL pour arriérés pré-existants (avant informatisation)',
    MODIFY COLUMN `IdClient` INT NOT NULL COMMENT 'Obligatoire - Client concerné',
    MODIFY COLUMN `Montant` DECIMAL(18,2) NULL COMMENT 'Montant total pour ce client (facture.Montant × nombreBatiment)',
    MODIFY COLUMN `nombreBatiment` INT NULL COMMENT 'Snapshot du nombreBatiment au moment de la facture',
    MODIFY COLUMN `MontantPaye` DECIMAL(18,2) NULL DEFAULT 0 COMMENT 'Somme des paiements validés (pré-calculé)',
    MODIFY COLUMN `MontantDu` DECIMAL(18,2) NULL COMMENT 'Montant restant dû = Montant - MontantPaye',
    MODIFY COLUMN `Mois` VARCHAR(20) NULL COMMENT 'Mois d''émission (format libre: "01", "Janvier", etc.)',
    MODIFY COLUMN `Annees` INT NULL COMMENT 'Année d''émission (2000-2100)',
    MODIFY COLUMN `DateEmission` DATETIME(6) NULL COMMENT 'Date d''émission (plus fiable pour tri et filtrage)',
    MODIFY COLUMN `EstArrierePreExistant` TINYINT(1) NOT NULL DEFAULT FALSE COMMENT 'TRUE si arriéré avant informatisation',
    MODIFY COLUMN `Description` VARCHAR(500) NULL COMMENT 'Description/libellé pour les arriérés pré-existants',
    MODIFY COLUMN `Statut` TINYINT(1) NOT NULL DEFAULT TRUE COMMENT 'TRUE = actif, FALSE = inactif (soft delete)';

-- Vérifier la création
SELECT 
    '✅ Table ClientFactures créée avec succès' as Statut,
    TABLE_NAME,
    TABLE_ROWS,
    ENGINE,
    TABLE_COLLATION
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'ClientFactures';

-- ============================================================================
-- PARTIE 2: VÉRIFICATIONS PRÉALABLES À LA MIGRATION
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '🔍 PARTIE 2: VÉRIFICATIONS PRÉALABLES' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Compter les factures existantes
SELECT 
    '📊 Factures existantes:' as Info;
SELECT 
    COUNT(*) as Nombre_Factures_Actives,
    COUNT(DISTINCT IdUsage) as Nombre_Usages_Differents,
    SUM(Montant) as Montant_Total_Factures
FROM Factures
WHERE Statut = 1;

-- Compter les ClientFactures déjà créées
SELECT 
    '📊 ClientFactures déjà créées:' as Info;
SELECT 
    COUNT(*) as Nombre_ClientFactures_Existantes,
    COUNT(DISTINCT IdFacture) as Nombre_Factures_Deja_Migrees,
    COUNT(DISTINCT IdClient) as Nombre_Clients_Concernes
FROM ClientFactures
WHERE IdFacture IS NOT NULL
  AND Statut = 1;

-- Compter les paiements existants
SELECT 
    '📊 Paiements existants:' as Info;
SELECT 
    COUNT(*) as Nombre_Paiements_Valides,
    SUM(MontantPaye) as Montant_Total_Paye
FROM Paiements
WHERE Statut IN ('Validé', 'true', 'True', 'TRUE');

-- ============================================================================
-- PARTIE 3: MIGRATION DES DONNÉES EXISTANTES
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '🚀 PARTIE 3: MIGRATION DES DONNÉES' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

START TRANSACTION;

-- Pour chaque facture, créer les ClientFacture pour tous les clients ayant l'usage
INSERT INTO ClientFactures (
    IdFacture,
    IdClient,
    Montant,
    nombreBatiment,
    MontantPaye,
    MontantDu,
    Mois,
    Annees,
    DateEmission,
    EstArrierePreExistant,
    Statut,
    DateCreation
)
SELECT DISTINCT
    f.IdFacture,
    cu.IdClient,
    -- Calculer Montant = facture.Montant × nombreBatiment
    (f.Montant * COALESCE(cu.nombreBatiment, 1)) as Montant,
    cu.nombreBatiment as nombreBatiment,
    -- Calculer MontantPaye depuis la table Paiements
    COALESCE(
        (SELECT SUM(p.MontantPaye)
         FROM Paiements p
         WHERE p.IdFacture = f.IdFacture
           AND p.IdClient = cu.IdClient
           AND p.Statut IN ('Validé', 'true', 'True', 'TRUE')
        ),
        0
    ) as MontantPaye,
    -- Calculer MontantDu = Montant - MontantPaye
    (f.Montant * COALESCE(cu.nombreBatiment, 1)) - 
    COALESCE(
        (SELECT SUM(p.MontantPaye)
         FROM Paiements p
         WHERE p.IdFacture = f.IdFacture
           AND p.IdClient = cu.IdClient
           AND p.Statut IN ('Validé', 'true', 'True', 'TRUE')
        ),
        0
    ) as MontantDu,
    -- Mois depuis facture.MoisEmission (format "01", "02", etc.)
    LPAD(f.MoisEmission, 2, '0') as Mois,
    f.AnneesEmission as Annees,
    f.DateEmission as DateEmission,
    FALSE as EstArrierePreExistant,
    TRUE as Statut,
    NOW(6) as DateCreation
FROM Factures f
INNER JOIN ClientUsages cu ON cu.IdUsage = f.IdUsage
INNER JOIN Clients c ON c.IdClient = cu.IdClient
WHERE f.Statut = 1
  AND cu.Statut = 1
  AND c.Statut = 1
  -- Éviter les doublons : ne créer que si la ClientFacture n'existe pas déjà
  AND NOT EXISTS (
      SELECT 1
      FROM ClientFactures cf
      WHERE cf.IdFacture = f.IdFacture
        AND cf.IdClient = cu.IdClient
        AND cf.Statut = 1
  )
ORDER BY f.IdFacture, cu.IdClient;

-- Afficher le nombre de ClientFactures créées
SELECT 
    '✅ ClientFactures créées dans cette exécution:' as Info;
SELECT 
    ROW_COUNT() as Nombre_ClientFactures_Creees;

-- ============================================================================
-- PARTIE 4: VALIDATION DE LA MIGRATION
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '✅ PARTIE 4: VALIDATION DE LA MIGRATION' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Statistiques finales
SELECT 
    '📊 Statistiques finales:' as Info;
SELECT 
    COUNT(*) as Total_ClientFactures,
    COUNT(DISTINCT IdFacture) as Nombre_Factures_Avec_ClientFactures,
    COUNT(DISTINCT IdClient) as Nombre_Clients_Avec_ClientFactures,
    SUM(CASE WHEN MontantDu > 0 THEN 1 ELSE 0 END) as Nombre_ClientFactures_Avec_Arrieres,
    SUM(MontantDu) as Total_Arrieres,
    SUM(MontantPaye) as Total_Montant_Paye
FROM ClientFactures
WHERE IdFacture IS NOT NULL
  AND Statut = 1;

-- Vérifier la cohérence : comparer MontantPaye dans ClientFacture vs SUM(Paiements)
SELECT 
    '🔍 Vérification de cohérence MontantPaye:' as Info;
SELECT 
    COUNT(*) as Total_ClientFactures_Verifiees,
    COUNT(CASE 
        WHEN ABS(cf.MontantPaye - COALESCE(
            (SELECT SUM(p.MontantPaye)
             FROM Paiements p
             WHERE p.IdFacture = cf.IdFacture
               AND p.IdClient = cf.IdClient
               AND p.Statut IN ('Validé', 'true', 'True', 'TRUE')
            ),
            0
        )) <= 0.01 THEN 1
    END) as ClientFactures_Coherentes,
    COUNT(CASE 
        WHEN ABS(cf.MontantPaye - COALESCE(
            (SELECT SUM(p.MontantPaye)
             FROM Paiements p
             WHERE p.IdFacture = cf.IdFacture
               AND p.IdClient = cf.IdClient
               AND p.Statut IN ('Validé', 'true', 'True', 'TRUE')
            ),
            0
        )) > 0.01 THEN 1
    END) as ClientFactures_Incoherentes
FROM ClientFactures cf
WHERE cf.IdFacture IS NOT NULL
  AND cf.Statut = 1;

-- Afficher les incohérences détectées (limité à 10)
SELECT 
    '⚠️  Incohérences détectées (top 10):' as Info;
SELECT 
    cf.IdClientFacture,
    cf.IdFacture,
    cf.IdClient,
    cf.MontantPaye as MontantPaye_ClientFacture,
    COALESCE(
        (SELECT SUM(p.MontantPaye)
         FROM Paiements p
         WHERE p.IdFacture = cf.IdFacture
           AND p.IdClient = cf.IdClient
           AND p.Statut IN ('Validé', 'true', 'True', 'TRUE')
        ),
        0
    ) as MontantPaye_Calcule,
    ABS(cf.MontantPaye - COALESCE(
        (SELECT SUM(p.MontantPaye)
         FROM Paiements p
         WHERE p.IdFacture = cf.IdFacture
           AND p.IdClient = cf.IdClient
           AND p.Statut IN ('Validé', 'true', 'True', 'TRUE')
        ),
        0
    )) as Difference
FROM ClientFactures cf
WHERE cf.IdFacture IS NOT NULL
  AND cf.Statut = 1
  AND ABS(cf.MontantPaye - COALESCE(
        (SELECT SUM(p.MontantPaye)
         FROM Paiements p
         WHERE p.IdFacture = cf.IdFacture
           AND p.IdClient = cf.IdClient
           AND p.Statut IN ('Validé', 'true', 'True', 'TRUE')
        ),
        0
    )) > 0.01
ORDER BY Difference DESC
LIMIT 10;

-- Vérifier la cohérence : MontantDu = Montant - MontantPaye
SELECT 
    '🔍 Vérification de cohérence MontantDu:' as Info;
SELECT 
    COUNT(*) as Total_ClientFactures_Verifiees,
    COUNT(CASE 
        WHEN ABS(cf.MontantDu - (cf.Montant - cf.MontantPaye)) <= 0.01 THEN 1
    END) as ClientFactures_Coherentes,
    COUNT(CASE 
        WHEN ABS(cf.MontantDu - (cf.Montant - cf.MontantPaye)) > 0.01 THEN 1
    END) as ClientFactures_Incoherentes
FROM ClientFactures cf
WHERE cf.Montant IS NOT NULL
  AND cf.MontantPaye IS NOT NULL
  AND cf.MontantDu IS NOT NULL
  AND cf.Statut = 1;

-- Vérifier que toutes les factures actives ont au moins une ClientFacture
SELECT 
    '📊 Factures actives sans ClientFacture:' as Info;
SELECT 
    COUNT(*) as Nombre_Factures_Sans_ClientFacture
FROM Factures f
WHERE f.Statut = 1
  AND NOT EXISTS (
      SELECT 1
      FROM ClientFactures cf
      WHERE cf.IdFacture = f.IdFacture
        AND cf.Statut = 1
  );

-- Afficher les factures sans ClientFacture (limité à 10)
SELECT 
    '📋 Liste des factures sans ClientFacture (top 10):' as Info;
SELECT 
    f.IdFacture,
    f.NumeroFacture,
    f.MoisEmission,
    f.AnneesEmission,
    f.IdUsage,
    f.Montant,
    (SELECT COUNT(*) 
     FROM ClientUsages cu 
     WHERE cu.IdUsage = f.IdUsage 
       AND cu.Statut = 1) as Nombre_Clients_Avec_Usage
FROM Factures f
WHERE f.Statut = 1
  AND NOT EXISTS (
      SELECT 1
      FROM ClientFactures cf
      WHERE cf.IdFacture = f.IdFacture
        AND cf.Statut = 1
  )
LIMIT 10;

-- Vérifier les doublons potentiels
SELECT 
    '🔍 Vérification des doublons:' as Info;
SELECT 
    COUNT(*) as Nombre_Doublons_Potentiels
FROM (
    SELECT 
        IdFacture,
        IdClient,
        COUNT(*) as Nombre
    FROM ClientFactures
    WHERE IdFacture IS NOT NULL
      AND Statut = 1
    GROUP BY IdFacture, IdClient
    HAVING COUNT(*) > 1
) as Doublons;

-- ============================================================================
-- PARTIE 5: COMMIT ET FINALISATION
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '📋 RÉSUMÉ FINAL' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

SELECT 
    '✅ Migration terminée' as Statut,
    (SELECT COUNT(*) FROM ClientFactures WHERE Statut = 1) as Total_ClientFactures,
    (SELECT COUNT(DISTINCT IdFacture) FROM ClientFactures WHERE IdFacture IS NOT NULL AND Statut = 1) as Factures_Migrees,
    (SELECT COUNT(*) FROM Factures WHERE Statut = 1) as Total_Factures_Actives,
    CASE 
        WHEN (SELECT COUNT(*) FROM Factures WHERE Statut = 1) = 
             (SELECT COUNT(DISTINCT IdFacture) FROM ClientFactures WHERE IdFacture IS NOT NULL AND Statut = 1)
        THEN '✅ Toutes les factures ont été migrées'
        ELSE CONCAT('⚠️  ', 
                   (SELECT COUNT(*) FROM Factures WHERE Statut = 1) - 
                   (SELECT COUNT(DISTINCT IdFacture) FROM ClientFactures WHERE IdFacture IS NOT NULL AND Statut = 1),
                   ' facture(s) non migrée(s) - Peut être normal si aucun client n''a l''usage')
    END as Statut_Migration;

-- Si tout est OK, valider la transaction
-- ⚠️  IMPORTANT : Vérifiez les résultats ci-dessus avant de décommenter la ligne suivante
COMMIT;

-- En cas de problème, décommenter la ligne suivante pour annuler
-- ROLLBACK;

SET FOREIGN_KEY_CHECKS = 1;

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '✅ DÉPLOIEMENT TERMINÉ' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '📝 Prochaines étapes:' as Info;
SELECT 
    '1. Vérifiez les résultats ci-dessus' as Etape_1,
    '2. Testez les endpoints API /api/Client/{id}/arrieres' as Etape_2,
    '3. Surveillez les performances' as Etape_3,
    '4. Créez des arriérés pré-existants si nécessaire via /api/ClientFacture/pre-existant' as Etape_4;

-- ============================================================================
-- FIN DU SCRIPT
-- ============================================================================
