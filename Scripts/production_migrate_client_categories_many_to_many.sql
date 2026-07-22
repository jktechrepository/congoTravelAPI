-- ============================================================================
-- SCRIPT DE MIGRATION PRODUCTION : Relation Many-to-Many Client-CategorieClient
-- ============================================================================
-- 
-- DESCRIPTION:
-- Ce script crée la table de jointure ClientCategorieClients pour permettre
-- à un client d'appartenir à plusieurs catégories, tout en conservant la
-- compatibilité avec le champ IdCategorieClient existant.
--
-- IMPORTANT - AVANT D'EXÉCUTER:
-- 1. ⚠️  FAITES UN BACKUP COMPLET de votre base de données
-- 2. Testez ce script sur une base de données de test d'abord
-- 3. Vérifiez que vous êtes connecté à la bonne base de données
-- 4. Exécutez pendant une période de faible activité si possible
--
-- MODIFICATIONS:
-- - Crée la table ClientCategorieClients (table de jointure)
-- - Migre les catégories principales existantes vers la nouvelle table
-- - Conserve IdCategorieClient pour compatibilité ascendante
--
-- DURÉE ESTIMÉE: 1-5 minutes selon le volume de données
-- ============================================================================

SET FOREIGN_KEY_CHECKS = 0;
SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET AUTOCOMMIT = 0;
START TRANSACTION;
SET time_zone = "+00:00";

-- ============================================================================
-- PARTIE 1: VÉRIFICATIONS PRÉALABLES
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '🔍 VÉRIFICATIONS PRÉALABLES' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Vérifier que les tables nécessaires existent
SELECT 
    CASE 
        WHEN COUNT(*) > 0 THEN CONCAT('✅ Table Clients existe (', COUNT(*), ' ligne(s))')
        ELSE '❌ ERREUR: Table Clients n''existe pas'
    END as Statut_Clients
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'Clients';

SELECT 
    CASE 
        WHEN COUNT(*) > 0 THEN CONCAT('✅ Table CategorieClients existe (', COUNT(*), ' ligne(s))')
        ELSE '❌ ERREUR: Table CategorieClients n''existe pas'
    END as Statut_CategorieClients
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'CategorieClients';

-- Vérifier si la table ClientCategorieClients existe déjà
SELECT 
    CASE 
        WHEN COUNT(*) > 0 THEN '⚠️  ATTENTION: Table ClientCategorieClients existe déjà. Le script va migrer uniquement les nouvelles données.'
        ELSE '✅ Table ClientCategorieClients n''existe pas, création nécessaire'
    END as Statut_Table_Jointure
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'ClientCategorieClients';

-- Statistiques avant migration
SELECT 
    '📊 STATISTIQUES AVANT MIGRATION' as Info;
SELECT 
    COUNT(*) as Total_Clients,
    COUNT(IdCategorieClient) as Clients_Avec_Categorie,
    COUNT(*) - COUNT(IdCategorieClient) as Clients_Sans_Categorie
FROM Clients;

-- ============================================================================
-- PARTIE 2: CRÉATION DE LA TABLE ClientCategorieClients
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '🔄 CRÉATION DE LA TABLE ClientCategorieClients' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Vérifier si la table existe déjà
SET @table_exists := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = DATABASE() 
      AND TABLE_NAME = 'ClientCategorieClients'
);

-- Créer la table si elle n'existe pas
SET @sql_create := IF(@table_exists = 0,
    'CREATE TABLE `ClientCategorieClients` (
        `IdClient` int NOT NULL,
        `IdCategorie` int NOT NULL,
        `DateAttribution` datetime(6) NOT NULL,
        PRIMARY KEY (`IdClient`, `IdCategorie`),
        KEY `IX_ClientCategorieClient_IdClient` (`IdClient`),
        KEY `IX_ClientCategorieClient_IdCategorie` (`IdCategorie`),
        CONSTRAINT `FK_ClientCategorieClients_CategorieClients_IdCategorie` 
            FOREIGN KEY (`IdCategorie`) 
            REFERENCES `CategorieClients` (`IdCategorie`) 
            ON DELETE CASCADE,
        CONSTRAINT `FK_ClientCategorieClients_Clients_IdClient` 
            FOREIGN KEY (`IdClient`) 
            REFERENCES `Clients` (`IdClient`) 
            ON DELETE CASCADE
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;',
    'SELECT "Table ClientCategorieClients existe déjà, création ignorée." AS message;'
);

PREPARE stmt_create FROM @sql_create;
EXECUTE stmt_create;
DEALLOCATE PREPARE stmt_create;

SELECT 
    CASE 
        WHEN @table_exists = 0 THEN '✅ Table ClientCategorieClients créée avec succès'
        ELSE 'ℹ️  Table ClientCategorieClients existe déjà'
    END as Resultat_Creation;

-- ============================================================================
-- PARTIE 3: MIGRATION DES DONNÉES EXISTANTES
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '🔄 MIGRATION DES DONNÉES EXISTANTES' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Compter les relations à migrer
SELECT 
    COUNT(*) as Relations_A_Migrer
FROM Clients c
WHERE c.IdCategorieClient IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 
      FROM ClientCategorieClients ccc 
      WHERE ccc.IdClient = c.IdClient 
        AND ccc.IdCategorie = c.IdCategorieClient
  );

-- Migrer les catégories principales vers la table de jointure
-- (uniquement les relations qui n'existent pas déjà)
INSERT IGNORE INTO `ClientCategorieClients` (`IdClient`, `IdCategorie`, `DateAttribution`)
SELECT 
    c.IdClient,
    c.IdCategorieClient,
    COALESCE(c.DateCreation, NOW())
FROM Clients c
WHERE c.IdCategorieClient IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 
      FROM ClientCategorieClients ccc 
      WHERE ccc.IdClient = c.IdClient 
        AND ccc.IdCategorie = c.IdCategorieClient
  );

-- Afficher le résultat de la migration
SELECT 
    CASE 
        WHEN ROW_COUNT() > 0 THEN CONCAT('✅ ', ROW_COUNT(), ' relation(s) créée(s)')
        ELSE 'ℹ️  Toutes les catégories principales ont déjà été migrées'
    END as Resultat_Migration;

-- ============================================================================
-- PARTIE 4: VÉRIFICATIONS POST-MIGRATION
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '✅ VÉRIFICATIONS POST-MIGRATION' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Vérifier l'intégrité des données
SELECT 
    '📊 Vérification de l''intégrité des données:' as Info;

-- Compter les relations totales
SELECT 
    COUNT(*) as Total_Relations
FROM ClientCategorieClients;

-- Vérifier que tous les clients avec catégorie principale ont été migrés
SELECT 
    COUNT(DISTINCT c.IdClient) as Clients_Migres,
    COUNT(DISTINCT c.IdClient) - (
        SELECT COUNT(DISTINCT c2.IdClient)
        FROM Clients c2
        WHERE c2.IdCategorieClient IS NOT NULL
    ) as Difference
FROM Clients c
INNER JOIN ClientCategorieClients ccc ON c.IdClient = ccc.IdClient
WHERE c.IdCategorieClient IS NOT NULL
  AND ccc.IdCategorie = c.IdCategorieClient;

-- Vérifier les références orphelines (ne devrait pas y en avoir)
SELECT 
    COUNT(*) as Relations_Orphelines_Categorie
      FROM ClientCategorieClients ccc 
LEFT JOIN CategorieClients cc ON ccc.IdCategorie = cc.IdCategorie
WHERE cc.IdCategorie IS NULL;

SELECT 
    COUNT(*) as Relations_Orphelines_Client
FROM ClientCategorieClients ccc
LEFT JOIN Clients c ON ccc.IdClient = c.IdClient
WHERE c.IdClient IS NULL;

-- ============================================================================
-- PARTIE 5: STATISTIQUES FINALES
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '📊 STATISTIQUES FINALES' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Clients avec une seule catégorie (catégorie principale uniquement)
SELECT 
    'Clients avec une seule catégorie' as Type,
    COUNT(DISTINCT c.IdClient) as Nombre
FROM Clients c
INNER JOIN ClientCategorieClients ccc ON c.IdClient = ccc.IdClient
WHERE c.IdCategorieClient IS NOT NULL
GROUP BY c.IdClient
HAVING COUNT(ccc.IdCategorie) = 1;

-- Clients avec plusieurs catégories (après ajout manuel)
SELECT 
    'Clients avec plusieurs catégories' as Type,
    COUNT(DISTINCT ccc.IdClient) as Nombre
FROM ClientCategorieClients ccc
GROUP BY ccc.IdClient
HAVING COUNT(ccc.IdCategorie) > 1;

-- Distribution des catégories
SELECT 
    'Distribution des catégories' as Info;
SELECT 
    cc.NomCategorie,
    COUNT(DISTINCT ccc.IdClient) as Nombre_Clients
FROM ClientCategorieClients ccc
INNER JOIN CategorieClients cc ON ccc.IdCategorie = cc.IdCategorie
GROUP BY cc.IdCategorie, cc.NomCategorie
ORDER BY Nombre_Clients DESC
LIMIT 10;

-- ============================================================================
-- VALIDATION ET COMMIT
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '✅ VALIDATION' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Vérifier qu'il n'y a pas d'erreurs critiques
SELECT 
    COUNT(*) as Relations_Orphelines_Total
FROM ClientCategorieClients ccc
LEFT JOIN Clients c ON ccc.IdClient = c.IdClient
LEFT JOIN CategorieClients cc ON ccc.IdCategorie = cc.IdCategorie
WHERE c.IdClient IS NULL OR cc.IdCategorie IS NULL;

SELECT 
    '✅ Si le nombre ci-dessus est 0, la migration est réussie!' as Statut_Final;
SELECT 
    '⚠️  Si le nombre est > 0, il y a des relations orphelines à corriger' as Avertissement;

SET FOREIGN_KEY_CHECKS = 1;

-- ============================================================================
-- INSTRUCTIONS POST-MIGRATION
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '📝 INSTRUCTIONS POST-MIGRATION' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

SELECT 
    '1. ✅ Vérifiez que toutes les statistiques ci-dessus sont cohérentes' as Instruction;
SELECT 
    '2. ✅ Testez l''application pour vous assurer que tout fonctionne' as Instruction;
SELECT 
    '3. ✅ Les clients conservent leur IdCategorieClient pour compatibilité' as Instruction;
SELECT 
    '4. ✅ Vous pouvez maintenant ajouter plusieurs catégories aux clients via l''API' as Instruction;
SELECT 
    '5. ✅ Les calculs d''arriérés utilisent maintenant toutes les catégories' as Instruction;

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '✅ MIGRATION TERMINÉE' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- ============================================================================
-- ACTION REQUISE: VALIDATION MANUELLE
-- ============================================================================

SELECT 
    '⚠️  ACTION REQUISE:' as Avertissement;
SELECT 
    '1. Vérifiez tous les résultats ci-dessus' as Etape;
SELECT 
    '2. Si tout est OK, exécutez: COMMIT;' as Etape;
SELECT 
    '3. Si vous détectez des erreurs, exécutez: ROLLBACK;' as Etape;
SELECT 
    '' as Separateur;

-- La transaction est toujours ouverte - vous devez la valider manuellement
-- Exécutez maintenant: COMMIT; ou ROLLBACK;
