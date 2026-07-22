-- =============================================================================
-- Production : Clients.AdresseClient optionnel (+ alignement Agents email/tél.)
-- Migration EF équivalente : 20260619134037_ClientAdresseClientOptional
-- =============================================================================
-- Objectif :
--   - Permettre NULL sur Clients.AdresseClient (champ optionnel côté API)
--   - Normaliser les adresses vides existantes en NULL
--   - Aligner Agents.TelephoneAgent / Agents.EmailAgent sur varchar(200)
--
-- Prérequis :
--   - MySQL 5.7+ / 8.x
--   - Tables `Clients`, `Agents` présentes
--
-- EXÉCUTION :
--   1. USE nom_de_votre_base;
--   2. Sauvegarde recommandée
--   3. Exécuter ce script en entier (idempotent)
--   4. Vérifier les SELECT de contrôle en fin de script
-- =============================================================================

SET @db := DATABASE();

-- -----------------------------------------------------------------------------
-- 0. Vérification avant
-- -----------------------------------------------------------------------------
SELECT
    COUNT(*) AS ClientsAdresseVideOuEspaces,
    SUM(CASE WHEN AdresseClient IS NULL THEN 1 ELSE 0 END) AS ClientsAdresseDejaNull
FROM `Clients`
WHERE AdresseClient IS NOT NULL AND TRIM(AdresseClient) = '';

SELECT
    COLUMN_NAME,
    IS_NULLABLE,
    COLUMN_TYPE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = @db
  AND TABLE_NAME = 'Clients'
  AND COLUMN_NAME = 'AdresseClient';

SELECT
    COLUMN_NAME,
    IS_NULLABLE,
    COLUMN_TYPE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = @db
  AND TABLE_NAME = 'Agents'
  AND COLUMN_NAME IN ('TelephoneAgent', 'EmailAgent');

-- -----------------------------------------------------------------------------
-- 1. Backfill : adresses vides → NULL (idempotent)
-- -----------------------------------------------------------------------------
UPDATE `Clients`
SET `AdresseClient` = NULL
WHERE `AdresseClient` IS NOT NULL
  AND TRIM(`AdresseClient`) = '';

-- -----------------------------------------------------------------------------
-- 2. Clients.AdresseClient nullable (idempotent)
-- -----------------------------------------------------------------------------
SET @clients_adresse_nullable := (
    SELECT IS_NULLABLE
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db
      AND TABLE_NAME = 'Clients'
      AND COLUMN_NAME = 'AdresseClient'
    LIMIT 1
);

SET @sql_clients_adresse := IF(
    @clients_adresse_nullable = 'NO',
    'ALTER TABLE `Clients` MODIFY COLUMN `AdresseClient` varchar(500) CHARACTER SET utf8mb4 NULL',
    'SELECT ''Clients.AdresseClient déjà nullable — ignoré'' AS Info'
);

PREPARE stmt FROM @sql_clients_adresse;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- -----------------------------------------------------------------------------
-- 3. Agents.TelephoneAgent → varchar(200) (idempotent)
-- -----------------------------------------------------------------------------
SET @agents_tel_type := (
    SELECT COLUMN_TYPE
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db
      AND TABLE_NAME = 'Agents'
      AND COLUMN_NAME = 'TelephoneAgent'
    LIMIT 1
);

SET @sql_agents_tel := IF(
    @agents_tel_type IS NOT NULL AND @agents_tel_type <> 'varchar(200)',
    'ALTER TABLE `Agents` MODIFY COLUMN `TelephoneAgent` varchar(200) CHARACTER SET utf8mb4 NULL',
    'SELECT ''Agents.TelephoneAgent déjà varchar(200) ou colonne absente — ignoré'' AS Info'
);

PREPARE stmt FROM @sql_agents_tel;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- -----------------------------------------------------------------------------
-- 4. Agents.EmailAgent → varchar(200) (idempotent)
-- -----------------------------------------------------------------------------
SET @agents_email_type := (
    SELECT COLUMN_TYPE
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db
      AND TABLE_NAME = 'Agents'
      AND COLUMN_NAME = 'EmailAgent'
    LIMIT 1
);

SET @sql_agents_email := IF(
    @agents_email_type IS NOT NULL AND @agents_email_type <> 'varchar(200)',
    'ALTER TABLE `Agents` MODIFY COLUMN `EmailAgent` varchar(200) CHARACTER SET utf8mb4 NULL',
    'SELECT ''Agents.EmailAgent déjà varchar(200) ou colonne absente — ignoré'' AS Info'
);

PREPARE stmt FROM @sql_agents_email;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- -----------------------------------------------------------------------------
-- 5. Historique EF (évite un re-run si dotnet ef database update est utilisé ensuite)
-- -----------------------------------------------------------------------------
INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260619134037_ClientAdresseClientOptional', '6.0.25');

-- -----------------------------------------------------------------------------
-- 6. Vérification après
-- -----------------------------------------------------------------------------
SELECT
    COUNT(*) AS ClientsAdresseVideOuEspacesRestantes
FROM `Clients`
WHERE AdresseClient IS NOT NULL AND TRIM(AdresseClient) = '';

SELECT
    COLUMN_NAME,
    IS_NULLABLE,
    COLUMN_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = @db
  AND TABLE_NAME = 'Clients'
  AND COLUMN_NAME = 'AdresseClient';

SELECT
    COLUMN_NAME,
    IS_NULLABLE,
    COLUMN_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = @db
  AND TABLE_NAME = 'Agents'
  AND COLUMN_NAME IN ('TelephoneAgent', 'EmailAgent');

SELECT `MigrationId`, `ProductVersion`
FROM `__EFMigrationsHistory`
WHERE `MigrationId` = '20260619134037_ClientAdresseClientOptional';

-- Attendu :
--   Clients.AdresseClient : IS_NULLABLE = YES, COLUMN_TYPE = varchar(500)
--   ClientsAdresseVideOuEspacesRestantes = 0
--   1 ligne dans __EFMigrationsHistory pour cette migration

-- -----------------------------------------------------------------------------
-- Rollback manuel (déconseillé si des NULL existent déjà)
-- -----------------------------------------------------------------------------
-- UPDATE `Clients` SET `AdresseClient` = '' WHERE `AdresseClient` IS NULL;
-- ALTER TABLE `Clients` MODIFY COLUMN `AdresseClient` varchar(500) CHARACTER SET utf8mb4 NOT NULL;
-- DELETE FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20260619134037_ClientAdresseClientOptional';
