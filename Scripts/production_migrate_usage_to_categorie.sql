-- ============================================================================
-- SCRIPT DE MIGRATION PRODUCTION : Déplacer Usage de Clients vers CategorieClients
-- ============================================================================
-- 
-- DESCRIPTION:
-- Ce script migre les données de Usage de la table Clients vers CategorieClients.
-- Pour chaque catégorie, on prend l'usage le plus fréquent parmi ses clients.
-- Si une catégorie a plusieurs usages différents, on prend le premier non-null.
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
-- DURÉE ESTIMÉE: 1-3 minutes selon le volume de données
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
        WHEN COUNT(*) > 0 THEN '⚠️  ATTENTION: Colonne Usage existe déjà dans CategorieClients. Le script va migrer uniquement les nouvelles données.'
        ELSE '✅ Colonne Usage n''existe pas dans CategorieClients, création nécessaire'
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
    COUNT(DISTINCT Usage) as Nombre_Usages_Differents
FROM Clients
WHERE Usage IS NOT NULL;

-- Statistiques par catégorie
SELECT 
    '📊 Distribution des usages par catégorie:' as Info;
SELECT 
    cc.IdCategorie,
    cc.NomCategorie,
    COUNT(DISTINCT c.Usage) as Nombre_Usages_Differents,
    GROUP_CONCAT(DISTINCT c.Usage SEPARATOR ', ') as Usages_Uniques
FROM CategorieClients cc
LEFT JOIN Clients c ON cc.IdCategorie = c.IdCategorieClient
WHERE c.Usage IS NOT NULL
GROUP BY cc.IdCategorie, cc.NomCategorie
HAVING COUNT(DISTINCT c.Usage) > 1;

-- ============================================================================
-- PARTIE 2: AJOUT DE LA COLONNE Usage À CategorieClients
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '🔄 AJOUT DE LA COLONNE Usage À CategorieClients' as Title;
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
    END as Resultat_Ajout_Colonne;

-- ============================================================================
-- PARTIE 3: MIGRATION DES DONNÉES
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '🔄 MIGRATION DES DONNÉES' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Pour chaque catégorie, prendre l'usage le plus fréquent parmi ses clients
-- Si plusieurs usages différents, prendre le premier non-null trouvé
UPDATE CategorieClients cc
SET cc.Usage = (
    SELECT c.Usage
    FROM Clients c
    WHERE c.IdCategorieClient = cc.IdCategorie
      AND c.Usage IS NOT NULL
    GROUP BY c.Usage
    ORDER BY COUNT(*) DESC, c.Usage ASC
    LIMIT 1
)
WHERE cc.Usage IS NULL
  AND EXISTS (
      SELECT 1 
      FROM Clients c 
      WHERE c.IdCategorieClient = cc.IdCategorie 
        AND c.Usage IS NOT NULL
  );

-- Afficher le résultat
SELECT 
    CASE 
        WHEN ROW_COUNT() > 0 THEN CONCAT('✅ ', ROW_COUNT(), ' catégorie(s) mise(s) à jour')
        ELSE 'ℹ️  Toutes les catégories ont déjà un usage ou aucun client avec usage'
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
    COUNT(DISTINCT cc.IdCategorie) as Categories_Avec_Clients_Usage_Mais_Sans_Usage
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
    COUNT(c.IdClient) as Nombre_Clients
FROM CategorieClients cc
LEFT JOIN Clients c ON cc.IdCategorie = c.IdCategorieClient
WHERE cc.Usage IS NOT NULL
GROUP BY cc.IdCategorie, cc.NomCategorie, cc.Usage
LIMIT 10;

-- ============================================================================
-- PARTIE 5: SUPPRESSION DE LA COLONNE Usage DE Clients
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '⚠️  SUPPRESSION DE LA COLONNE Usage DE Clients' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- IMPORTANT: Vérifiez manuellement les résultats ci-dessus avant de décommenter
-- Si tout est OK, décommentez les lignes suivantes pour supprimer la colonne:

-- ALTER TABLE `Clients` DROP COLUMN `Usage`;

SELECT 
    '⚠️  ACTION REQUISE:' as Avertissement;
SELECT 
    '1. Vérifiez tous les résultats ci-dessus' as Etape;
SELECT 
    '2. Si tout est OK, décommentez la ligne ALTER TABLE pour supprimer Usage de Clients' as Etape;
SELECT 
    '3. Exécutez ensuite: COMMIT;' as Etape;
SELECT 
    '' as Separateur;

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
    COUNT(DISTINCT cc.IdCategorie) as Categories_Problematiques
FROM CategorieClients cc
INNER JOIN Clients c ON cc.IdCategorie = c.IdCategorieClient
WHERE c.Usage IS NOT NULL
  AND (cc.Usage IS NULL OR cc.Usage = '');

SELECT 
    CASE 
        WHEN (SELECT COUNT(*) FROM (
            SELECT COUNT(DISTINCT cc.IdCategorie) as cnt
            FROM CategorieClients cc
            INNER JOIN Clients c ON cc.IdCategorie = c.IdCategorieClient
            WHERE c.Usage IS NOT NULL
              AND (cc.Usage IS NULL OR cc.Usage = '')
        ) as subquery) = 0 THEN '✅ Aucune erreur détectée - Migration réussie!'
        ELSE '⚠️  ATTENTION: Certaines catégories ont des clients avec usage mais la catégorie n''a pas d''usage'
    END as Statut_Final;

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
    '3. ✅ Décommentez la ligne ALTER TABLE pour supprimer Usage de Clients' as Instruction;
SELECT 
    '4. ✅ Exécutez COMMIT; pour valider la transaction' as Instruction;
SELECT 
    '5. ✅ Testez l''application pour vous assurer que tout fonctionne' as Instruction;

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '✅ MIGRATION TERMINÉE' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
