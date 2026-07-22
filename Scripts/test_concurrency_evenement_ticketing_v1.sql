-- =============================================================================
-- Tests concurrence anti-survente - Evenement Ticketing V1
-- Execution manuelle dans 2 connexions SQL distinctes:
--   Connexion 1 => Session A
--   Connexion 2 => Session B
-- =============================================================================

-- -----------------------------------------------------------------------------
-- PREPARATION DONNEES
-- -----------------------------------------------------------------------------
SET @IdSociete = (SELECT `IdSociete` FROM `Societes` ORDER BY `IdSociete` LIMIT 1);

INSERT INTO `EvenementSessions`
(`IdSociete`,`CodeSession`,`Libelle`,`StartAtUtc`,`InventoryMode`,`Status`,`DateCreation`)
VALUES
(@IdSociete,'TEST-GLOBAL','Test Global Quota',UTC_TIMESTAMP(6),'GlobalQuota','Published',UTC_TIMESTAMP(6)),
(@IdSociete,'TEST-CLASS','Test Class Quota',UTC_TIMESTAMP(6),'ClassQuota','Published',UTC_TIMESTAMP(6)),
(@IdSociete,'TEST-SEAT','Test SeatNumbered',UTC_TIMESTAMP(6),'SeatNumbered','Published',UTC_TIMESTAMP(6));

SET @SessionGlobal = (SELECT `IdEvenementSession` FROM `EvenementSessions` WHERE `CodeSession`='TEST-GLOBAL' ORDER BY `IdEvenementSession` DESC LIMIT 1);
SET @SessionClass  = (SELECT `IdEvenementSession` FROM `EvenementSessions` WHERE `CodeSession`='TEST-CLASS'  ORDER BY `IdEvenementSession` DESC LIMIT 1);
SET @SessionSeat   = (SELECT `IdEvenementSession` FROM `EvenementSessions` WHERE `CodeSession`='TEST-SEAT'   ORDER BY `IdEvenementSession` DESC LIMIT 1);

INSERT INTO `EvenementSessionGlobalQuotas` (`IdEvenementSession`,`CapaciteTotale`,`QuantiteHold`,`QuantiteVendue`)
VALUES (@SessionGlobal, 10, 0, 0);

INSERT INTO `EvenementClasses`
(`IdSociete`,`CodeClasse`,`Libelle`,`Statut`,`DateCreation`)
VALUES (@IdSociete,'VIP-TST','VIP test',1,UTC_TIMESTAMP(6));
SET @ClasseVip = (SELECT `IdEvenementClasse` FROM `EvenementClasses` WHERE `CodeClasse`='VIP-TST' ORDER BY `IdEvenementClasse` DESC LIMIT 1);

INSERT INTO `EvenementSessionClassQuotas`
(`IdEvenementSession`,`IdEvenementClasse`,`CapaciteTotale`,`QuantiteHold`,`QuantiteVendue`,`PrixUnitaire`,`CodeDevise`)
VALUES (@SessionClass,@ClasseVip,5,0,0,50.00,'USD');
SET @ClassQuota = (SELECT `IdEvenementSessionClassQuota` FROM `EvenementSessionClassQuotas` WHERE `IdEvenementSession`=@SessionClass AND `IdEvenementClasse`=@ClasseVip);

INSERT INTO `EvenementSessionSeats`
(`IdEvenementSession`,`SeatCode`,`SeatStatus`,`PrixUnitaire`,`CodeDevise`)
VALUES
(@SessionSeat,'A-01','Available',20.00,'USD'),
(@SessionSeat,'A-02','Available',20.00,'USD');
SET @SeatA01 = (SELECT `IdEvenementSessionSeat` FROM `EvenementSessionSeats` WHERE `IdEvenementSession`=@SessionSeat AND `SeatCode`='A-01');

-- -----------------------------------------------------------------------------
-- TEST 1 : GLOBAL QUOTA (A et B tentent qty 7 sur capacite 10)
-- -----------------------------------------------------------------------------
-- Session A:
-- START TRANSACTION;
-- UPDATE `EvenementSessionGlobalQuotas`
--   SET `QuantiteHold` = `QuantiteHold` + 7
-- WHERE `IdEvenementSession` = @SessionGlobal
--   AND (`QuantiteHold` + `QuantiteVendue` + 7) <= `CapaciteTotale`;
-- SELECT ROW_COUNT() AS A_RowCount; -- attendu 1
-- COMMIT;

-- Session B (en meme temps):
-- START TRANSACTION;
-- UPDATE `EvenementSessionGlobalQuotas`
--   SET `QuantiteHold` = `QuantiteHold` + 7
-- WHERE `IdEvenementSession` = @SessionGlobal
--   AND (`QuantiteHold` + `QuantiteVendue` + 7) <= `CapaciteTotale`;
-- SELECT ROW_COUNT() AS B_RowCount; -- attendu 0 (stock insuffisant)
-- ROLLBACK;

-- -----------------------------------------------------------------------------
-- TEST 2 : CLASS QUOTA (A qty=4, B qty=3 sur capacite 5)
-- -----------------------------------------------------------------------------
-- Session A:
-- START TRANSACTION;
-- UPDATE `EvenementSessionClassQuotas`
--   SET `QuantiteHold` = `QuantiteHold` + 4
-- WHERE `IdEvenementSessionClassQuota` = @ClassQuota
--   AND (`QuantiteHold` + `QuantiteVendue` + 4) <= `CapaciteTotale`;
-- SELECT ROW_COUNT() AS A_RowCount; -- attendu 1
-- COMMIT;

-- Session B:
-- START TRANSACTION;
-- UPDATE `EvenementSessionClassQuotas`
--   SET `QuantiteHold` = `QuantiteHold` + 3
-- WHERE `IdEvenementSessionClassQuota` = @ClassQuota
--   AND (`QuantiteHold` + `QuantiteVendue` + 3) <= `CapaciteTotale`;
-- SELECT ROW_COUNT() AS B_RowCount; -- attendu 0
-- ROLLBACK;

-- -----------------------------------------------------------------------------
-- TEST 3 : SEAT MODE (A et B tentent le meme siege A-01)
-- -----------------------------------------------------------------------------
-- Session A:
-- START TRANSACTION;
-- UPDATE `EvenementSessionSeats`
--   SET `SeatStatus`='Held', `HoldExpireAtUtc` = UTC_TIMESTAMP(6) + INTERVAL 10 MINUTE
-- WHERE `IdEvenementSessionSeat` = @SeatA01
--   AND `SeatStatus`='Available';
-- SELECT ROW_COUNT() AS A_RowCount; -- attendu 1
-- COMMIT;

-- Session B:
-- START TRANSACTION;
-- UPDATE `EvenementSessionSeats`
--   SET `SeatStatus`='Held', `HoldExpireAtUtc` = UTC_TIMESTAMP(6) + INTERVAL 10 MINUTE
-- WHERE `IdEvenementSessionSeat` = @SeatA01
--   AND `SeatStatus`='Available';
-- SELECT ROW_COUNT() AS B_RowCount; -- attendu 0
-- ROLLBACK;

-- -----------------------------------------------------------------------------
-- CONTROLES FINAUX
-- -----------------------------------------------------------------------------
SELECT
  g.`IdEvenementSession`,
  g.`CapaciteTotale`,
  g.`QuantiteHold`,
  g.`QuantiteVendue`,
  (g.`CapaciteTotale` - g.`QuantiteHold` - g.`QuantiteVendue`) AS `ResteGlobal`
FROM `EvenementSessionGlobalQuotas` g
WHERE g.`IdEvenementSession` = @SessionGlobal;

SELECT
  q.`IdEvenementSessionClassQuota`,
  q.`CapaciteTotale`,
  q.`QuantiteHold`,
  q.`QuantiteVendue`,
  (q.`CapaciteTotale` - q.`QuantiteHold` - q.`QuantiteVendue`) AS `ResteClasse`
FROM `EvenementSessionClassQuotas` q
WHERE q.`IdEvenementSessionClassQuota` = @ClassQuota;

SELECT
  s.`IdEvenementSessionSeat`,
  s.`SeatCode`,
  s.`SeatStatus`,
  s.`HoldExpireAtUtc`
FROM `EvenementSessionSeats` s
WHERE s.`IdEvenementSession` = @SessionSeat
ORDER BY s.`SeatCode`;

