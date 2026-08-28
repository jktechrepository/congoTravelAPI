-- CongoTravel — Hôtel Phase 3 : réservations multi-nuit + acompte CASH
-- Prérequis : production_hotel_v1.sql, production_hotel_phase2_allotments.sql
SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS `HotelReservations` (
  `IdHotelReservation` int NOT NULL AUTO_INCREMENT,
  `IdSociete` int NOT NULL, `IdHotel` int NOT NULL, `IdSite` int NULL,
  `IdUtilisateur` int NULL, `IdClient` int NULL,
  `ReferenceReservation` varchar(64) NOT NULL, `CustomerRef` varchar(100) NULL,
  `CheckInDate` date NOT NULL, `CheckOutDate` date NOT NULL, `NombreNuits` int NOT NULL,
  `Status` enum('HOLD','CONFIRMED','CANCELLED','EXPIRED') NOT NULL DEFAULT 'HOLD',
  `ExpiresAtUtc` datetime(6) NULL,
  `MontantSejour` decimal(18,2) NOT NULL DEFAULT 0,
  `MontantSousTotal` decimal(18,2) NOT NULL DEFAULT 0,
  `CodeDevise` char(3) NOT NULL DEFAULT 'CDF',
  `IdempotencyKey` varchar(120) NULL,
  `DateCreation` datetime(6) NOT NULL, `DateModification` datetime(6) NULL,
  PRIMARY KEY (`IdHotelReservation`),
  UNIQUE KEY `IX_HotelReservations_Societe_Reference_UQ` (`IdSociete`,`ReferenceReservation`),
  UNIQUE KEY `IX_HotelReservations_Societe_Idempotency_UQ` (`IdSociete`,`IdempotencyKey`),
  KEY `IX_HotelReservations_Status_ExpiresAtUtc` (`Status`,`ExpiresAtUtc`),
  CONSTRAINT `FK_HotelReservations_Societes` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
  CONSTRAINT `FK_HotelReservations_Hotels` FOREIGN KEY (`IdHotel`) REFERENCES `Hotels` (`IdHotel`) ON DELETE RESTRICT,
  CONSTRAINT `FK_HotelReservations_Sites` FOREIGN KEY (`IdSite`) REFERENCES `Sites` (`IdSite`) ON DELETE RESTRICT,
  CONSTRAINT `FK_HotelReservations_Clients` FOREIGN KEY (`IdClient`) REFERENCES `Clients` (`IdClient`) ON DELETE RESTRICT,
  CONSTRAINT `CK_HotelReservations_Dates` CHECK (`CheckOutDate` > `CheckInDate`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `HotelReservationLines` (
  `IdHotelReservationLine` int NOT NULL AUTO_INCREMENT,
  `IdHotelReservation` int NOT NULL, `IdHotelRoomType` int NOT NULL,
  `Quantity` int NOT NULL, `PrixSejourUnitaire` decimal(18,2) NOT NULL,
  `MontantLigne` decimal(18,2) NOT NULL, `CodeDevise` char(3) NOT NULL DEFAULT 'CDF',
  PRIMARY KEY (`IdHotelReservationLine`),
  KEY `IX_HotelReservationLines_IdReservation` (`IdHotelReservation`),
  KEY `IX_HotelReservationLines_IdRoomType` (`IdHotelRoomType`),
  CONSTRAINT `FK_HotelReservationLines_Reservations` FOREIGN KEY (`IdHotelReservation`) REFERENCES `HotelReservations` (`IdHotelReservation`) ON DELETE CASCADE,
  CONSTRAINT `FK_HotelReservationLines_RoomTypes` FOREIGN KEY (`IdHotelRoomType`) REFERENCES `HotelRoomTypes` (`IdHotelRoomType`) ON DELETE RESTRICT,
  CONSTRAINT `CK_HotelReservationLines_Quantity` CHECK (`Quantity` > 0)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `HotelPayments` (
  `IdHotelPayment` int NOT NULL AUTO_INCREMENT, `IdHotelReservation` int NOT NULL,
  `IdSite` int NULL, `ReferencePaiement` varchar(100) NOT NULL,
  `Provider` varchar(40) NOT NULL, `ProviderTxRef` varchar(120) NULL,
  `Status` enum('PENDING','SUCCEEDED','FAILED','REFUNDED') NOT NULL,
  `Montant` decimal(18,2) NOT NULL, `CodeDevise` char(3) NOT NULL DEFAULT 'CDF',
  `MontantTarif` decimal(18,2) NOT NULL, `CodeDeviseTarif` char(3) NOT NULL DEFAULT 'CDF',
  `TauxVersDevisePaiement` decimal(18,8) NOT NULL DEFAULT 1,
  `IdempotencyKey` varchar(120) NULL, `DateCreation` datetime(6) NOT NULL,
  `DateModification` datetime(6) NULL,
  PRIMARY KEY (`IdHotelPayment`),
  UNIQUE KEY `IX_HotelPayments_Reference_UQ` (`ReferencePaiement`),
  UNIQUE KEY `IX_HotelPayments_Idempotency_UQ` (`IdempotencyKey`),
  KEY `IX_HotelPayments_Reservation_Status` (`IdHotelReservation`,`Status`),
  CONSTRAINT `FK_HotelPayments_Reservations` FOREIGN KEY (`IdHotelReservation`) REFERENCES `HotelReservations` (`IdHotelReservation`) ON DELETE RESTRICT,
  CONSTRAINT `FK_HotelPayments_Sites` FOREIGN KEY (`IdSite`) REFERENCES `Sites` (`IdSite`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

-- Installer ensuite production_hotel_hold_expiration_procedure_only.sql.
