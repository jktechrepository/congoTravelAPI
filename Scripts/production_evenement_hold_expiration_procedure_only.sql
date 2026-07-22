-- =============================================================================
-- Procédure d'expiration HOLD (sans event scheduler MariaDB)
-- Le hosted service .NET appelle CALL sp_ExpireEvenementHolds() toutes les minutes.
-- Pour event scheduler DB : utiliser production_evenement_hold_expiration_job.sql
-- =============================================================================

DELIMITER $$

DROP PROCEDURE IF EXISTS `sp_ExpireEvenementHolds`$$
CREATE PROCEDURE `sp_ExpireEvenementHolds`()
BEGIN
    DECLARE v_done INT DEFAULT 0;
    DECLARE v_IdEvenementReservation INT;

    DECLARE cur CURSOR FOR
        SELECT r.`IdEvenementReservation`
        FROM `EvenementReservations` r
        WHERE r.`Status` = 'HOLD'
          AND r.`ExpiresAtUtc` IS NOT NULL
          AND r.`ExpiresAtUtc` < UTC_TIMESTAMP(6)
        ORDER BY r.`ExpiresAtUtc`
        FOR UPDATE;

    DECLARE CONTINUE HANDLER FOR NOT FOUND SET v_done = 1;

    START TRANSACTION;

    OPEN cur;
    read_loop: LOOP
        FETCH cur INTO v_IdEvenementReservation;
        IF v_done = 1 THEN
            LEAVE read_loop;
        END IF;

        UPDATE `EvenementSessionGlobalQuotas` g
        JOIN `EvenementReservations` r ON r.`IdEvenementSession` = g.`IdEvenementSession`
        JOIN `EvenementReservationLines` rl ON rl.`IdEvenementReservation` = r.`IdEvenementReservation`
        SET g.`QuantiteHold` = GREATEST(0, g.`QuantiteHold` - rl.`Quantite`)
        WHERE r.`IdEvenementReservation` = v_IdEvenementReservation
          AND rl.`LineType` = 'GlobalQuota';

        UPDATE `EvenementSessionClassQuotas` q
        JOIN `EvenementReservationLines` rl ON rl.`IdEvenementSessionClassQuota` = q.`IdEvenementSessionClassQuota`
        SET q.`QuantiteHold` = GREATEST(0, q.`QuantiteHold` - rl.`Quantite`)
        WHERE rl.`IdEvenementReservation` = v_IdEvenementReservation
          AND rl.`LineType` = 'ClassQuota';

        UPDATE `EvenementSessionSeats` s
        JOIN `EvenementReservationLines` rl ON rl.`IdEvenementSessionSeat` = s.`IdEvenementSessionSeat`
        SET s.`SeatStatus` = 'Available',
            s.`IdEvenementReservationCourante` = NULL,
            s.`HoldExpireAtUtc` = NULL
        WHERE rl.`IdEvenementReservation` = v_IdEvenementReservation
          AND rl.`LineType` = 'Seat'
          AND s.`SeatStatus` = 'Held';

        UPDATE `EvenementReservations`
        SET `Status` = 'EXPIRED',
            `DateModification` = UTC_TIMESTAMP(6)
        WHERE `IdEvenementReservation` = v_IdEvenementReservation
          AND `Status` = 'HOLD';
    END LOOP;
    CLOSE cur;

    COMMIT;
END$$

DELIMITER ;
