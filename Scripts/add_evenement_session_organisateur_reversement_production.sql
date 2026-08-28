-- Reversement organisateur événement : MM bénéficiaire session + gates vente / reversement.
-- Migration EF : AddEvenementSessionOrganisateurReversement

SET @db := DATABASE();

SET @col_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db
      AND TABLE_NAME = 'EvenementSessions'
      AND COLUMN_NAME = 'NumeroMobileMoneyOrganisateur'
);

SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE `EvenementSessions`
        ADD COLUMN `NumeroMobileMoneyOrganisateur` varchar(30) NULL
        AFTER `TelephoneOrganisateur`',
    'SELECT ''Colonne NumeroMobileMoneyOrganisateur déjà présente — ignoré'' AS Info'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db
      AND TABLE_NAME = 'EvenementSessions'
      AND COLUMN_NAME = 'VenteEnLigneActive'
);

SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE `EvenementSessions`
        ADD COLUMN `VenteEnLigneActive` tinyint(1) NOT NULL DEFAULT 1
        AFTER `NumeroMobileMoneyOrganisateur`',
    'SELECT ''Colonne VenteEnLigneActive déjà présente — ignoré'' AS Info'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db
      AND TABLE_NAME = 'EvenementSessions'
      AND COLUMN_NAME = 'AutoReversementOrganisateur'
);

SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE `EvenementSessions`
        ADD COLUMN `AutoReversementOrganisateur` tinyint(1) NOT NULL DEFAULT 1
        AFTER `VenteEnLigneActive`',
    'SELECT ''Colonne AutoReversementOrganisateur déjà présente — ignoré'' AS Info'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Rétrocompatibilité : sessions existantes restent vendables et reversables.
UPDATE `EvenementSessions`
SET `VenteEnLigneActive` = 1
WHERE `VenteEnLigneActive` = 0;

UPDATE `EvenementSessions`
SET `AutoReversementOrganisateur` = 1
WHERE `AutoReversementOrganisateur` = 0;
