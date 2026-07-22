-- ============================================================================
-- Script de migration : Copie des catégories existantes vers la table de jointure
-- Date : 2025-12-20
-- Description : Migre les données existantes de IdCategorieClient vers 
--               la table ClientCategorieClients (relation many-to-many)
-- IMPORTANT : Ce script est IDEMPOTENT - peut être exécuté plusieurs fois sans erreur
-- ============================================================================

USE `FactureNormaliseeRDC`;

-- ============================================================================
-- PARTIE 1: VÉRIFICATION PRÉALABLE
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '🔍 VÉRIFICATION PRÉALABLE' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Vérifier que la table ClientCategorieClients existe
SELECT 
    CASE 
        WHEN COUNT(*) > 0 THEN '✅ Table ClientCategorieClients existe'
        ELSE '❌ ERREUR: Table ClientCategorieClients n''existe pas. Exécutez d''abord la migration AddClientCategorieClientManyToMany'
    END as Statut_Table
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'ClientCategorieClients';

-- Compter les clients avec une catégorie principale
SELECT 
    '📊 Clients avec catégorie principale:' as Info;
SELECT 
    COUNT(*) as Nombre_Clients_Avec_Categorie
FROM Clients
WHERE IdCategorieClient IS NOT NULL;

-- Compter les relations déjà existantes dans ClientCategorieClients
SELECT 
    '📊 Relations déjà existantes:' as Info;
SELECT 
    COUNT(*) as Nombre_Relations_Existantes
FROM ClientCategorieClients;

-- ============================================================================
-- PARTIE 2: MIGRATION DES DONNÉES
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '🔄 MIGRATION DES DONNÉES' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Copier les catégories principales existantes vers la table de jointure
-- (uniquement si la relation n'existe pas déjà)
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

-- Afficher le résultat
SELECT 
    CASE 
        WHEN ROW_COUNT() > 0 THEN CONCAT('✅ ', ROW_COUNT(), ' relation(s) créée(s)')
        ELSE 'ℹ️ Toutes les catégories principales ont déjà été migrées'
    END as Resultat;

-- ============================================================================
-- PARTIE 3: VÉRIFICATION POST-MIGRATION
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '✅ VÉRIFICATION POST-MIGRATION' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Compter les relations après migration
SELECT 
    '📊 Relations après migration:' as Info;
SELECT 
    COUNT(*) as Nombre_Relations_Total
FROM ClientCategorieClients;

-- Vérifier que tous les clients avec catégorie principale ont été migrés
SELECT 
    '📊 Clients avec catégorie principale migrés:' as Info;
SELECT 
    COUNT(DISTINCT c.IdClient) as Clients_Migres
FROM Clients c
INNER JOIN ClientCategorieClients ccc ON c.IdClient = ccc.IdClient
WHERE c.IdCategorieClient IS NOT NULL
  AND ccc.IdCategorie = c.IdCategorieClient;

-- Liste des clients avec leurs catégories (principale + multiples)
SELECT 
    '📋 Exemples de clients avec leurs catégories:' as Info;
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
-- PARTIE 4: STATISTIQUES
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '📊 STATISTIQUES' as Title;
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

-- Clients avec plusieurs catégories
SELECT 
    'Clients avec plusieurs catégories' as Type,
    COUNT(DISTINCT c.IdClient) as Nombre
FROM Clients c
INNER JOIN ClientCategorieClients ccc ON c.IdClient = ccc.IdClient
GROUP BY c.IdClient
HAVING COUNT(ccc.IdCategorie) > 1;

-- ============================================================================
-- FIN DU SCRIPT
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '✅ MIGRATION TERMINÉE' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

SELECT 
    '📝 NOTES IMPORTANTES:' as Info;
SELECT 
    '1. Les clients conservent leur IdCategorieClient pour compatibilité ascendante' as Note;
SELECT 
    '2. Les nouvelles catégories peuvent être ajoutées via la relation many-to-many' as Note;
SELECT 
    '3. Les calculs d''arriérés utilisent maintenant toutes les catégories du client' as Note;
