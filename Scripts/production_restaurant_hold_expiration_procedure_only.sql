-- =============================================================================
-- Procédure d'expiration HOLD Restaurant (sans event scheduler)
-- Hosted service .NET : CALL sp_ExpireRestaurantHolds()
-- Phase 4 : restitue aussi QuantiteHold ZoneQuota (lignes ClassQuota)
-- =============================================================================

DELIMITER $$

DROP PROCEDURE IF EXISTS `sp_ExpireRestaurantHolds`$$
CREATE PROCEDURE `sp_ExpireRestaurantHolds`()
BEGIN
    DECLARE v_done INT DEFAULT 0;
    DECLARE v_IdRestaurantReservation INT;

    DECLARE cur CURSOR FOR
        SELECT r.`IdRestaurantReservation`
        FROM `RestaurantReservations` r
        WHERE r.`Status` = 'HOLD'
          AND r.`ExpiresAtUtc` IS NOT NULL
          AND r.`ExpiresAtUtc` < UTC_TIMESTAMP(6)
        ORDER BY r.`ExpiresAtUtc`
        FOR UPDATE;

    DECLARE CONTINUE HANDLER FOR NOT FOUND SET v_done = 1;

    START TRANSACTION;

    OPEN cur;
    read_loop: LOOP
        FETCH cur INTO v_IdRestaurantReservation;
        IF v_done = 1 THEN
            LEAVE read_loop;
        END IF;

        UPDATE `RestaurantCreneauGlobalQuotas` g
        JOIN `RestaurantReservations` r ON r.`IdRestaurantCreneau` = g.`IdRestaurantCreneau`
        JOIN `RestaurantReservationLines` rl ON rl.`IdRestaurantReservation` = r.`IdRestaurantReservation`
        SET g.`QuantiteHold` = GREATEST(0, g.`QuantiteHold` - rl.`Quantite`)
        WHERE r.`IdRestaurantReservation` = v_IdRestaurantReservation
          AND rl.`LineType` = 'GlobalQuota';

        UPDATE `RestaurantCreneauZoneQuotas` q
        JOIN `RestaurantReservationLines` rl ON rl.`IdRestaurantCreneauZoneQuota` = q.`IdRestaurantCreneauZoneQuota`
        SET q.`QuantiteHold` = GREATEST(0, q.`QuantiteHold` - rl.`Quantite`)
        WHERE rl.`IdRestaurantReservation` = v_IdRestaurantReservation
          AND rl.`LineType` = 'ClassQuota';

        UPDATE `RestaurantReservations`
        SET `Status` = 'EXPIRED',
            `DateModification` = UTC_TIMESTAMP(6)
        WHERE `IdRestaurantReservation` = v_IdRestaurantReservation
          AND `Status` = 'HOLD';
    END LOOP;
    CLOSE cur;

    COMMIT;
END$$

DELIMITER ;
