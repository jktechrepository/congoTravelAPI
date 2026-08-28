-- Hôtel Phase 3 — expiration atomique des holds multi-nuit
DROP PROCEDURE IF EXISTS `sp_ExpireHotelHolds`;
DELIMITER $$
CREATE PROCEDURE `sp_ExpireHotelHolds`()
BEGIN
  START TRANSACTION;
  DROP TEMPORARY TABLE IF EXISTS `_ExpiredHotelHolds`;
  CREATE TEMPORARY TABLE `_ExpiredHotelHolds` (
    `IdHotelReservation` int NOT NULL PRIMARY KEY
  ) ENGINE=MEMORY;

  INSERT INTO `_ExpiredHotelHolds` (`IdHotelReservation`)
  SELECT `IdHotelReservation`
  FROM `HotelReservations`
  WHERE `Status` = 'HOLD'
    AND `ExpiresAtUtc` IS NOT NULL
    AND `ExpiresAtUtc` < UTC_TIMESTAMP(6)
  FOR UPDATE;

  UPDATE `HotelNightAllotments` a
  JOIN `HotelReservations` r ON r.`IdHotel` = a.`IdHotel`
  JOIN `_ExpiredHotelHolds` e ON e.`IdHotelReservation` = r.`IdHotelReservation`
  JOIN `HotelReservationLines` l
    ON l.`IdHotelReservation` = r.`IdHotelReservation`
   AND l.`IdHotelRoomType` = a.`IdHotelRoomType`
  SET a.`QuantiteHold` = GREATEST(0, a.`QuantiteHold` - l.`Quantity`),
      a.`DateModification` = UTC_TIMESTAMP(6)
  WHERE a.`NightDate` >= r.`CheckInDate`
    AND a.`NightDate` < r.`CheckOutDate`;

  UPDATE `HotelReservations` r
  JOIN `_ExpiredHotelHolds` e ON e.`IdHotelReservation` = r.`IdHotelReservation`
  SET r.`Status` = 'EXPIRED', r.`ExpiresAtUtc` = NULL,
      r.`DateModification` = UTC_TIMESTAMP(6)
  WHERE r.`Status` = 'HOLD';

  DROP TEMPORARY TABLE `_ExpiredHotelHolds`;
  COMMIT;
END$$
DELIMITER ;
