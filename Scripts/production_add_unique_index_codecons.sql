-- =====================================================
-- Script SQL : Ajout de l'index unique sur CodeCons
-- =====================================================
-- Description : Ajoute une contrainte d'unicité sur le champ CodeCons
--               de la table Clients pour garantir qu'un CodeCons
--               ne peut être utilisé que par un seul client.
-- 
-- Date : 2025-01-05
-- Version : 1.0.0
-- =====================================================

-- ⚠️ IMPORTANT : Exécuter d'abord la vérification des doublons
-- avant de créer l'index unique

-- =====================================================
-- ÉTAPE 1 : Vérification des doublons existants
-- =====================================================
-- Cette requête permet de détecter les doublons de CodeCons
-- avant de créer l'index unique.

SELECT 
    CodeCons,
    COUNT(*) as NombreOccurrences,
    GROUP_CONCAT(IdClient ORDER BY IdClient SEPARATOR ', ') as IdClients
FROM Clients
WHERE CodeCons IS NOT NULL 
  AND CodeCons != ''
  AND Statut = 1  -- Seulement les clients actifs
GROUP BY CodeCons
HAVING COUNT(*) > 1;

-- ⚠️ Si cette requête retourne des résultats, il faut résoudre
-- les doublons avant de créer l'index unique.

-- =====================================================
-- ÉTAPE 2 : Résolution des doublons (si nécessaire)
-- =====================================================
-- Si des doublons sont détectés, vous devez décider comment les résoudre :
-- Option 1 : Supprimer les doublons (soft delete)
-- Option 2 : Modifier les CodeCons pour les rendre uniques
-- Option 3 : Conserver seulement le premier client créé

-- Exemple : Soft delete des doublons (garder le premier créé)
-- UPDATE Clients c1
-- INNER JOIN (
--     SELECT CodeCons, MIN(IdClient) as PremierIdClient
--     FROM Clients
--     WHERE CodeCons IS NOT NULL 
--       AND CodeCons != ''
--       AND Statut = 1
--     GROUP BY CodeCons
--     HAVING COUNT(*) > 1
-- ) c2 ON c1.CodeCons = c2.CodeCons
-- SET c1.Statut = 0, c1.IsActif = 0
-- WHERE c1.IdClient != c2.PremierIdClient
--   AND c1.Statut = 1;

-- =====================================================
-- ÉTAPE 3 : Vérification des valeurs NULL
-- =====================================================
-- Compter les clients sans CodeCons
SELECT 
    COUNT(*) as ClientsSansCodeCons
FROM Clients
WHERE (CodeCons IS NULL OR CodeCons = '')
  AND Statut = 1;

-- ⚠️ Note : Les valeurs NULL sont autorisées dans l'index unique
-- (plusieurs clients peuvent avoir CodeCons = NULL)

-- =====================================================
-- ÉTAPE 4 : Création de l'index unique
-- =====================================================
-- Créer l'index unique sur CodeCons
-- Cet index garantit qu'un CodeCons ne peut être utilisé que par un seul client

CREATE UNIQUE INDEX IX_Client_CodeCons_Unique 
ON Clients(CodeCons);

-- =====================================================
-- ÉTAPE 5 : Vérification de l'index créé
-- =====================================================
-- Vérifier que l'index a bien été créé
SHOW INDEX FROM Clients WHERE Key_name = 'IX_Client_CodeCons_Unique';

-- =====================================================
-- NOTES IMPORTANTES
-- =====================================================
-- 1. L'index unique permet plusieurs valeurs NULL
--    (plusieurs clients peuvent avoir CodeCons = NULL)
-- 
-- 2. Si un CodeCons est déjà utilisé, toute tentative
--    d'insertion ou de mise à jour avec ce CodeCons échouera
--    avec une erreur de contrainte unique
-- 
-- 3. Pour les clients existants sans CodeCons, il faudra
--    générer un CodeCons unique lors de la prochaine mise à jour
-- 
-- 4. La génération automatique de CodeCons lors de la création
--    d'un client garantit qu'un CodeCons unique sera toujours
--    généré si IdAxe est fourni

-- =====================================================
-- ROLLBACK (si nécessaire)
-- =====================================================
-- Pour supprimer l'index unique si nécessaire :
-- DROP INDEX IX_Client_CodeCons_Unique ON Clients;
