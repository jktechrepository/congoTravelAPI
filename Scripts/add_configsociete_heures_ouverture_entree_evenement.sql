-- Ajoute HeuresOuvertureEntreeEvenementAvantDebut sur ConfigSocietes (défaut 3).
-- Migration EF : AddConfigSocieteHeuresOuvertureEntreeEvenement

SET @db := DATABASE();

SET @col_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db
      AND TABLE_NAME = 'ConfigSocietes'
      AND COLUMN_NAME = 'HeuresOuvertureEntreeEvenementAvantDebut'
);

SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE `ConfigSocietes`
        ADD COLUMN `HeuresOuvertureEntreeEvenementAvantDebut` int NOT NULL DEFAULT 3
        AFTER `HeuresFermetureEmbarquementApresJourDepart`',
    'SELECT ''Colonne HeuresOuvertureEntreeEvenementAvantDebut déjà présente — ignoré'' AS Info'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
