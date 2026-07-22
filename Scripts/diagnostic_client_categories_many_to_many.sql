-- ============================================================================
-- SCRIPT DE DIAGNOSTIC : État de la migration Client-CategorieClient Many-to-Many
-- Date : 2025-12-20
-- Description : Vérifie l'état actuel de la base de données concernant
--               la relation many-to-many entre Client et CategorieClient
-- ============================================================================

SELECT 
    '═══════════════════════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '🔍 DIAGNOSTIC : État de la migration Client-CategorieClient Many-to-Many' as Title;
SELECT 
    '═══════════════════════════════════════════════════════════════════════════════' as Separator;

-- ============================================================================
-- PARTIE 1: VÉRIFICATION DE LA TABLE ClientCategorieClients
-- ============================================================================

SELECT 
    '═══════════════════════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '1️⃣ VÉRIFICATION DE LA TABLE ClientCategorieClients' as Title;
SELECT 
    '═══════════════════════════════════════════════════════════════════════════════' as Separator;

-- Existence de la table
SELECT 
    CASE 
        WHEN COUNT(*) > 0 THEN '✅ Table ClientCategorieClients existe'
        ELSE '❌ Table ClientCategorieClients n''existe pas - Migration non effectuée'
    END as Statut_Table
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'ClientCategorieClients';

-- Structure de la table
SELECT 
    'Structure de la table ClientCategorieClients:' as Info;
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'ClientCategorieClients'
ORDER BY ORDINAL_POSITION;

-- Index de la table
SELECT 
    'Index de la table ClientCategorieClients:' as Info;
SELECT 
    INDEX_NAME,
    COLUMN_NAME,
    NON_UNIQUE,
    SEQ_IN_INDEX
FROM INFORMATION_SCHEMA.STATISTICS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'ClientCategorieClients'
ORDER BY INDEX_NAME, SEQ_IN_INDEX;

-- Contraintes de clés étrangères
SELECT 
    'Contraintes de clés étrangères:' as Info;
SELECT 
    CONSTRAINT_NAME,
    TABLE_NAME,
    COLUMN_NAME,
    REFERENCED_TABLE_NAME,
    REFERENCED_COLUMN_NAME,
    DELETE_RULE,
    UPDATE_RULE
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
INNER JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc 
    ON kcu.CONSTRAINT_NAME = rc.CONSTRAINT_NAME
WHERE kcu.TABLE_SCHEMA = DATABASE()
  AND kcu.TABLE_NAME = 'ClientCategorieClients';

-- ============================================================================
-- PARTIE 2: STATISTIQUES GÉNÉRALES
-- ============================================================================

SELECT 
    '═══════════════════════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '2️⃣ STATISTIQUES GÉNÉRALES' as Title;
SELECT 
    '═══════════════════════════════════════════════════════════════════════════════' as Separator;

-- Statistiques sur les clients
SELECT 
    'Statistiques sur les clients:' as Info;
SELECT 
    COUNT(*) as Total_Clients,
    COUNT(IdCategorieClient) as Clients_Avec_Categorie_Principale,
    COUNT(*) - COUNT(IdCategorieClient) as Clients_Sans_Categorie_Principale,
    COUNT(DISTINCT IdCategorieClient) as Nombre_Categories_Differentes
FROM Clients;

-- Statistiques sur les relations many-to-many
SELECT 
    'Statistiques sur les relations many-to-many:' as Info;
SELECT 
    CASE 
        WHEN (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES 
              WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'ClientCategorieClients') > 0
        THEN (
            SELECT 
                COUNT(*) as Total_Relations,
                COUNT(DISTINCT IdClient) as Clients_Avec_Relations,
                COUNT(DISTINCT IdCategorie) as Categories_Utilisees,
                MIN(DateAttribution) as Premiere_Attribution,
                MAX(DateAttribution) as Derniere_Attribution
            FROM ClientCategorieClients
        )
        ELSE SELECT 
            'Table ClientCategorieClients n''existe pas' as Message,
            0 as Total_Relations,
            0 as Clients_Avec_Relations,
            0 as Categories_Utilisees
    END as Resultat;

-- ============================================================================
-- PARTIE 3: VÉRIFICATION DE L'INTÉGRITÉ DES DONNÉES
-- ============================================================================

SELECT 
    '═══════════════════════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '3️⃣ VÉRIFICATION DE L''INTÉGRITÉ DES DONNÉES' as Title;
SELECT 
    '═══════════════════════════════════════════════════════════════════════════════' as Separator;

-- Clients avec IdCategorieClient mais pas dans ClientCategorieClients
SELECT 
    '⚠️ Clients avec catégorie principale non migrés:' as Info;
SELECT 
    c.IdClient,
    c.NomClient,
    c.IdCategorieClient,
    cat.NomCategorie
FROM Clients c
LEFT JOIN CategorieClients cat ON c.IdCategorieClient = cat.IdCategorie
WHERE c.IdCategorieClient IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 
      FROM ClientCategorieClients ccc 
      WHERE ccc.IdClient = c.IdClient 
        AND ccc.IdCategorie = c.IdCategorieClient
  )
LIMIT 20;

-- Relations orphelines (sans client ou catégorie valide)
SELECT 
    '⚠️ Relations orphelines (à vérifier):' as Info;
SELECT 
    ccc.IdClient,
    ccc.IdCategorie,
    CASE WHEN c.IdClient IS NULL THEN 'Client inexistant' ELSE 'OK' END as Statut_Client,
    CASE WHEN cat.IdCategorie IS NULL THEN 'Catégorie inexistante' ELSE 'OK' END as Statut_Categorie
FROM ClientCategorieClients ccc
LEFT JOIN Clients c ON ccc.IdClient = c.IdClient
LEFT JOIN CategorieClients cat ON ccc.IdCategorie = cat.IdCategorie
WHERE c.IdClient IS NULL OR cat.IdCategorie IS NULL
LIMIT 20;

-- Clients avec plusieurs catégories
SELECT 
    '📊 Clients avec plusieurs catégories:' as Info;
SELECT 
    ccc.IdClient,
    c.NomClient,
    COUNT(ccc.IdCategorie) as Nombre_Categories,
    GROUP_CONCAT(ccc.IdCategorie ORDER BY ccc.DateAttribution SEPARATOR ', ') as Categories
FROM ClientCategorieClients ccc
INNER JOIN Clients c ON ccc.IdClient = c.IdClient
GROUP BY ccc.IdClient, c.NomClient
HAVING COUNT(ccc.IdCategorie) > 1
ORDER BY Nombre_Categories DESC
LIMIT 20;

-- ============================================================================
-- PARTIE 4: COMPARAISON AVANT/APRÈS MIGRATION
-- ============================================================================

SELECT 
    '═══════════════════════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '4️⃣ COMPARAISON AVANT/APRÈS MIGRATION' as Title;
SELECT 
    '═══════════════════════════════════════════════════════════════════════════════' as Separator;

-- Taux de migration
SELECT 
    'Taux de migration des catégories principales:' as Info;
SELECT 
    (SELECT COUNT(DISTINCT c.IdClient)
     FROM Clients c
     INNER JOIN ClientCategorieClients ccc ON c.IdClient = ccc.IdClient
     WHERE c.IdCategorieClient IS NOT NULL
       AND ccc.IdCategorie = c.IdCategorieClient) as Clients_Migres,
    (SELECT COUNT(*)
     FROM Clients
     WHERE IdCategorieClient IS NOT NULL) as Clients_Avec_Categorie_Principale,
    ROUND(
        (SELECT COUNT(DISTINCT c.IdClient)
         FROM Clients c
         INNER JOIN ClientCategorieClients ccc ON c.IdClient = ccc.IdClient
         WHERE c.IdCategorieClient IS NOT NULL
           AND ccc.IdCategorie = c.IdCategorieClient) * 100.0 /
        NULLIF((SELECT COUNT(*)
                FROM Clients
                WHERE IdCategorieClient IS NOT NULL), 0),
        2
    ) as Taux_Migration_Pourcentage;

-- ============================================================================
-- PARTIE 5: RECOMMANDATIONS
-- ============================================================================

SELECT 
    '═══════════════════════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '5️⃣ RECOMMANDATIONS' as Title;
SELECT 
    '═══════════════════════════════════════════════════════════════════════════════' as Separator;

-- Évaluer l'état et donner des recommandations
SELECT 
    CASE 
        WHEN (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES 
              WHERE TABLE_SCHEMA = DATABASE() 
                AND TABLE_NAME = 'ClientCategorieClients') = 0
        THEN '❌ ACTION REQUISE : Exécuter le script de migration production_migrate_client_categories_many_to_many.sql'
        WHEN (SELECT COUNT(*) 
              FROM Clients c
              WHERE c.IdCategorieClient IS NOT NULL
                AND NOT EXISTS (
                    SELECT 1 
                    FROM ClientCategorieClients ccc 
                    WHERE ccc.IdClient = c.IdClient 
                      AND ccc.IdCategorie = c.IdCategorieClient
                )) > 0
        THEN CONCAT(
            '⚠️ ATTENTION : ',
            (SELECT COUNT(*) 
             FROM Clients c
             WHERE c.IdCategorieClient IS NOT NULL
               AND NOT EXISTS (
                   SELECT 1 
                   FROM ClientCategorieClients ccc 
                   WHERE ccc.IdClient = c.IdClient 
                     AND ccc.IdCategorie = c.IdCategorieClient
               )),
            ' client(s) avec catégorie principale non migré(s). Relancer le script de migration.'
        )
        ELSE '✅ MIGRATION COMPLÈTE : Tous les clients avec catégorie principale ont été migrés'
    END as Recommandation;

SELECT 
    '═══════════════════════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '✅ DIAGNOSTIC TERMINÉ' as Title;
SELECT 
    '═══════════════════════════════════════════════════════════════════════════════' as Separator;
