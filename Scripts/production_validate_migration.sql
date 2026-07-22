-- ============================================================================
-- SCRIPT DE VALIDATION : Vérification post-migration Client-CategorieClient
-- ============================================================================
-- 
-- DESCRIPTION:
-- Script de diagnostic pour vérifier que la migration s'est bien déroulée
-- Exécutez ce script APRÈS avoir commité la migration principale
--
-- UTILISATION:
-- mysql -u root -p FactureNormaliseeRDC < production_validate_migration.sql
-- ============================================================================

USE `FactureNormaliseeRDC`;

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '🔍 VALIDATION DE LA MIGRATION' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- ============================================================================
-- 1. VÉRIFICATION DE LA TABLE
-- ============================================================================

SELECT 
    '1️⃣  Vérification de la table ClientCategorieClients' as Section;

SELECT 
    CASE 
        WHEN COUNT(*) > 0 THEN '✅ Table ClientCategorieClients existe'
        ELSE '❌ ERREUR: Table ClientCategorieClients n''existe pas'
    END as Statut
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'ClientCategorieClients';

-- Vérifier la structure de la table
SELECT 
    'Structure de la table:' as Info;
DESCRIBE ClientCategorieClients;

-- ============================================================================
-- 2. VÉRIFICATION DES CONTRAINTES
-- ============================================================================

SELECT 
    '2️⃣  Vérification des contraintes' as Section;

SELECT 
    CONSTRAINT_NAME,
    TABLE_NAME,
    REFERENCED_TABLE_NAME
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'ClientCategorieClients'
  AND REFERENCED_TABLE_NAME IS NOT NULL;

-- ============================================================================
-- 3. VÉRIFICATION DE L'INTÉGRITÉ DES DONNÉES
-- ============================================================================

SELECT 
    '3️⃣  Vérification de l''intégrité des données' as Section;

-- Relations orphelines (ne devrait pas y en avoir)
SELECT 
    'Relations orphelines (catégorie inexistante):' as Check_Type,
    COUNT(*) as Nombre
FROM ClientCategorieClients ccc
LEFT JOIN CategorieClients cc ON ccc.IdCategorie = cc.IdCategorie
WHERE cc.IdCategorie IS NULL;

SELECT 
    'Relations orphelines (client inexistant):' as Check_Type,
    COUNT(*) as Nombre
FROM ClientCategorieClients ccc
LEFT JOIN Clients c ON ccc.IdClient = c.IdClient
WHERE c.IdClient IS NULL;

-- ============================================================================
-- 4. STATISTIQUES
-- ============================================================================

SELECT 
    '4️⃣  Statistiques' as Section;

-- Total des relations
SELECT 
    'Total des relations dans ClientCategorieClients:' as Info,
    COUNT(*) as Nombre
FROM ClientCategorieClients;

-- Clients avec catégorie principale migrée
SELECT 
    'Clients avec catégorie principale migrée:' as Info,
    COUNT(DISTINCT c.IdClient) as Nombre
FROM Clients c
INNER JOIN ClientCategorieClients ccc ON c.IdClient = ccc.IdClient
WHERE c.IdCategorieClient IS NOT NULL
  AND ccc.IdCategorie = c.IdCategorieClient;

-- Clients avec plusieurs catégories
SELECT 
    'Clients avec plusieurs catégories:' as Info,
    COUNT(DISTINCT ccc.IdClient) as Nombre
FROM ClientCategorieClients ccc
GROUP BY ccc.IdClient
HAVING COUNT(ccc.IdCategorie) > 1;

-- ============================================================================
-- 5. EXEMPLES DE DONNÉES
-- ============================================================================

SELECT 
    '5️⃣  Exemples de données' as Section;

-- Exemples de clients avec leurs catégories
SELECT 
    c.IdClient,
    c.NomClient,
    c.IdCategorieClient as Categorie_Principale,
    GROUP_CONCAT(ccc.IdCategorie ORDER BY ccc.DateAttribution SEPARATOR ', ') as Categories_Multiples,
    COUNT(ccc.IdCategorie) as Nombre_Categories
FROM Clients c
LEFT JOIN ClientCategorieClients ccc ON c.IdClient = ccc.IdClient
WHERE c.IdCategorieClient IS NOT NULL
GROUP BY c.IdClient, c.NomClient, c.IdCategorieClient
LIMIT 10;

-- ============================================================================
-- 6. RÉSUMÉ
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '📊 RÉSUMÉ' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

SELECT 
    (SELECT COUNT(*) FROM ClientCategorieClients) as Total_Relations,
    (SELECT COUNT(DISTINCT IdClient) FROM ClientCategorieClients) as Clients_Avec_Categories,
    (SELECT COUNT(*) FROM Clients WHERE IdCategorieClient IS NOT NULL) as Clients_Avec_Categorie_Principale;

SELECT 
    '✅ Si toutes les vérifications ci-dessus sont OK, la migration est réussie!' as Conclusion;
