-- =============================================================================
-- Production : table ConfigSocietes + backfill + retrait colonnes Voyages
-- Migration EF équivalente : 20260530094931_ConfigSocieteCentralizedRules
-- =============================================================================
-- Prérequis : exécuter d'abord Scripts/audit_configsociete_voyage_divergences.sql
-- =============================================================================

SET @db := DATABASE();

SET @table_exists := (
    SELECT COUNT(*)
    FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'ConfigSocietes'
);

SET @sql_create := IF(
    @table_exists = 0,
    'CREATE TABLE `ConfigSocietes` (
        `IdConfigSociete` int NOT NULL AUTO_INCREMENT,
        `IdSociete` int NOT NULL,
        `DureeValiditeBilletJours` int NOT NULL DEFAULT 0,
        `PenaliteReaffectationPourcentage` decimal(18,2) NOT NULL DEFAULT 0.00,
        `JoursAvanceMaxReservation` int NULL DEFAULT 60,
        `HeuresLimiteReaffectation` int NOT NULL DEFAULT 2,
        `HeuresOuvertureEmbarquementAvantDepart` int NOT NULL DEFAULT 3,
        `HeuresFermetureEmbarquementApresJourDepart` int NOT NULL DEFAULT 24,
        `DureeHoldFlexPayMinutes` int NOT NULL DEFAULT 15,
        `ReaffectationActive` tinyint(1) NOT NULL DEFAULT 1,
        `DateCreation` datetime(6) NOT NULL,
        `DateModification` datetime(6) NULL,
        PRIMARY KEY (`IdConfigSociete`),
        UNIQUE KEY `IX_ConfigSociete_IdSociete_Unique` (`IdSociete`),
        CONSTRAINT `FK_ConfigSocietes_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE CASCADE
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4',
    'SELECT ''Table ConfigSocietes déjà présente — ignoré'' AS Info'
);

PREPARE stmt FROM @sql_create;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

START TRANSACTION;

INSERT INTO ConfigSocietes (
    IdSociete,
    DureeValiditeBilletJours,
    PenaliteReaffectationPourcentage,
    JoursAvanceMaxReservation,
    HeuresLimiteReaffectation,
    HeuresOuvertureEmbarquementAvantDepart,
    HeuresFermetureEmbarquementApresJourDepart,
    DureeHoldFlexPayMinutes,
    ReaffectationActive,
    DateCreation
)
SELECT
    s.IdSociete,
    COALESCE(v.DureeValiditeBilletJours, 0),
    0,
    60,
    COALESCE(v.HeuresLimiteReaffectation, 2),
    3,
    24,
    15,
    1,
    UTC_TIMESTAMP(6)
FROM Societes s
LEFT JOIN (
    SELECT v1.IdSociete,
           v1.DureeValiditeBilletJours,
           v1.PenaliteReaffectation,
           v1.HeuresLimiteReaffectation
    FROM Voyages v1
    INNER JOIN (
        SELECT v2.IdSociete, MAX(v2.Id) AS IdVoyageRetenu
        FROM Voyages v2
        INNER JOIN (
            SELECT IdSociete, MAX(DateCreation) AS MaxDateCreation
            FROM Voyages
            GROUP BY IdSociete
        ) m ON m.IdSociete = v2.IdSociete AND v2.DateCreation = m.MaxDateCreation
        GROUP BY v2.IdSociete
    ) pick ON pick.IdVoyageRetenu = v1.Id
) v ON v.IdSociete = s.IdSociete
WHERE NOT EXISTS (SELECT 1 FROM ConfigSocietes c WHERE c.IdSociete = s.IdSociete);

SET @col_duree := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'Voyages' AND COLUMN_NAME = 'DureeValiditeBilletJours'
);

SET @sql_drop_duree := IF(
    @col_duree > 0,
    'ALTER TABLE `Voyages` DROP COLUMN `DureeValiditeBilletJours`',
    'SELECT ''Colonne DureeValiditeBilletJours déjà absente'' AS Info'
);
PREPARE stmt FROM @sql_drop_duree; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_heures := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'Voyages' AND COLUMN_NAME = 'HeuresLimiteReaffectation'
);
SET @sql_drop_heures := IF(
    @col_heures > 0,
    'ALTER TABLE `Voyages` DROP COLUMN `HeuresLimiteReaffectation`',
    'SELECT ''Colonne HeuresLimiteReaffectation déjà absente'' AS Info'
);
PREPARE stmt FROM @sql_drop_heures; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_penalite := (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = @db AND TABLE_NAME = 'Voyages' AND COLUMN_NAME = 'PenaliteReaffectation'
);
SET @sql_drop_penalite := IF(
    @col_penalite > 0,
    'ALTER TABLE `Voyages` DROP COLUMN `PenaliteReaffectation`',
    'SELECT ''Colonne PenaliteReaffectation déjà absente'' AS Info'
);
PREPARE stmt FROM @sql_drop_penalite; EXECUTE stmt; DEALLOCATE PREPARE stmt;

INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260530094931_ConfigSocieteCentralizedRules', '6.0.25');

COMMIT;

-- Contrôles
SELECT COUNT(*) AS societes_sans_config
FROM Societes s
WHERE NOT EXISTS (SELECT 1 FROM ConfigSocietes c WHERE c.IdSociete = s.IdSociete);

SELECT IdSociete, DureeValiditeBilletJours, PenaliteReaffectationPourcentage, HeuresLimiteReaffectation
FROM ConfigSocietes
ORDER BY IdSociete;

-- ROLLBACK manuel (hors script) :
-- ALTER TABLE Voyages ADD PenaliteReaffectation decimal(18,2) NOT NULL DEFAULT 0;
-- ALTER TABLE Voyages ADD HeuresLimiteReaffectation int NOT NULL DEFAULT 2;
-- ALTER TABLE Voyages ADD DureeValiditeBilletJours int NOT NULL DEFAULT 0;
-- UPDATE Voyages v INNER JOIN ConfigSocietes c ON c.IdSociete = v.IdSociete SET ...;
-- DROP TABLE ConfigSocietes;
