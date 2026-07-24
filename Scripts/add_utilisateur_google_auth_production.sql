-- Auth Google : AuthProvider / ExternalSubjectId / EmailVerified sur Utilisateurs.
-- Idempotent + stamp EF.

SET @col := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'Utilisateurs'
      AND COLUMN_NAME = 'AuthProvider'
);
SET @sql := IF(@col = 0,
    'ALTER TABLE `Utilisateurs` ADD `AuthProvider` varchar(32) CHARACTER SET utf8mb4 NULL',
    'SELECT ''Utilisateurs.AuthProvider déjà présent'' AS Info'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'Utilisateurs'
      AND COLUMN_NAME = 'ExternalSubjectId'
);
SET @sql := IF(@col = 0,
    'ALTER TABLE `Utilisateurs` ADD `ExternalSubjectId` varchar(128) CHARACTER SET utf8mb4 NULL',
    'SELECT ''Utilisateurs.ExternalSubjectId déjà présent'' AS Info'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'Utilisateurs'
      AND COLUMN_NAME = 'EmailVerified'
);
SET @sql := IF(@col = 0,
    'ALTER TABLE `Utilisateurs` ADD `EmailVerified` tinyint(1) NULL',
    'SELECT ''Utilisateurs.EmailVerified déjà présent'' AS Info'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @idx := (
    SELECT COUNT(*) FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'Utilisateurs'
      AND INDEX_NAME = 'IX_Utilisateurs_AuthProvider_ExternalSubjectId'
);
SET @sql := IF(@idx = 0,
    'CREATE UNIQUE INDEX `IX_Utilisateurs_AuthProvider_ExternalSubjectId` ON `Utilisateurs` (`AuthProvider`, `ExternalSubjectId`)',
    'SELECT ''Index IX_Utilisateurs_AuthProvider_ExternalSubjectId déjà présent'' AS Info'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
SELECT '20260724193702_AddUtilisateurGoogleAuthFields', '6.0.25'
FROM DUAL
WHERE NOT EXISTS (
    SELECT 1 FROM `__EFMigrationsHistory`
    WHERE `MigrationId` = '20260724193702_AddUtilisateurGoogleAuthFields'
);
