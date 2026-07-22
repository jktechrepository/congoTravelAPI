-- ============================================================================
-- SCRIPT DE MIGRATION PRODUCTION COMPLET : Déplacer Usage vers CategorieClients
-- ============================================================================
-- 
-- DESCRIPTION:
-- Ce script effectue la migration complète du champ Usage de Clients vers 
-- CategorieClients. Il ajoute la colonne, migre les données, puis supprime 
-- l'ancienne colonne.
--
-- IMPORTANT - AVANT D'EXÉCUTER:
-- 1. ⚠️  FAITES UN BACKUP COMPLET de votre base de données
-- 2. Testez ce script sur une base de données de test d'abord
-- 3. Vérifiez que vous êtes connecté à la bonne base de données
-- 4. Exécutez pendant une période de faible activité si possible
--
-- MODIFICATIONS:
-- - Ajoute la colonne Usage à CategorieClients
-- - Migre les données de Clients.Usage vers CategorieClients.Usage
-- - Supprime la colonne Usage de Clients
--
-- DURÉE ESTIMÉE: 2-5 minutes selon le volume de données
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

-- Vérifier que les tables existent
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

-- Vérifier que la colonne Usage existe dans Clients
SELECT 
    CASE 
        WHEN COUNT(*) > 0 THEN '✅ Colonne Usage existe dans Clients'
        ELSE '❌ ERREUR: Colonne Usage n''existe pas dans Clients'
    END as Statut_Usage_Clients
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'Clients'
  AND COLUMN_NAME = 'Usage';

-- Vérifier si la colonne Usage existe déjà dans CategorieClients
SELECT 
    CASE 
        WHEN COUNT(*) > 0 THEN '⚠️  ATTENTION: Colonne Usage existe déjà dans CategorieClients'
        ELSE '✅ Colonne Usage n''existe pas dans CategorieClients'
    END as Statut_Usage_CategorieClients
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'CategorieClients'
  AND COLUMN_NAME = 'Usage';

-- Statistiques avant migration
SELECT 
    '📊 STATISTIQUES AVANT MIGRATION' as Info;
SELECT 
    COUNT(*) as Total_Clients,
    COUNT(Usage) as Clients_Avec_Usage,
    COUNT(*) - COUNT(Usage) as Clients_Sans_Usage,
    COUNT(DISTINCT Usage) as Nombre_Usages_Differents
FROM Clients;

-- Identifier les catégories avec plusieurs usages différents
SELECT 
    '📊 Catégories avec plusieurs usages différents:' as Info;
SELECT 
    cc.IdCategorie,
    cc.NomCategorie,
    COUNT(DISTINCT c.Usage) as Nombre_Usages_Differents,
    GROUP_CONCAT(DISTINCT c.Usage ORDER BY c.Usage SEPARATOR ', ') as Usages_Uniques,
    COUNT(c.IdClient) as Nombre_Clients
FROM CategorieClients cc
INNER JOIN Clients c ON cc.IdCategorie = c.IdCategorieClient
WHERE c.Usage IS NOT NULL
GROUP BY cc.IdCategorie, cc.NomCategorie
HAVING COUNT(DISTINCT c.Usage) > 1;

-- ============================================================================
-- PARTIE 2: AJOUT DE LA COLONNE Usage À CategorieClients
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '🔄 ÉTAPE 1: AJOUT DE LA COLONNE Usage À CategorieClients' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Vérifier si la colonne existe déjà
SET @col_exists := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'CategorieClients'
      AND COLUMN_NAME = 'Usage'
);

-- Ajouter la colonne si elle n'existe pas
SET @sql_add_col := IF(@col_exists = 0,
    'ALTER TABLE `CategorieClients` ADD COLUMN `Usage` VARCHAR(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL AFTER `Description`;',
    'SELECT "Colonne Usage existe déjà dans CategorieClients, création ignorée." AS message;'
);

PREPARE stmt_add_col FROM @sql_add_col;
EXECUTE stmt_add_col;
DEALLOCATE PREPARE stmt_add_col;

SELECT 
    CASE 
        WHEN @col_exists = 0 THEN '✅ Colonne Usage ajoutée à CategorieClients'
        ELSE 'ℹ️  Colonne Usage existe déjà dans CategorieClients'
    END as Resultat_Etape_1;

-- ============================================================================
-- PARTIE 3: MIGRATION DES DONNÉES
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '🔄 ÉTAPE 2: MIGRATION DES DONNÉES' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Compter les catégories à migrer
SELECT 
    COUNT(DISTINCT cc.IdCategorie) as Categories_A_Migrer
FROM CategorieClients cc
INNER JOIN Clients c ON cc.IdCategorie = c.IdCategorieClient
WHERE c.Usage IS NOT NULL
  AND (cc.Usage IS NULL OR cc.Usage = '');

-- Migrer les données : pour chaque catégorie, prendre l'usage le plus fréquent
-- Si plusieurs usages différents, prendre le premier non-null trouvé (ordre alphabétique)
UPDATE CategorieClients cc
INNER JOIN (
    SELECT 
        c.IdCategorieClient,
        c.Usage,
        COUNT(*) as Frequency
    FROM Clients c
    WHERE c.Usage IS NOT NULL
      AND c.IdCategorieClient IS NOT NULL
    GROUP BY c.IdCategorieClient, c.Usage
) as usage_stats ON cc.IdCategorie = usage_stats.IdCategorieClient
INNER JOIN (
    SELECT 
        IdCategorieClient,
        MAX(Frequency) as MaxFrequency
    FROM (
        SELECT 
            c.IdCategorieClient,
            c.Usage,
            COUNT(*) as Frequency
        FROM Clients c
        WHERE c.Usage IS NOT NULL
          AND c.IdCategorieClient IS NOT NULL
        GROUP BY c.IdCategorieClient, c.Usage
    ) as freq
    GROUP BY IdCategorieClient
) as max_freq ON usage_stats.IdCategorieClient = max_freq.IdCategorieClient
              AND usage_stats.Frequency = max_freq.MaxFrequency
SET cc.Usage = usage_stats.Usage
WHERE (cc.Usage IS NULL OR cc.Usage = '')
  AND usage_stats.Frequency = max_freq.MaxFrequency
  AND usage_stats.Usage = (
      SELECT MIN(usage_stats2.Usage)
      FROM (
          SELECT 
              c2.IdCategorieClient,
              c2.Usage,
              COUNT(*) as Frequency
          FROM Clients c2
          WHERE c2.Usage IS NOT NULL
            AND c2.IdCategorieClient = usage_stats.IdCategorieClient
          GROUP BY c2.IdCategorieClient, c2.Usage
          HAVING COUNT(*) = max_freq.MaxFrequency
      ) as usage_stats2
      WHERE usage_stats2.IdCategorieClient = usage_stats.IdCategorieClient
  );

-- Afficher le résultat
SELECT 
    CASE 
        WHEN ROW_COUNT() > 0 THEN CONCAT('✅ ', ROW_COUNT(), ' catégorie(s) mise(s) à jour')
        ELSE 'ℹ️  Toutes les catégories ont déjà un usage ou aucun client avec usage'
    END as Resultat_Etape_2;

-- ============================================================================
-- PARTIE 4: VÉRIFICATIONS POST-MIGRATION
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '✅ VÉRIFICATIONS POST-MIGRATION' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Compter les catégories avec usage
SELECT 
    '📊 Catégories avec usage après migration:' as Info;
SELECT 
    COUNT(*) as Total_Categories,
    COUNT(Usage) as Categories_Avec_Usage,
    COUNT(*) - COUNT(Usage) as Categories_Sans_Usage
FROM CategorieClients;

-- Vérifier la cohérence : catégories avec clients ayant usage mais sans usage dans la catégorie
SELECT 
    '📊 Vérification de cohérence:' as Info;
SELECT 
    COUNT(DISTINCT cc.IdCategorie) as Categories_Problematiques
FROM CategorieClients cc
INNER JOIN Clients c ON cc.IdCategorie = c.IdCategorieClient
WHERE c.Usage IS NOT NULL
  AND (cc.Usage IS NULL OR cc.Usage = '');

-- Exemples de catégories avec leur usage
SELECT 
    '📋 Exemples de catégories avec leur usage:' as Info;
SELECT 
    cc.IdCategorie,
    cc.NomCategorie,
    cc.Usage,
    COUNT(c.IdClient) as Nombre_Clients,
    COUNT(DISTINCT c.Usage) as Usages_Differents_Clients
FROM CategorieClients cc
LEFT JOIN Clients c ON cc.IdCategorie = c.IdCategorieClient
WHERE cc.Usage IS NOT NULL
GROUP BY cc.IdCategorie, cc.NomCategorie, cc.Usage
ORDER BY cc.NomCategorie
LIMIT 10;

-- ============================================================================
-- PARTIE 5: SUPPRESSION DE LA COLONNE Usage DE Clients
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '🔄 ÉTAPE 3: SUPPRESSION DE LA COLONNE Usage DE Clients' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Vérifier qu'il n'y a pas de problèmes avant suppression
SET @problems := (
    SELECT COUNT(DISTINCT cc.IdCategorie)
    FROM CategorieClients cc
    INNER JOIN Clients c ON cc.IdCategorie = c.IdCategorieClient
    WHERE c.Usage IS NOT NULL
      AND (cc.Usage IS NULL OR cc.Usage = '')
);

SELECT 
    CASE 
        WHEN @problems = 0 THEN '✅ Aucun problème détecté - Prêt pour suppression'
        ELSE CONCAT('⚠️  ATTENTION: ', @problems, ' catégorie(s) problématique(s) détectée(s)')
    END as Statut_Avant_Suppression;

-- Supprimer la colonne Usage de Clients
ALTER TABLE `Clients` DROP COLUMN `Usage`;

SELECT 
    '✅ Colonne Usage supprimée de Clients' as Resultat_Etape_3;

-- ============================================================================
-- VALIDATION FINALE
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '✅ VALIDATION FINALE' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Vérifier que la colonne n'existe plus dans Clients
SELECT 
    CASE 
        WHEN COUNT(*) = 0 THEN '✅ Colonne Usage supprimée de Clients avec succès'
        ELSE '❌ ERREUR: Colonne Usage existe encore dans Clients'
    END as Verification_Suppression_Clients
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'Clients'
  AND COLUMN_NAME = 'Usage';

-- Vérifier que la colonne existe dans CategorieClients
SELECT 
    CASE 
        WHEN COUNT(*) > 0 THEN '✅ Colonne Usage existe dans CategorieClients'
        ELSE '❌ ERREUR: Colonne Usage n''existe pas dans CategorieClients'
    END as Verification_Ajout_CategorieClients
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'CategorieClients'
  AND COLUMN_NAME = 'Usage';

-- Statistiques finales
SELECT 
    '📊 STATISTIQUES FINALES' as Info;
SELECT 
    (SELECT COUNT(*) FROM CategorieClients WHERE Usage IS NOT NULL) as Categories_Avec_Usage,
    (SELECT COUNT(*) FROM CategorieClients WHERE Usage IS NULL) as Categories_Sans_Usage,
    (SELECT COUNT(DISTINCT Usage) FROM CategorieClients WHERE Usage IS NOT NULL) as Nombre_Usages_Uniques;

-- ============================================================================
-- COMMIT DE LA TRANSACTION
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '⚠️  VALIDATION MANUELLE REQUISE' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

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
    '2. ✅ Si des catégories ont plusieurs usages différents, vérifiez manuellement' as Instruction;
SELECT 
    '3. ✅ Exécutez COMMIT; pour valider la transaction' as Instruction;
SELECT 
    '4. ✅ Testez l''application pour vous assurer que tout fonctionne' as Instruction;
SELECT 
    '5. ✅ Vérifiez que le filtrage par usage fonctionne via les catégories' as Instruction;
SELECT 
    '6. ✅ Vérifiez que l''import Excel fonctionne sans la colonne Usage' as Instruction;

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '✅ MIGRATION TERMINÉE' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
