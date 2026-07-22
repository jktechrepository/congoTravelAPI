-- =============================================================================
-- Triggers de cohérence EvenementReservationLines (complément migrations EF)
-- À exécuter APRÈS dotnet ef database update (tables Evenement* créées)
-- =============================================================================

DELIMITER $$

DROP TRIGGER IF EXISTS `TRG_EvenementReservationLines_BI`$$
CREATE TRIGGER `TRG_EvenementReservationLines_BI`
BEFORE INSERT ON `EvenementReservationLines`
FOR EACH ROW
BEGIN
    IF NEW.`LineType` = 'Seat' THEN
        IF NEW.`Quantite` <> 1 OR NEW.`IdEvenementSessionSeat` IS NULL OR NEW.`IdEvenementSessionClassQuota` IS NOT NULL THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'LineType Seat invalide: Quantite=1, Seat obligatoire, ClassQuota null.';
        END IF;
    ELSEIF NEW.`LineType` = 'ClassQuota' THEN
        IF NEW.`IdEvenementSessionClassQuota` IS NULL OR NEW.`IdEvenementSessionSeat` IS NOT NULL THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'LineType ClassQuota invalide: ClassQuota obligatoire, Seat null.';
        END IF;
    ELSEIF NEW.`LineType` = 'GlobalQuota' THEN
        IF NEW.`IdEvenementSessionSeat` IS NOT NULL OR NEW.`IdEvenementSessionClassQuota` IS NOT NULL THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'LineType GlobalQuota invalide: Seat et ClassQuota doivent etre null.';
        END IF;
    END IF;
END$$

DROP TRIGGER IF EXISTS `TRG_EvenementReservationLines_BU`$$
CREATE TRIGGER `TRG_EvenementReservationLines_BU`
BEFORE UPDATE ON `EvenementReservationLines`
FOR EACH ROW
BEGIN
    IF NEW.`LineType` = 'Seat' THEN
        IF NEW.`Quantite` <> 1 OR NEW.`IdEvenementSessionSeat` IS NULL OR NEW.`IdEvenementSessionClassQuota` IS NOT NULL THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'LineType Seat invalide: Quantite=1, Seat obligatoire, ClassQuota null.';
        END IF;
    ELSEIF NEW.`LineType` = 'ClassQuota' THEN
        IF NEW.`IdEvenementSessionClassQuota` IS NULL OR NEW.`IdEvenementSessionSeat` IS NOT NULL THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'LineType ClassQuota invalide: ClassQuota obligatoire, Seat null.';
        END IF;
    ELSEIF NEW.`LineType` = 'GlobalQuota' THEN
        IF NEW.`IdEvenementSessionSeat` IS NOT NULL OR NEW.`IdEvenementSessionClassQuota` IS NOT NULL THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'LineType GlobalQuota invalide: Seat et ClassQuota doivent etre null.';
        END IF;
    END IF;
END$$

DELIMITER ;
