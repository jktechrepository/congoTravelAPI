-- =============================================================================
-- CongoTravel — RestaurantTickets + ConfigSocietes.HeuresOuvertureEntreeRestaurantAvantDebut
-- Migration : 20260815170259_AddRestaurantTickets
-- Prérequis : tables RestaurantReservationLines + ConfigSocietes déjà présentes
-- =============================================================================
-- À exécuter UNE SEULE FOIS. Vérifier avant :
--   SELECT * FROM `__EFMigrationsHistory`
--     WHERE `MigrationId` = '20260815170259_AddRestaurantTickets';
-- =============================================================================

START TRANSACTION;

ALTER TABLE `ConfigSocietes`
    ADD `HeuresOuvertureEntreeRestaurantAvantDebut` int NOT NULL DEFAULT 1;

CREATE TABLE `RestaurantTickets` (
    `IdRestaurantTicket` int NOT NULL AUTO_INCREMENT,
    `IdRestaurantReservationLine` int NOT NULL,
    `TicketCode` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Status` enum('ISSUED','USED','VOID') NOT NULL DEFAULT 'ISSUED',
    `IssuedAtUtc` datetime(6) NOT NULL,
    `UsedAtUtc` datetime(6) NULL,
    CONSTRAINT `PK_RestaurantTickets` PRIMARY KEY (`IdRestaurantTicket`),
    CONSTRAINT `FK_RestaurantTickets_RestaurantReservationLines_IdRestaurantReservationLine`
        FOREIGN KEY (`IdRestaurantReservationLine`)
        REFERENCES `RestaurantReservationLines` (`IdRestaurantReservationLine`)
        ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE UNIQUE INDEX `IX_RestaurantTickets_TicketCode_UQ`
    ON `RestaurantTickets` (`TicketCode`);

CREATE INDEX `IX_RestaurantTickets_Status`
    ON `RestaurantTickets` (`Status`);

CREATE INDEX `IX_RestaurantTickets_IdRestaurantReservationLine`
    ON `RestaurantTickets` (`IdRestaurantReservationLine`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260815170259_AddRestaurantTickets', '6.0.25');

COMMIT;
