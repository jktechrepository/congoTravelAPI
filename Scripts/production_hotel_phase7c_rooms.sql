-- =============================================================================
-- CongoTravel — Hôtel Phase 7c HotelRoom (MySQL, idempotent)
-- =============================================================================
-- Prérequis : production_hotel_phase3_reservations.sql (+ room types / hôtels)
-- Catalogue chambres physiques + attributions post-confirm (pas SeatNumbered inventaire)
-- =============================================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS `HotelRooms` (
    `IdHotelRoom` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `IdHotel` int NOT NULL,
    `IdHotelRoomType` int NOT NULL,
    `Numero` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    `Etage` varchar(32) CHARACTER SET utf8mb4 NULL,
    `Libelle` varchar(120) CHARACTER SET utf8mb4 NULL,
    `IsActif` tinyint(1) NOT NULL DEFAULT 1,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_HotelRooms` PRIMARY KEY (`IdHotelRoom`),
    CONSTRAINT `FK_HotelRooms_Societes` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
    CONSTRAINT `FK_HotelRooms_Hotels` FOREIGN KEY (`IdHotel`) REFERENCES `Hotels` (`IdHotel`) ON DELETE RESTRICT,
    CONSTRAINT `FK_HotelRooms_RoomTypes` FOREIGN KEY (`IdHotelRoomType`) REFERENCES `HotelRoomTypes` (`IdHotelRoomType`) ON DELETE RESTRICT,
    UNIQUE KEY `IX_HotelRooms_Hotel_Numero_UQ` (`IdHotel`, `Numero`),
    KEY `IX_HotelRooms_IdSociete` (`IdSociete`),
    KEY `IX_HotelRooms_IdHotelRoomType` (`IdHotelRoomType`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `HotelRoomAssignments` (
    `IdHotelRoomAssignment` int NOT NULL AUTO_INCREMENT,
    `IdHotelReservation` int NOT NULL,
    `IdHotelReservationLine` int NOT NULL,
    `IdHotelRoom` int NOT NULL,
    `DateAttributionUtc` datetime(6) NOT NULL,
    CONSTRAINT `PK_HotelRoomAssignments` PRIMARY KEY (`IdHotelRoomAssignment`),
    CONSTRAINT `FK_HotelRoomAssignments_Reservations`
        FOREIGN KEY (`IdHotelReservation`) REFERENCES `HotelReservations` (`IdHotelReservation`) ON DELETE CASCADE,
    CONSTRAINT `FK_HotelRoomAssignments_Lines`
        FOREIGN KEY (`IdHotelReservationLine`) REFERENCES `HotelReservationLines` (`IdHotelReservationLine`) ON DELETE RESTRICT,
    CONSTRAINT `FK_HotelRoomAssignments_Rooms`
        FOREIGN KEY (`IdHotelRoom`) REFERENCES `HotelRooms` (`IdHotelRoom`) ON DELETE RESTRICT,
    UNIQUE KEY `IX_HotelRoomAssignments_Room_Reservation_UQ` (`IdHotelRoom`, `IdHotelReservation`),
    KEY `IX_HotelRoomAssignments_IdHotelRoom` (`IdHotelRoom`),
    KEY `IX_HotelRoomAssignments_IdReservation` (`IdHotelReservation`),
    KEY `IX_HotelRoomAssignments_IdLine` (`IdHotelReservationLine`)
) CHARACTER SET=utf8mb4;

SET FOREIGN_KEY_CHECKS = 1;

SELECT 'production_hotel_phase7c_rooms.sql appliqué' AS Info;
