-- =====================================================
-- Script de Diagnostic : Ajout du champ IdAxe au modèle Client
-- =====================================================
-- Ce script permet de vérifier que la modification a été appliquée correctement
-- =====================================================

-- 1. Vérifier que la colonne IdAxe existe dans Clients
SELECT 
    'Colonne IdAxe dans Clients' AS Verification,
    CASE 
        WHEN COUNT(*) > 0 THEN '✅ Colonne IdAxe existe'
        ELSE '❌ Colonne IdAxe n''existe pas'
    END AS Statut
FROM information_schema.columns 
WHERE table_schema = DATABASE() 
  AND table_name = 'Clients' 
  AND column_name = 'IdAxe';

-- 2. Vérifier l'index sur IdAxe
SELECT 
    'Index IX_Client_IdAxe' AS Verification,
    CASE 
        WHEN COUNT(*) > 0 THEN '✅ Index IX_Client_IdAxe existe'
        ELSE '❌ Index IX_Client_IdAxe n''existe pas'
    END AS Statut
FROM information_schema.statistics 
WHERE table_schema = DATABASE() 
  AND table_name = 'Clients' 
  AND index_name = 'IX_Client_IdAxe';

-- 3. Vérifier la contrainte de clé étrangère
SELECT 
    CONSTRAINT_NAME AS 'Contrainte',
    TABLE_NAME AS 'Table',
    COLUMN_NAME AS 'Colonne',
    REFERENCED_TABLE_NAME AS 'Table Référencée',
    REFERENCED_COLUMN_NAME AS 'Colonne Référencée'
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'Clients'
  AND COLUMN_NAME = 'IdAxe'
  AND REFERENCED_TABLE_NAME IS NOT NULL;

-- 4. Compter les clients avec et sans axe
SELECT 
    'Répartition des clients par axe' AS Info,
    COUNT(*) AS 'Total Clients',
    COUNT(IdAxe) AS 'Clients avec Axe',
    COUNT(*) - COUNT(IdAxe) AS 'Clients sans Axe'
FROM Clients
WHERE Statut = true;

-- 5. Lister les clients avec leur axe
SELECT 
    c.IdClient,
    c.NomClient,
    a.NomAxe AS 'Nom Axe',
    cab.Nom AS 'Nom Cabine',
    s.Nom AS 'Nom Societe'
FROM Clients c
LEFT JOIN Axes a ON c.IdAxe = a.IdAxe
LEFT JOIN Cabines cab ON a.IdCabine = cab.IdCabine
LEFT JOIN Societes s ON cab.IdSociete = s.IdSociete
WHERE c.Statut = true
ORDER BY c.NomClient
LIMIT 20;

-- 6. Compter les clients par axe
SELECT 
    a.NomAxe AS 'Axe',
    COUNT(c.IdClient) AS 'Nombre de Clients'
FROM Axes a
LEFT JOIN Clients c ON a.IdAxe = c.IdAxe AND c.Statut = true
GROUP BY a.IdAxe, a.NomAxe
ORDER BY COUNT(c.IdClient) DESC;
