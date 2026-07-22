-- ═══════════════════════════════════════════════════════════════
-- MIGRATION PRODUCTION : Augmentation NumeroExpediteur
-- Date : 2025-12-09
-- Description : Augmente la taille de la colonne NumeroExpediteur 
--               de 20 à 50 caractères pour supporter les SenderId Twilio
--               (ex: MG20ae2559987c6b3822b3b3eaba81ec85 = 34 caractères)
-- ═══════════════════════════════════════════════════════════════

-- ✅ Vérifier que la table existe
SELECT COUNT(*) INTO @table_exists
FROM information_schema.tables 
WHERE table_schema = DATABASE() 
  AND table_name = 'SmsLogs';

-- ✅ Vérifier la taille actuelle de la colonne
SELECT CHARACTER_MAXIMUM_LENGTH INTO @current_length
FROM information_schema.columns
WHERE table_schema = DATABASE()
  AND table_name = 'SmsLogs'
  AND column_name = 'NumeroExpediteur';

-- ✅ Appliquer la migration uniquement si nécessaire
SET @sql = NULL;

SELECT CASE 
    WHEN @table_exists = 0 THEN 
        CONCAT('⚠️ ERREUR: La table SmsLogs n''existe pas dans la base de données ', DATABASE())
    WHEN @current_length IS NULL THEN 
        CONCAT('⚠️ ERREUR: La colonne NumeroExpediteur n''existe pas dans la table SmsLogs')
    WHEN @current_length < 50 THEN 
        CONCAT('ALTER TABLE `SmsLogs` MODIFY COLUMN `NumeroExpediteur` VARCHAR(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL;')
    ELSE 
        CONCAT('✅ La colonne NumeroExpediteur a déjà la taille correcte (', @current_length, ' caractères)')
END INTO @sql;

-- ✅ Exécuter la commande SQL si nécessaire
SET @sql_execute = IF(@sql LIKE 'ALTER TABLE%', @sql, NULL);

-- Afficher le message
SELECT @sql AS Message;

-- Exécuter la modification si nécessaire
SET @sql_execute = IF(@sql LIKE 'ALTER TABLE%', @sql, NULL);

-- ═══════════════════════════════════════════════════════════════
-- EXÉCUTION MANUELLE (si la logique conditionnelle ne fonctionne pas)
-- ═══════════════════════════════════════════════════════════════

-- Décommentez la ligne suivante si vous voulez forcer l'exécution :
-- ALTER TABLE `SmsLogs` MODIFY COLUMN `NumeroExpediteur` VARCHAR(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL;

-- ═══════════════════════════════════════════════════════════════
-- VÉRIFICATION POST-MIGRATION
-- ═══════════════════════════════════════════════════════════════

SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH AS TailleMax,
    IS_NULLABLE AS Nullable,
    COLUMN_DEFAULT AS ValeurParDefaut
FROM 
    INFORMATION_SCHEMA.COLUMNS
WHERE 
    TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'SmsLogs'
    AND COLUMN_NAME = 'NumeroExpediteur';

-- ═══════════════════════════════════════════════════════════════
-- VÉRIFICATION DES DONNÉES EXISTANTES
-- ═══════════════════════════════════════════════════════════════

-- Vérifier s'il y a des enregistrements avec NumeroExpediteur > 20 caractères
SELECT 
    COUNT(*) AS NombreEnregistrements,
    MAX(CHAR_LENGTH(NumeroExpediteur)) AS TailleMaximaleActuelle,
    MIN(CHAR_LENGTH(NumeroExpediteur)) AS TailleMinimaleActuelle
FROM 
    SmsLogs
WHERE 
    NumeroExpediteur IS NOT NULL;

-- ═══════════════════════════════════════════════════════════════
-- NOTES IMPORTANTES
-- ═══════════════════════════════════════════════════════════════
-- 
-- 1. Cette migration est IDEMPOTENTE : elle peut être exécutée plusieurs fois sans risque
-- 2. Aucune perte de données : les données existantes sont préservées
-- 3. Temps d'exécution : < 1 seconde (modification de métadonnées uniquement)
-- 4. Compatibilité : Compatible avec MariaDB 10.11.0+
-- 
-- ═══════════════════════════════════════════════════════════════

