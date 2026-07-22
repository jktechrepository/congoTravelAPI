-- =====================================================
-- Script de Diagnostic : État de la table Clients
-- =====================================================
-- Description : Ce script vérifie l'état actuel de la table Clients
--               concernant les champs IdAxe et Zone
-- =====================================================

-- =====================================================
-- 1. Vérification de la colonne IdAxe
-- =====================================================

SELECT 
    'Colonne IdAxe' AS Element,
    CASE 
        WHEN COUNT(*) > 0 THEN '✅ Présente'
        ELSE '❌ Absente'
    END AS Statut,
    CASE 
        WHEN COUNT(*) > 0 THEN 
            CONCAT('Type: ', data_type, 
                   ', Nullable: ', IF(is_nullable = 'YES', 'Oui', 'Non'))
        ELSE 'N/A'
    END AS Details
FROM information_schema.columns 
WHERE table_schema = DATABASE() 
  AND table_name = 'Clients' 
  AND column_name = 'IdAxe';

-- =====================================================
-- 2. Vérification de l'index IX_Client_IdAxe
-- =====================================================

SELECT 
    'Index IX_Client_IdAxe' AS Element,
    CASE 
        WHEN COUNT(*) > 0 THEN '✅ Présent'
        ELSE '❌ Absent'
    END AS Statut,
    CASE 
        WHEN COUNT(*) > 0 THEN 
            CONCAT('Colonnes: ', GROUP_CONCAT(column_name ORDER BY seq_in_index))
        ELSE 'N/A'
    END AS Details
FROM information_schema.statistics 
WHERE table_schema = DATABASE() 
  AND table_name = 'Clients' 
  AND index_name = 'IX_Client_IdAxe'
GROUP BY index_name;

-- =====================================================
-- 3. Vérification de la clé étrangère FK_Clients_Axes_IdAxe
-- =====================================================

SELECT 
    'Clé étrangère FK_Clients_Axes_IdAxe' AS Element,
    CASE 
        WHEN COUNT(*) > 0 THEN '✅ Présente'
        ELSE '❌ Absente'
    END AS Statut,
    CASE 
        WHEN COUNT(*) > 0 THEN 
            CONCAT('Table référencée: ', referenced_table_name, 
                   ', Colonne: ', referenced_column_name,
                   ', ON DELETE: ', delete_rule)
        ELSE 'N/A'
    END AS Details
FROM information_schema.key_column_usage kcu
JOIN information_schema.referential_constraints rc 
    ON kcu.constraint_name = rc.constraint_name
WHERE kcu.table_schema = DATABASE() 
  AND kcu.table_name = 'Clients' 
  AND kcu.constraint_name = 'FK_Clients_Axes_IdAxe';

-- =====================================================
-- 4. Vérification de la colonne Zone
-- =====================================================

SELECT 
    'Colonne Zone' AS Element,
    CASE 
        WHEN COUNT(*) > 0 THEN '⚠️ Toujours présente (à supprimer)'
        ELSE '✅ Absente (déjà supprimée)'
    END AS Statut,
    CASE 
        WHEN COUNT(*) > 0 THEN 
            CONCAT('Type: ', data_type, 
                   ', Nullable: ', IF(is_nullable = 'YES', 'Oui', 'Non'))
        ELSE 'N/A'
    END AS Details
FROM information_schema.columns 
WHERE table_schema = DATABASE() 
  AND table_name = 'Clients' 
  AND column_name = 'Zone';

-- =====================================================
-- 5. Statistiques sur les clients avec/sans IdAxe
-- =====================================================

SELECT 
    'Statistiques IdAxe' AS Element,
    CONCAT(
        'Total clients: ', COUNT(*), 
        ' | Avec IdAxe: ', SUM(CASE WHEN IdAxe IS NOT NULL THEN 1 ELSE 0 END),
        ' | Sans IdAxe: ', SUM(CASE WHEN IdAxe IS NULL THEN 1 ELSE 0 END)
    ) AS Details
FROM Clients;

-- =====================================================
-- 6. Vérification de l'existence de la table Axes
-- =====================================================

SELECT 
    'Table Axes' AS Element,
    CASE 
        WHEN COUNT(*) > 0 THEN '✅ Existe'
        ELSE '❌ N''existe pas (requis pour la FK)'
    END AS Statut,
    CASE 
        WHEN COUNT(*) > 0 THEN 
            CONCAT('Nombre d''axes: ', (SELECT COUNT(*) FROM Axes))
        ELSE 'N/A'
    END AS Details
FROM information_schema.tables 
WHERE table_schema = DATABASE() 
  AND table_name = 'Axes';

-- =====================================================
-- 7. Liste des colonnes actuelles de la table Clients
-- =====================================================

SELECT 
    'Colonnes de Clients' AS Element,
    GROUP_CONCAT(column_name ORDER BY ordinal_position SEPARATOR ', ') AS Details
FROM information_schema.columns 
WHERE table_schema = DATABASE() 
  AND table_name = 'Clients';

-- =====================================================
-- FIN DU DIAGNOSTIC
-- =====================================================
