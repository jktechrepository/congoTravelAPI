-- =============================================================================
-- CongoTravel — Restaurant Phase 2 : réservations + paiements acompte
-- Prérequis : Scripts/production_restaurant_v1.sql
-- =============================================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS `RestaurantReservations` (
    `IdRestaurantReservation` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `IdRestaurant` int NOT NULL,
    `IdRestaurantCreneau` int NOT NULL,
    `IdSite` int NULL,
    `IdUtilisateur` int NULL,
    `ReferenceReservation` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `CustomerRef` varchar(100) CHARACTER SET utf8mb4 NULL,
    `Status` enum('HOLD','CONFIRMED','CANCELLED','EXPIRED') CHARACTER SET utf8mb4 NOT NULL,
    `ExpiresAtUtc` datetime(6) NULL,
    `MontantSousTotal` decimal(18,2) NOT NULL DEFAULT 0.00,
    `CodeDevise` char(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF',
    `NombreCouverts` int NOT NULL,
    `IdempotencyKey` varchar(120) CHARACTER SET utf8mb4 NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_RestaurantReservations` PRIMARY KEY (`IdRestaurantReservation`),
    CONSTRAINT `FK_RestaurantReservations_Societes`
        FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
    CONSTRAINT `FK_RestaurantReservations_Restaurants`
        FOREIGN KEY (`IdRestaurant`) REFERENCES `Restaurants` (`IdRestaurant`) ON DELETE RESTRICT,
    CONSTRAINT `FK_RestaurantReservations_Creneaux`
        FOREIGN KEY (`IdRestaurantCreneau`) REFERENCES `RestaurantCreneaux` (`IdRestaurantCreneau`) ON DELETE RESTRICT,
    CONSTRAINT `FK_RestaurantReservations_Sites`
        FOREIGN KEY (`IdSite`) REFERENCES `Sites` (`IdSite`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `RestaurantReservationLines` (
    `IdRestaurantReservationLine` int NOT NULL AUTO_INCREMENT,
    `IdRestaurantReservation` int NOT NULL,
    `LineType` enum('GlobalQuota','ClassQuota') CHARACTER SET utf8mb4 NOT NULL,
    `Quantite` int NOT NULL,
    `PrixUnitaire` decimal(18,2) NOT NULL,
    `MontantLigne` decimal(18,2) NOT NULL,
    `CodeDevise` char(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF',
    `IdRestaurantCreneauGlobalQuota` int NULL,
    CONSTRAINT `PK_RestaurantReservationLines` PRIMARY KEY (`IdRestaurantReservationLine`),
    CONSTRAINT `CK_RestaurantReservationLines_Quantite` CHECK (`Quantite` > 0),
    CONSTRAINT `FK_RestaurantReservationLines_Reservations`
        FOREIGN KEY (`IdRestaurantReservation`) REFERENCES `RestaurantReservations` (`IdRestaurantReservation`) ON DELETE CASCADE,
    CONSTRAINT `FK_RestaurantReservationLines_GlobalQuota`
        FOREIGN KEY (`IdRestaurantCreneauGlobalQuota`) REFERENCES `RestaurantCreneauGlobalQuotas` (`IdRestaurantCreneau`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE IF NOT EXISTS `RestaurantPayments` (
    `IdRestaurantPayment` int NOT NULL AUTO_INCREMENT,
    `IdRestaurantReservation` int NOT NULL,
    `IdSite` int NULL,
    `ReferencePaiement` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Provider` varchar(40) CHARACTER SET utf8mb4 NOT NULL,
    `ProviderTxRef` varchar(120) CHARACTER SET utf8mb4 NULL,
    `Status` enum('PENDING','SUCCEEDED','FAILED','REFUNDED') CHARACTER SET utf8mb4 NOT NULL,
    `Montant` decimal(18,2) NOT NULL,
    `CodeDevise` char(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF',
    `MontantTarif` decimal(18,2) NOT NULL,
    `CodeDeviseTarif` char(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF',
    `TauxVersDevisePaiement` decimal(18,8) NOT NULL DEFAULT 1.00000000,
    `IdempotencyKey` varchar(120) CHARACTER SET utf8mb4 NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_RestaurantPayments` PRIMARY KEY (`IdRestaurantPayment`),
    CONSTRAINT `FK_RestaurantPayments_Reservations`
        FOREIGN KEY (`IdRestaurantReservation`) REFERENCES `RestaurantReservations` (`IdRestaurantReservation`) ON DELETE RESTRICT,
    CONSTRAINT `FK_RestaurantPayments_Sites`
        FOREIGN KEY (`IdSite`) REFERENCES `Sites` (`IdSite`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

-- Indexes (idempotent via information_schema)
SET @idx := (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'RestaurantReservations' AND INDEX_NAME = 'IX_RestaurantReservations_Societe_Reference_UQ');
SET @sql := IF(@idx = 0, 'CREATE UNIQUE INDEX `IX_RestaurantReservations_Societe_Reference_UQ` ON `RestaurantReservations` (`IdSociete`, `ReferenceReservation`)', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

SET @idx := (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'RestaurantReservations' AND INDEX_NAME = 'IX_RestaurantReservations_Societe_Idempotency_UQ');
SET @sql := IF(@idx = 0, 'CREATE UNIQUE INDEX `IX_RestaurantReservations_Societe_Idempotency_UQ` ON `RestaurantReservations` (`IdSociete`, `IdempotencyKey`)', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

SET @idx := (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'RestaurantReservations' AND INDEX_NAME = 'IX_RestaurantReservations_Status_ExpiresAtUtc');
SET @sql := IF(@idx = 0, 'CREATE INDEX `IX_RestaurantReservations_Status_ExpiresAtUtc` ON `RestaurantReservations` (`Status`, `ExpiresAtUtc`)', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

SET @idx := (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'RestaurantReservations' AND INDEX_NAME = 'IX_RestaurantReservations_Creneau_Status');
SET @sql := IF(@idx = 0, 'CREATE INDEX `IX_RestaurantReservations_Creneau_Status` ON `RestaurantReservations` (`IdRestaurantCreneau`, `Status`)', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

SET @idx := (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'RestaurantReservations' AND INDEX_NAME = 'IX_RestaurantReservations_IdSite');
SET @sql := IF(@idx = 0, 'CREATE INDEX `IX_RestaurantReservations_IdSite` ON `RestaurantReservations` (`IdSite`)', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

SET @idx := (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'RestaurantReservations' AND INDEX_NAME = 'IX_RestaurantReservations_IdUtilisateur');
SET @sql := IF(@idx = 0, 'CREATE INDEX `IX_RestaurantReservations_IdUtilisateur` ON `RestaurantReservations` (`IdUtilisateur`)', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

SET @idx := (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'RestaurantReservations' AND INDEX_NAME = 'IX_RestaurantReservations_IdRestaurant');
SET @sql := IF(@idx = 0, 'CREATE INDEX `IX_RestaurantReservations_IdRestaurant` ON `RestaurantReservations` (`IdRestaurant`)', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

SET @idx := (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'RestaurantReservationLines' AND INDEX_NAME = 'IX_RestaurantReservationLines_IdReservation');
SET @sql := IF(@idx = 0, 'CREATE INDEX `IX_RestaurantReservationLines_IdReservation` ON `RestaurantReservationLines` (`IdRestaurantReservation`)', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

SET @idx := (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'RestaurantPayments' AND INDEX_NAME = 'IX_RestaurantPayments_ReferencePaiement_UQ');
SET @sql := IF(@idx = 0, 'CREATE UNIQUE INDEX `IX_RestaurantPayments_ReferencePaiement_UQ` ON `RestaurantPayments` (`ReferencePaiement`)', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

SET @idx := (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'RestaurantPayments' AND INDEX_NAME = 'IX_RestaurantPayments_Idempotency_UQ');
SET @sql := IF(@idx = 0, 'CREATE UNIQUE INDEX `IX_RestaurantPayments_Idempotency_UQ` ON `RestaurantPayments` (`IdempotencyKey`)', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

SET @idx := (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'RestaurantPayments' AND INDEX_NAME = 'IX_RestaurantPayments_Reservation_Status');
SET @sql := IF(@idx = 0, 'CREATE INDEX `IX_RestaurantPayments_Reservation_Status` ON `RestaurantPayments` (`IdRestaurantReservation`, `Status`)', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

SET @idx := (SELECT COUNT(*) FROM information_schema.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'RestaurantPayments' AND INDEX_NAME = 'IX_RestaurantPayments_IdSite');
SET @sql := IF(@idx = 0, 'CREATE INDEX `IX_RestaurantPayments_IdSite` ON `RestaurantPayments` (`IdSite`)', 'SELECT 1');
PREPARE s FROM @sql; EXECUTE s; DEALLOCATE PREPARE s;

SET FOREIGN_KEY_CHECKS = 1;

-- Voir aussi : Scripts/production_restaurant_hold_expiration_procedure_only.sql (sp_ExpireRestaurantHolds)
