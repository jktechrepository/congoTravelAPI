-- =============================================================================
-- Verification d'alignement API <-> DB
-- Scope: /api/events/sessions/{id}/holds payload par mode (A/B/C)
-- =============================================================================

SET @DbName = DATABASE();

-- -----------------------------------------------------------------------------
-- 0) Migrations EF Evenement
-- -----------------------------------------------------------------------------
SELECT
    h.`MigrationId`,
    h.`ProductVersion`,
    'OK' AS `Status`
FROM `__EFMigrationsHistory` h
WHERE h.`MigrationId` IN (
    '20260703101713_EvenementTicketingV1',
    '20260703120104_EvenementSessionGlobalQuotaPricing'
)
ORDER BY h.`MigrationId`;

SELECT
    'Tables Evenement manquantes' AS `Check`,
    GROUP_CONCAT(t.expected ORDER BY t.expected) AS `Manquantes`,
    CASE WHEN COUNT(*) = 0 THEN 'OK' ELSE 'KO' END AS `Status`
FROM (
    SELECT 'EvenementClasses' AS expected UNION ALL
    SELECT 'EvenementSessions' UNION ALL
    SELECT 'EvenementSessionSections' UNION ALL
    SELECT 'EvenementSessionGlobalQuotas' UNION ALL
    SELECT 'EvenementSessionClassQuotas' UNION ALL
    SELECT 'EvenementSessionSeats' UNION ALL
    SELECT 'EvenementReservations' UNION ALL
    SELECT 'EvenementReservationLines' UNION ALL
    SELECT 'EvenementTickets' UNION ALL
    SELECT 'EvenementPayments'
) t
WHERE NOT EXISTS (
    SELECT 1 FROM `INFORMATION_SCHEMA`.`TABLES` x
    WHERE x.`TABLE_SCHEMA` = @DbName AND x.`TABLE_NAME` = t.expected
);

-- -----------------------------------------------------------------------------
-- 1) Colonnes requises pour ReservationLine
-- -----------------------------------------------------------------------------
SELECT
    c.`TABLE_NAME`,
    c.`COLUMN_NAME`,
    c.`IS_NULLABLE`,
    c.`COLUMN_TYPE`
FROM `INFORMATION_SCHEMA`.`COLUMNS` c
WHERE c.`TABLE_SCHEMA` = @DbName
  AND c.`TABLE_NAME` = 'EvenementReservationLines'
  AND c.`COLUMN_NAME` IN ('LineType','Quantite','IdEvenementSessionSeat','IdEvenementSessionClassQuota')
ORDER BY c.`COLUMN_NAME`;

-- -----------------------------------------------------------------------------
-- 2) Mapping attendu LineType -> colonnes
--    A Seat        => Quantite=1, seat non null, classQuota null
--    B ClassQuota  => classQuota non null, seat null
--    C GlobalQuota => seat null et classQuota null
-- -----------------------------------------------------------------------------
SELECT
  'Seat' AS `LineType`,
  COUNT(*) AS `RowsInvalides`
FROM `EvenementReservationLines`
WHERE `LineType` = 'Seat'
  AND (`Quantite` <> 1 OR `IdEvenementSessionSeat` IS NULL OR `IdEvenementSessionClassQuota` IS NOT NULL)
UNION ALL
SELECT
  'ClassQuota' AS `LineType`,
  COUNT(*) AS `RowsInvalides`
FROM `EvenementReservationLines`
WHERE `LineType` = 'ClassQuota'
  AND (`IdEvenementSessionClassQuota` IS NULL OR `IdEvenementSessionSeat` IS NOT NULL)
UNION ALL
SELECT
  'GlobalQuota' AS `LineType`,
  COUNT(*) AS `RowsInvalides`
FROM `EvenementReservationLines`
WHERE `LineType` = 'GlobalQuota'
  AND (`IdEvenementSessionSeat` IS NOT NULL OR `IdEvenementSessionClassQuota` IS NOT NULL);

-- -----------------------------------------------------------------------------
-- 3) Index critiques de lookup/API
-- -----------------------------------------------------------------------------
SELECT
    s.`TABLE_NAME`,
    s.`INDEX_NAME`,
    GROUP_CONCAT(s.`COLUMN_NAME` ORDER BY s.`SEQ_IN_INDEX`) AS `IndexColumns`
FROM `INFORMATION_SCHEMA`.`STATISTICS` s
WHERE s.`TABLE_SCHEMA` = @DbName
  AND (
      (s.`TABLE_NAME` = 'EvenementReservations' AND s.`INDEX_NAME` IN ('IX_EvenementReservations_Status_ExpiresAtUtc', 'IX_EvenementReservations_Session_Status'))
      OR (s.`TABLE_NAME` = 'EvenementSessionSeats' AND s.`INDEX_NAME` IN ('IX_EvenementSessionSeats_Session_SeatStatus'))
      OR (s.`TABLE_NAME` = 'EvenementSessionClassQuotas' AND s.`INDEX_NAME` IN ('IX_EvenementSessionClassQuotas_Session_Classe_UQ'))
  )
GROUP BY s.`TABLE_NAME`, s.`INDEX_NAME`
ORDER BY s.`TABLE_NAME`, s.`INDEX_NAME`;

-- -----------------------------------------------------------------------------
-- 4) Contrat mode session -> table d'inventaire attendue
-- -----------------------------------------------------------------------------
SELECT
    es.`IdEvenementSession`,
    es.`CodeSession`,
    es.`InventoryMode`,
    CASE
        WHEN es.`InventoryMode` = 'SeatNumbered' AND EXISTS (
            SELECT 1 FROM `EvenementSessionSeats` ss WHERE ss.`IdEvenementSession` = es.`IdEvenementSession`
        ) THEN 'OK'
        WHEN es.`InventoryMode` = 'ClassQuota' AND EXISTS (
            SELECT 1 FROM `EvenementSessionClassQuotas` cq WHERE cq.`IdEvenementSession` = es.`IdEvenementSession`
        ) THEN 'OK'
        WHEN es.`InventoryMode` = 'GlobalQuota' AND EXISTS (
            SELECT 1 FROM `EvenementSessionGlobalQuotas` gq WHERE gq.`IdEvenementSession` = es.`IdEvenementSession`
        ) THEN 'OK'
        ELSE 'INCOHERENT'
    END AS `ContratInventaire`
FROM `EvenementSessions` es
ORDER BY es.`IdEvenementSession` DESC
LIMIT 100;

