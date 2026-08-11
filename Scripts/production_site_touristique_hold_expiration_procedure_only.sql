-- =============================================================================
-- Procédure d'expiration HOLD Site Touristique (sans event scheduler)
-- Hosted service .NET : CALL sp_ExpireSiteTouristiqueHolds()
-- =============================================================================

DELIMITER $$

DROP PROCEDURE IF EXISTS `sp_ExpireSiteTouristiqueHolds`$$
CREATE PROCEDURE `sp_ExpireSiteTouristiqueHolds`()
BEGIN
    DECLARE v_done INT DEFAULT 0;
    DECLARE v_IdSiteTouristiqueReservation INT;

    DECLARE cur CURSOR FOR
        SELECT r.`IdSiteTouristiqueReservation`
        FROM `SiteTouristiqueReservations` r
        WHERE r.`Status` = 'HOLD'
          AND r.`ExpiresAtUtc` IS NOT NULL
          AND r.`ExpiresAtUtc` < UTC_TIMESTAMP(6)
        ORDER BY r.`ExpiresAtUtc`
        FOR UPDATE;

    DECLARE CONTINUE HANDLER FOR NOT FOUND SET v_done = 1;

    START TRANSACTION;

    OPEN cur;
    read_loop: LOOP
        FETCH cur INTO v_IdSiteTouristiqueReservation;
        IF v_done = 1 THEN
            LEAVE read_loop;
        END IF;

        UPDATE `SiteTouristiqueGlobalQuotas` g
        JOIN `SiteTouristiqueReservations` r ON r.`IdSiteTouristiqueJournee` = g.`IdSiteTouristiqueJournee`
        JOIN `SiteTouristiqueReservationLines` rl ON rl.`IdSiteTouristiqueReservation` = r.`IdSiteTouristiqueReservation`
        SET g.`QuantiteHold` = GREATEST(0, g.`QuantiteHold` - rl.`Quantite`)
        WHERE r.`IdSiteTouristiqueReservation` = v_IdSiteTouristiqueReservation
          AND rl.`LineType` = 'GlobalQuota';

        UPDATE `SiteTouristiqueClassQuotas` q
        JOIN `SiteTouristiqueReservationLines` rl ON rl.`IdSiteTouristiqueClassQuota` = q.`IdSiteTouristiqueClassQuota`
        SET q.`QuantiteHold` = GREATEST(0, q.`QuantiteHold` - rl.`Quantite`)
        WHERE rl.`IdSiteTouristiqueReservation` = v_IdSiteTouristiqueReservation
          AND rl.`LineType` = 'ClassQuota';

        UPDATE `SiteTouristiqueReservations`
        SET `Status` = 'EXPIRED',
            `DateModification` = UTC_TIMESTAMP(6)
        WHERE `IdSiteTouristiqueReservation` = v_IdSiteTouristiqueReservation
          AND `Status` = 'HOLD';
    END LOOP;
    CLOSE cur;

    COMMIT;
END$$

DELIMITER ;
