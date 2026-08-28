-- =============================================================================
-- CongoTravel — Hôtel Phase 7e Extras (MySQL, idempotent)
-- =============================================================================
-- Prérequis : production_hotel_phase3_reservations.sql (+ hôtels)
-- Catalogue extras (petit-déj, parking…) + lignes sur réservation CONFIRMED
-- =============================================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS `HotelExtras` (
    `IdHotelExtra` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `IdHotel` int NOT NULL,
    `Code` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `Libelle` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
    `PrixUnitaire` decimal(18,2) NOT NULL,
    `CodeDevise` char(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF',
    `PricingUnit` enum('PerStay','PerNight') CHARACTER SET utf8mb4 NOT NULL DEFAULT 'PerStay',
    `IsActif` tinyint(1) NOT NULL DEFAULT 1,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_HotelExtras` PRIMARY KEY (`IdHotelExtra`),
    CONSTRAINT `FK_HotelExtras_Societes` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
    CONSTRAINT `FK_HotelExtras_Hotels` FOREIGN KEY (`IdHotel`) REFERENCES `Hotels` (`IdHotel`) ON DELETE RESTRICT,
    UNIQUE KEY `IX_HotelExtras_Hotel_Code_UQ` (`IdHotel`, `Code`),
    KEY `IX_HotelExtras_IdSociete` (`IdSociete`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `HotelReservationExtras` (
    `IdHotelReservationExtra` int NOT NULL AUTO_INCREMENT,
    `IdHotelReservation` int NOT NULL,
    `IdHotelExtra` int NOT NULL,
    `Quantity` int NOT NULL,
    `PrixUnitaireSnapshot` decimal(18,2) NOT NULL,
    `MontantLigne` decimal(18,2) NOT NULL,
    `CodeDevise` char(3) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_HotelReservationExtras` PRIMARY KEY (`IdHotelReservationExtra`),
    CONSTRAINT `FK_HotelReservationExtras_Reservations`
        FOREIGN KEY (`IdHotelReservation`) REFERENCES `HotelReservations` (`IdHotelReservation`) ON DELETE CASCADE,
    CONSTRAINT `FK_HotelReservationExtras_Extras`
        FOREIGN KEY (`IdHotelExtra`) REFERENCES `HotelExtras` (`IdHotelExtra`) ON DELETE RESTRICT,
    CONSTRAINT `CK_HotelReservationExtras_Quantity` CHECK (`Quantity` > 0),
    UNIQUE KEY `IX_HotelReservationExtras_Reservation_Extra_UQ` (`IdHotelReservation`, `IdHotelExtra`),
    KEY `IX_HotelReservationExtras_IdHotelExtra` (`IdHotelExtra`)
) CHARACTER SET=utf8mb4;

SET FOREIGN_KEY_CHECKS = 1;

SELECT 'production_hotel_phase7e_extras.sql appliqué' AS Info;
