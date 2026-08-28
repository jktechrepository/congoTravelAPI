-- CongoTravel — Hôtel Phase 2 allotments (MySQL, idempotent)
SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS `HotelNightAllotments` (
    `IdHotelNightAllotment` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `IdHotel` int NOT NULL,
    `IdHotelRoomType` int NOT NULL,
    `NightDate` date NOT NULL,
    `CapaciteTotale` int NOT NULL,
    `QuantiteHold` int NOT NULL DEFAULT 0,
    `QuantiteVendue` int NOT NULL DEFAULT 0,
    `PrixNuit` decimal(18,2) NOT NULL,
    `CodeDevise` char(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF',
    `Status` enum('Draft','Published','Closed','Cancelled') NOT NULL DEFAULT 'Draft',
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_HotelNightAllotments` PRIMARY KEY (`IdHotelNightAllotment`),
    CONSTRAINT `CK_HotelNightAllotments_Capacite` CHECK (`CapaciteTotale` >= 0),
    CONSTRAINT `CK_HotelNightAllotments_StockPositive` CHECK (`QuantiteHold` >= 0 AND `QuantiteVendue` >= 0),
    CONSTRAINT `CK_HotelNightAllotments_StockMax` CHECK (`QuantiteHold` + `QuantiteVendue` <= `CapaciteTotale`),
    CONSTRAINT `FK_HotelNightAllotments_Societes` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
    CONSTRAINT `FK_HotelNightAllotments_Hotels` FOREIGN KEY (`IdHotel`) REFERENCES `Hotels` (`IdHotel`) ON DELETE RESTRICT,
    CONSTRAINT `FK_HotelNightAllotments_RoomTypes` FOREIGN KEY (`IdHotelRoomType`) REFERENCES `HotelRoomTypes` (`IdHotelRoomType`) ON DELETE RESTRICT,
    UNIQUE KEY `IX_HotelNightAllotments_Hotel_RoomType_Night_UQ` (`IdHotel`, `IdHotelRoomType`, `NightDate`),
    KEY `IX_HotelNightAllotments_IdSociete` (`IdSociete`),
    KEY `IX_HotelNightAllotments_IdHotel_NightDate` (`IdHotel`, `NightDate`),
    KEY `IX_HotelNightAllotments_IdHotelRoomType` (`IdHotelRoomType`)
) CHARACTER SET=utf8mb4;

SET FOREIGN_KEY_CHECKS = 1;
SELECT 'production_hotel_phase2_allotments.sql appliqué' AS Info;
