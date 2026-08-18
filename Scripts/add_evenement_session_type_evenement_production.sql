-- Ajoute TypeEvenement + coordonnées organisateur sur EvenementSessions.
-- Migrations EF : AddEvenementSessionTypeEvenement + AddEvenementSessionOrganisateurFields

SET @db := DATABASE();

SET @col_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db
      AND TABLE_NAME = 'EvenementSessions'
      AND COLUMN_NAME = 'TypeEvenement'
);

SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE `EvenementSessions`
        ADD COLUMN `TypeEvenement` enum(''Sport'',''Music'',''Art'',''Cinema'',''Formation'',''Conference'',''Spectacle'',''Festival'',''Autres'')
        NOT NULL DEFAULT ''Autres''
        AFTER `InventoryMode`',
    'SELECT ''Colonne TypeEvenement déjà présente — ignoré'' AS Info'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db
      AND TABLE_NAME = 'EvenementSessions'
      AND COLUMN_NAME = 'Description'
);

SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE `EvenementSessions`
        ADD COLUMN `Description` varchar(2000) NULL
        AFTER `Libelle`',
    'SELECT ''Colonne Description déjà présente — ignoré'' AS Info'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db
      AND TABLE_NAME = 'EvenementSessions'
      AND COLUMN_NAME = 'Ville'
);

SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE `EvenementSessions`
        ADD COLUMN `Ville` varchar(100) NULL
        AFTER `MailOrganisateur`',
    'SELECT ''Colonne Ville déjà présente — ignoré'' AS Info'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db
      AND TABLE_NAME = 'EvenementSessions'
      AND COLUMN_NAME = 'Commune'
);

SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE `EvenementSessions`
        ADD COLUMN `Commune` varchar(100) NULL
        AFTER `Ville`',
    'SELECT ''Colonne Commune déjà présente — ignoré'' AS Info'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db
      AND TABLE_NAME = 'EvenementSessions'
      AND COLUMN_NAME = 'Quartier'
);

SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE `EvenementSessions`
        ADD COLUMN `Quartier` varchar(100) NULL
        AFTER `Commune`',
    'SELECT ''Colonne Quartier déjà présente — ignoré'' AS Info'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db
      AND TABLE_NAME = 'EvenementSessions'
      AND COLUMN_NAME = 'Avenue'
);

SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE `EvenementSessions`
        ADD COLUMN `Avenue` varchar(200) NULL
        AFTER `Quartier`',
    'SELECT ''Colonne Avenue déjà présente — ignoré'' AS Info'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db
      AND TABLE_NAME = 'EvenementSessions'
      AND COLUMN_NAME = 'Numero'
);

SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE `EvenementSessions`
        ADD COLUMN `Numero` varchar(50) NULL
        AFTER `Avenue`',
    'SELECT ''Colonne Numero déjà présente — ignoré'' AS Info'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db
      AND TABLE_NAME = 'EvenementSessions'
      AND COLUMN_NAME = 'NomOrganisateur'
);

SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE `EvenementSessions`
        ADD COLUMN `NomOrganisateur` varchar(255) NULL
        AFTER `TypeEvenement`',
    'SELECT ''Colonne NomOrganisateur déjà présente — ignoré'' AS Info'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db
      AND TABLE_NAME = 'EvenementSessions'
      AND COLUMN_NAME = 'TelephoneOrganisateur'
);

SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE `EvenementSessions`
        ADD COLUMN `TelephoneOrganisateur` varchar(50) NULL
        AFTER `NomOrganisateur`',
    'SELECT ''Colonne TelephoneOrganisateur déjà présente — ignoré'' AS Info'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db
      AND TABLE_NAME = 'EvenementSessions'
      AND COLUMN_NAME = 'MailOrganisateur'
);

SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE `EvenementSessions`
        ADD COLUMN `MailOrganisateur` varchar(255) NULL
        AFTER `TelephoneOrganisateur`',
    'SELECT ''Colonne MailOrganisateur déjà présente — ignoré'' AS Info'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
