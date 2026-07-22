-- =============================================================================
-- Pré-vol déploiement billetterie événement V1
-- Exécuter AVANT deploy_evenement_ticketing_production.sh ou le script SQL EF.
-- Toutes les lignes STATUS = OK => prêt pour le déploiement.
-- =============================================================================

SET @DbName = DATABASE();

SELECT 'DB courante' AS `Check`, @DbName AS `Valeur`,
       CASE WHEN @DbName IS NOT NULL AND @DbName <> '' THEN 'OK' ELSE 'KO' END AS `Status`;

SELECT 'Version serveur' AS `Check`, VERSION() AS `Valeur`, 'INFO' AS `Status`;

-- Migration transport précédente (point de départ EF)
SELECT 'Migration prereq ClientAdresseClientOptional' AS `Check`,
       COALESCE(
           (SELECT `MigrationId` FROM `__EFMigrationsHistory`
            WHERE `MigrationId` = '20260619134037_ClientAdresseClientOptional'),
           'ABSENTE'
       ) AS `Valeur`,
       CASE WHEN EXISTS (
           SELECT 1 FROM `__EFMigrationsHistory`
           WHERE `MigrationId` = '20260619134037_ClientAdresseClientOptional'
       ) THEN 'OK' ELSE 'WARN — appliquer les migrations transport en retard avant Evenement' END AS `Status`;

-- Déjà déployé ?
SELECT 'EvenementTicketingV1 déjà appliquée' AS `Check`,
       CASE WHEN EXISTS (
           SELECT 1 FROM `__EFMigrationsHistory`
           WHERE `MigrationId` = '20260703101713_EvenementTicketingV1'
       ) THEN 'OUI' ELSE 'NON' END AS `Valeur`,
       CASE WHEN EXISTS (
           SELECT 1 FROM `__EFMigrationsHistory`
           WHERE `MigrationId` = '20260703101713_EvenementTicketingV1'
       ) THEN 'SKIP ou idempotent' ELSE 'OK — à déployer' END AS `Status`;

SELECT 'EvenementSessionGlobalQuotaPricing déjà appliquée' AS `Check`,
       CASE WHEN EXISTS (
           SELECT 1 FROM `__EFMigrationsHistory`
           WHERE `MigrationId` = '20260703120104_EvenementSessionGlobalQuotaPricing'
       ) THEN 'OUI' ELSE 'NON' END AS `Valeur`,
       CASE WHEN EXISTS (
           SELECT 1 FROM `__EFMigrationsHistory`
           WHERE `MigrationId` = '20260703120104_EvenementSessionGlobalQuotaPricing'
       ) THEN 'SKIP ou idempotent' ELSE 'OK — à déployer' END AS `Status`;

-- Tables orphelines sans historique EF
SELECT 'Table EvenementSessions sans historique EF' AS `Check`,
       CASE WHEN EXISTS (
           SELECT 1 FROM `INFORMATION_SCHEMA`.`TABLES`
           WHERE `TABLE_SCHEMA` = @DbName AND `TABLE_NAME` = 'EvenementSessions'
       ) AND NOT EXISTS (
           SELECT 1 FROM `__EFMigrationsHistory`
           WHERE `MigrationId` = '20260703101713_EvenementTicketingV1'
       ) THEN 'INCOHERENT' ELSE 'OK' END AS `Valeur`,
       CASE WHEN EXISTS (
           SELECT 1 FROM `INFORMATION_SCHEMA`.`TABLES`
           WHERE `TABLE_SCHEMA` = @DbName AND `TABLE_NAME` = 'EvenementSessions'
       ) AND NOT EXISTS (
           SELECT 1 FROM `__EFMigrationsHistory`
           WHERE `MigrationId` = '20260703101713_EvenementTicketingV1'
       ) THEN 'KO — tables présentes mais migration non stampée' ELSE 'OK' END AS `Status`;

-- Colonne ConfigSociete
SELECT 'ConfigSocietes.DureeHoldEvenementMinutes' AS `Check`,
       CASE WHEN EXISTS (
           SELECT 1 FROM `INFORMATION_SCHEMA`.`COLUMNS`
           WHERE `TABLE_SCHEMA` = @DbName
             AND `TABLE_NAME` = 'ConfigSocietes'
             AND `COLUMN_NAME` = 'DureeHoldEvenementMinutes'
       ) THEN 'PRESENTE' ELSE 'ABSENTE' END AS `Valeur`,
       'INFO' AS `Status`;

-- Procédure expiration
SELECT 'sp_ExpireEvenementHolds' AS `Check`,
       CASE WHEN EXISTS (
           SELECT 1 FROM `INFORMATION_SCHEMA`.`ROUTINES`
           WHERE `ROUTINE_SCHEMA` = @DbName
             AND `ROUTINE_NAME` = 'sp_ExpireEvenementHolds'
       ) THEN 'PRESENTE' ELSE 'ABSENTE' END AS `Valeur`,
       'INFO' AS `Status`;

-- Triggers
SELECT 'TRG_EvenementReservationLines_BI' AS `Check`,
       CASE WHEN EXISTS (
           SELECT 1 FROM `INFORMATION_SCHEMA`.`TRIGGERS`
           WHERE `TRIGGER_SCHEMA` = @DbName
             AND `TRIGGER_NAME` = 'TRG_EvenementReservationLines_BI'
       ) THEN 'PRESENT' ELSE 'ABSENT' END AS `Valeur`,
       'INFO' AS `Status`;
