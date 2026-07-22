-- ═══════════════════════════════════════════════════════════════
-- Migration : Augmentation de la taille de NumeroExpediteur
-- Date : 2025-12-09
-- Description : Augmente la taille de la colonne NumeroExpediteur 
--               de 20 à 50 caractères pour supporter les SenderId Twilio
-- ═══════════════════════════════════════════════════════════════

-- Vérifier si la colonne existe et modifier sa taille
ALTER TABLE `SmsLogs` 
MODIFY COLUMN `NumeroExpediteur` VARCHAR(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NULL;

-- Vérification
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM 
    INFORMATION_SCHEMA.COLUMNS
WHERE 
    TABLE_SCHEMA = DATABASE()
    AND TABLE_NAME = 'SmsLogs'
    AND COLUMN_NAME = 'NumeroExpediteur';
