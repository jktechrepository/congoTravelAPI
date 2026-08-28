-- =============================================================================
-- CongoTravel — Hôtel Phase 7d Check-in réception (MySQL, idempotent)
-- =============================================================================
-- Prérequis : production_hotel_phase3_reservations.sql (+ phase7c si rooms)
-- Timestamps arrivée / départ staff sur HotelReservations (pas nouveau statut)
-- =============================================================================

SET NAMES utf8mb4;

SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'HotelReservations' AND COLUMN_NAME = 'CheckedInAtUtc'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `HotelReservations` ADD `CheckedInAtUtc` datetime(6) NULL AFTER `ExpiresAtUtc`',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*) FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'HotelReservations' AND COLUMN_NAME = 'CheckedOutAtUtc'
);
SET @sql := IF(@col_exists = 0,
    'ALTER TABLE `HotelReservations` ADD `CheckedOutAtUtc` datetime(6) NULL AFTER `CheckedInAtUtc`',
    'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SELECT 'production_hotel_phase7d_checkin.sql appliqué' AS Info;
