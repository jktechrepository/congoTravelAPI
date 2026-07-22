-- ============================================================================
-- SCRIPT DE MIGRATION PRODUCTION : Ajout du champ CodeCons au modèle Client
-- ============================================================================
-- 
-- DESCRIPTION:
-- Ce script ajoute la colonne CodeCons (code consommateur) à la table Clients.
-- Ce champ est nullable et optionnel.
--
-- IMPORTANT - AVANT D'EXÉCUTER:
-- 1. ⚠️  FAITES UN BACKUP COMPLET de votre base de données
-- 2. Testez ce script sur une base de données de test d'abord
-- 3. Vérifiez que vous êtes connecté à la bonne base de données
-- 4. Exécutez pendant une période de faible activité si possible
--
-- MODIFICATIONS:
-- - Ajoute la colonne CodeCons (VARCHAR(100), nullable) à la table Clients
--
-- DURÉE ESTIMÉE: < 1 minute
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

-- Vérifier que la table Clients existe
SELECT 
    CASE 
        WHEN COUNT(*) > 0 THEN CONCAT('✅ Table Clients existe (', COUNT(*), ' ligne(s))')
        ELSE '❌ ERREUR: Table Clients n''existe pas'
    END as Statut_Clients
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'Clients';

-- Vérifier si la colonne CodeCons existe déjà
SELECT 
    CASE 
        WHEN COUNT(*) > 0 THEN '⚠️  ATTENTION: Colonne CodeCons existe déjà dans Clients'
        ELSE '✅ Colonne CodeCons n''existe pas dans Clients'
    END as Statut_CodeCons
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'Clients'
  AND COLUMN_NAME = 'CodeCons';

-- Statistiques avant migration
SELECT 
    '📊 STATISTIQUES AVANT MIGRATION' as Info;
SELECT 
    COUNT(*) as Total_Clients
FROM Clients;

-- ============================================================================
-- PARTIE 2: AJOUT DE LA COLONNE CodeCons
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '🔄 AJOUT DE LA COLONNE CodeCons' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Vérifier si la colonne existe déjà
SET @col_exists := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'Clients'
      AND COLUMN_NAME = 'CodeCons'
);

-- Ajouter la colonne si elle n'existe pas
SET @sql_add_col := IF(@col_exists = 0,
    'ALTER TABLE `Clients` ADD COLUMN `CodeCons` VARCHAR(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL DEFAULT NULL AFTER `numero_compteur`;',
    'SELECT "Colonne CodeCons existe déjà dans Clients, création ignorée." AS message;'
);

PREPARE stmt_add_col FROM @sql_add_col;
EXECUTE stmt_add_col;
DEALLOCATE PREPARE stmt_add_col;

SELECT 
    CASE 
        WHEN @col_exists = 0 THEN '✅ Colonne CodeCons ajoutée à Clients'
        ELSE 'ℹ️  Colonne CodeCons existe déjà dans Clients'
    END as Resultat_Ajout_Colonne;

-- ============================================================================
-- PARTIE 3: VÉRIFICATIONS POST-MIGRATION
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '✅ VÉRIFICATIONS POST-MIGRATION' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Vérifier que la colonne existe
SELECT 
    CASE 
        WHEN COUNT(*) > 0 THEN '✅ Colonne CodeCons existe dans Clients'
        ELSE '❌ ERREUR: Colonne CodeCons n''existe pas dans Clients'
    END as Verification_Colonne
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'Clients'
  AND COLUMN_NAME = 'CodeCons';

-- Vérifier la structure de la colonne
SELECT 
    'Structure de la colonne CodeCons:' as Info;
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'Clients'
  AND COLUMN_NAME = 'CodeCons';

-- Statistiques
SELECT 
    '📊 STATISTIQUES' as Info;
SELECT 
    COUNT(*) as Total_Clients,
    COUNT(CodeCons) as Clients_Avec_CodeCons,
    COUNT(*) - COUNT(CodeCons) as Clients_Sans_CodeCons
FROM Clients;

-- ============================================================================
-- VALIDATION ET COMMIT
-- ============================================================================

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '✅ VALIDATION' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;

-- Vérifier qu'il n'y a pas d'erreurs
SET @errors := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'Clients'
      AND COLUMN_NAME = 'CodeCons'
);

SELECT 
    CASE 
        WHEN @errors > 0 THEN '✅ Aucune erreur détectée - Migration réussie!'
        ELSE '❌ ERREUR: Colonne CodeCons n''a pas été créée'
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
    '2. ✅ Si tout est OK, exécutez: COMMIT;' as Instruction;
SELECT 
    '3. ✅ Si vous détectez des erreurs, exécutez: ROLLBACK;' as Instruction;
SELECT 
    '4. ✅ Testez l''application pour vous assurer que tout fonctionne' as Instruction;
SELECT 
    '5. ✅ Vérifiez que le template Excel contient bien la colonne CodeCons' as Instruction;
SELECT 
    '6. ✅ Testez l''import Excel avec des données contenant CodeCons' as Instruction;

SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
SELECT 
    '✅ MIGRATION TERMINÉE' as Title;
SELECT 
    '════════════════════════════════════════════════════════════' as Separator;
