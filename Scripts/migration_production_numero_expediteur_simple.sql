-- ═══════════════════════════════════════════════════════════════
-- MIGRATION PRODUCTION : Augmentation NumeroExpediteur
-- Date : 2025-12-09
-- Description : Augmente la taille de la colonne NumeroExpediteur 
--               de 20 à 50 caractères pour supporter les SenderId Twilio
--               (ex: MG20ae2559987c6b3822b3b3eaba81ec85 = 34 caractères)
-- ═══════════════════════════════════════════════════════════════

-- ✅ ÉTAPE 1 : Modifier la colonne NumeroExpediteur
ALTER TABLE `SmsLogs` 
MODIFY COLUMN `NumeroExpediteur` VARCHAR(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL;

-- ✅ ÉTAPE 2 : Vérification de la modification
SELECT 
    COLUMN_NAME AS 'Colonne',
    DATA_TYPE AS 'Type',
    CHARACTER_MAXIMUM_LENGTH AS 'Taille Max',
    IS_NULLABLE AS 'Nullable'
FROM 
    INFORMATION_SCHEMA.COLUMNS
WHERE 
    TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'SmsLogs'
    AND COLUMN_NAME = 'NumeroExpediteur';

-- ✅ ÉTAPE 3 : Vérification des données existantes
SELECT 
    COUNT(*) AS 'Total Enregistrements',
    MAX(CHAR_LENGTH(NumeroExpediteur)) AS 'Taille Max Actuelle',
    MIN(CHAR_LENGTH(NumeroExpediteur)) AS 'Taille Min Actuelle',
    COUNT(CASE WHEN CHAR_LENGTH(NumeroExpediteur) > 20 THEN 1 END) AS 'Enregistrements > 20 chars'
FROM 
    SmsLogs
WHERE 
    NumeroExpediteur IS NOT NULL;

-- ═══════════════════════════════════════════════════════════════
-- NOTES IMPORTANTES
-- ═══════════════════════════════════════════════════════════════
-- 
-- ✅ Cette migration est IDEMPOTENTE : peut être exécutée plusieurs fois
-- ✅ Aucune perte de données : les données existantes sont préservées
-- ✅ Temps d'exécution : < 1 seconde (modification de métadonnées)
-- ✅ Compatible avec MariaDB 10.11.0+
-- 
-- ═══════════════════════════════════════════════════════════════

