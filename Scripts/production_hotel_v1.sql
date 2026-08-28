-- CongoTravel — Hôtel V1 Phase 1 (MySQL, idempotent)
SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ConfigSocietes'
      AND COLUMN_NAME = 'DureeHoldHotelMinutes'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `ConfigSocietes` ADD `DureeHoldHotelMinutes` int NOT NULL DEFAULT 15',
    'SELECT ''ConfigSocietes.DureeHoldHotelMinutes déjà présent'' AS Info');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

CREATE TABLE IF NOT EXISTS `Hotels` (
    `IdHotel` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `IdSite` int NULL,
    `CodeHotel` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `Nom` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Description` varchar(2000) CHARACTER SET utf8mb4 NULL,
    `Adresse` varchar(500) CHARACTER SET utf8mb4 NULL,
    `AcomptePourcentDefaut` decimal(5,2) NOT NULL DEFAULT 0.00,
    `Status` enum('Draft','Published','Closed','Cancelled') NOT NULL DEFAULT 'Draft',
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_Hotels` PRIMARY KEY (`IdHotel`),
    CONSTRAINT `FK_Hotels_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Hotels_Sites_IdSite` FOREIGN KEY (`IdSite`) REFERENCES `Sites` (`IdSite`) ON DELETE RESTRICT,
    UNIQUE KEY `IX_Hotels_Societe_CodeHotel_UQ` (`IdSociete`, `CodeHotel`),
    KEY `IX_Hotels_IdSite` (`IdSite`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `HotelRoomTypes` (
    `IdHotelRoomType` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `IdHotel` int NOT NULL,
    `Code` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `Libelle` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
    `Description` varchar(500) CHARACTER SET utf8mb4 NULL,
    `CapacitePersonnesMax` int NULL,
    `PrixNuitReference` decimal(18,2) NULL,
    `CodeDevise` char(3) CHARACTER SET utf8mb4 NULL,
    `Status` enum('Draft','Published','Closed','Cancelled') NOT NULL DEFAULT 'Draft',
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_HotelRoomTypes` PRIMARY KEY (`IdHotelRoomType`),
    CONSTRAINT `FK_HotelRoomTypes_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
    CONSTRAINT `FK_HotelRoomTypes_Hotels_IdHotel` FOREIGN KEY (`IdHotel`) REFERENCES `Hotels` (`IdHotel`) ON DELETE RESTRICT,
    UNIQUE KEY `IX_HotelRoomTypes_Hotel_Code_UQ` (`IdHotel`, `Code`),
    KEY `IX_HotelRoomTypes_IdSociete` (`IdSociete`),
    KEY `IX_HotelRoomTypes_IdHotel` (`IdHotel`)
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `HotelPhotos` (
    `IdHotelPhoto` int NOT NULL AUTO_INCREMENT,
    `IdHotel` int NOT NULL,
    `PhotoData` mediumblob NULL,
    `StorageKey` varchar(500) CHARACTER SET utf8mb4 NULL,
    `Ordre` int NOT NULL,
    `OriginalFileName` varchar(100) CHARACTER SET utf8mb4 NULL,
    `TypeMIME` varchar(50) CHARACTER SET utf8mb4 NULL,
    `FileSize` bigint NULL,
    `Statut` tinyint(1) NOT NULL DEFAULT 1,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_HotelPhotos` PRIMARY KEY (`IdHotelPhoto`),
    CONSTRAINT `FK_HotelPhotos_Hotels_IdHotel` FOREIGN KEY (`IdHotel`) REFERENCES `Hotels` (`IdHotel`) ON DELETE CASCADE,
    UNIQUE KEY `IX_HotelPhotos_Hotel_Ordre_UQ` (`IdHotel`, `Ordre`),
    KEY `IX_HotelPhotos_IdHotel` (`IdHotel`)
) CHARACTER SET=utf8mb4;

SET FOREIGN_KEY_CHECKS = 1;
SELECT 'production_hotel_v1.sql appliqué' AS Info;
