START TRANSACTION;

ALTER TABLE `Paiements` ADD `StatutPaiementMetier` int NULL;

CREATE TABLE `CommandesReservationEnAttente` (
    `IdCommandeReservationEnAttente` char(36) COLLATE ascii_general_ci NOT NULL,
    `IdSociete` int NOT NULL,
    `IdSite` int NULL,
    `IdUtilisateur` int NOT NULL,
    `MethodePaiement` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `MontantVoyage` decimal(18,2) NOT NULL,
    `CodeDeviseVoyage` varchar(3) CHARACTER SET utf8mb4 NOT NULL,
    `MontantFlexPay` decimal(18,2) NOT NULL,
    `CodeDevisePaiement` varchar(3) CHARACTER SET utf8mb4 NOT NULL,
    `TauxVersDevisePaiement` decimal(18,8) NOT NULL,
    `OrderNumberFlexPay` varchar(100) CHARACTER SET utf8mb4 NULL,
    `ReferenceFlexPay` varchar(100) CHARACTER SET utf8mb4 NULL,
    `PayloadMetierJson` longtext CHARACTER SET utf8mb4 NOT NULL,
    `IdPaiementEnAttente` int NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateExpiration` datetime(6) NULL,
    CONSTRAINT `PK_CommandesReservationEnAttente` PRIMARY KEY (`IdCommandeReservationEnAttente`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `SiegeHoldsEnAttente` (
    `IdSiegeHoldEnAttente` int NOT NULL AUTO_INCREMENT,
    `IdVoyage` int NOT NULL,
    `IdSiege` int NOT NULL,
    `IdCommandeReservationEnAttente` char(36) COLLATE ascii_general_ci NOT NULL,
    `ExpireAt` datetime(6) NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    CONSTRAINT `PK_SiegeHoldsEnAttente` PRIMARY KEY (`IdSiegeHoldEnAttente`)
) CHARACTER SET=utf8mb4;

CREATE UNIQUE INDEX `IX_CommandesReservationEnAttente_OrderNumber` ON `CommandesReservationEnAttente` (`OrderNumberFlexPay`);

CREATE INDEX `IX_CommandesReservationEnAttente_Societe_Date` ON `CommandesReservationEnAttente` (`IdSociete`, `DateCreation`);

CREATE INDEX `IX_SiegeHoldsEnAttente_IdCommande` ON `SiegeHoldsEnAttente` (`IdCommandeReservationEnAttente`);

CREATE INDEX `IX_SiegeHoldsEnAttente_Voyage_ExpireAt` ON `SiegeHoldsEnAttente` (`IdVoyage`, `ExpireAt`);

CREATE UNIQUE INDEX `IX_SiegeHoldsEnAttente_Voyage_Siege_Unique` ON `SiegeHoldsEnAttente` (`IdVoyage`, `IdSiege`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260524142738_FlexPayRegressionFoundation', '6.0.25');

COMMIT;

START TRANSACTION;

CREATE TABLE `InfoPaiementsSociete` (
    `IdInfoPaiementSociete` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `IdSite` int NOT NULL,
    `CodeMarchand` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `ApiToken` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `ActifMobileMoney` tinyint(1) NOT NULL,
    `ActifCarteBancaire` tinyint(1) NOT NULL,
    `Statut` tinyint(1) NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_InfoPaiementsSociete` PRIMARY KEY (`IdInfoPaiementSociete`),
    CONSTRAINT `FK_InfoPaiementsSociete_Sites_IdSite` FOREIGN KEY (`IdSite`) REFERENCES `Sites` (`IdSite`) ON DELETE RESTRICT,
    CONSTRAINT `FK_InfoPaiementsSociete_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `TransactionsFlexPay` (
    `IdTransaction` char(36) COLLATE ascii_general_ci NOT NULL,
    `OrderNumber` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Reference` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `ProviderReference` varchar(100) CHARACTER SET utf8mb4 NULL,
    `TypePaiement` varchar(10) CHARACTER SET utf8mb4 NOT NULL,
    `Channel` varchar(50) CHARACTER SET utf8mb4 NULL,
    `Amount` decimal(18,2) NOT NULL,
    `AmountCustomer` decimal(18,2) NULL,
    `Currency` varchar(10) CHARACTER SET utf8mb4 NOT NULL,
    `Phone` varchar(20) CHARACTER SET utf8mb4 NULL,
    `StatusFlexPay` int NOT NULL,
    `CodeFlexPay` varchar(10) CHARACTER SET utf8mb4 NULL,
    `MessageFlexPay` varchar(500) CHARACTER SET utf8mb4 NULL,
    `StatutPaiement` int NOT NULL,
    `Merchant` varchar(100) CHARACTER SET utf8mb4 NULL,
    `CallbackUrl` varchar(500) CHARACTER SET utf8mb4 NULL,
    `PaymentUrl` varchar(500) CHARACTER SET utf8mb4 NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateCreationFlexPay` datetime(6) NULL,
    `DateCallback` datetime(6) NULL,
    `DateDerniereVerification` datetime(6) NULL,
    `IdUtilisateur` int NOT NULL,
    `IdCommandeReservationEnAttente` char(36) COLLATE ascii_general_ci NULL,
    `IdPaiement` int NULL,
    `IdReservation` int NULL,
    `MessageErreur` varchar(1000) CHARACTER SET utf8mb4 NULL,
    `CodeHttpFlexPay` int NULL,
    `ReponseBruteFlexPay` longtext CHARACTER SET utf8mb4 NULL,
    `NombreCallbacks` int NOT NULL,
    `NombreVerifications` int NOT NULL,
    CONSTRAINT `PK_TransactionsFlexPay` PRIMARY KEY (`IdTransaction`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `CallbacksFlexPay` (
    `IdCallback` char(36) COLLATE ascii_general_ci NOT NULL,
    `IdTransaction` char(36) COLLATE ascii_general_ci NULL,
    `OrderNumber` varchar(100) CHARACTER SET utf8mb4 NULL,
    `Code` varchar(10) CHARACTER SET utf8mb4 NULL,
    `Reference` varchar(100) CHARACTER SET utf8mb4 NULL,
    `ProviderReference` varchar(100) CHARACTER SET utf8mb4 NULL,
    `Amount` varchar(50) CHARACTER SET utf8mb4 NULL,
    `AmountCustomer` varchar(50) CHARACTER SET utf8mb4 NULL,
    `Phone` varchar(20) CHARACTER SET utf8mb4 NULL,
    `Currency` varchar(10) CHARACTER SET utf8mb4 NULL,
    `Channel` varchar(50) CHARACTER SET utf8mb4 NULL,
    `CreatedAt` varchar(50) CHARACTER SET utf8mb4 NULL,
    `PayloadComplet` longtext CHARACTER SET utf8mb4 NULL,
    `Headers` longtext CHARACTER SET utf8mb4 NULL,
    `IpSource` varchar(50) CHARACTER SET utf8mb4 NULL,
    `DateReception` datetime(6) NOT NULL,
    `TraiteAvecSucces` tinyint(1) NOT NULL,
    `MessageErreur` varchar(1000) CHARACTER SET utf8mb4 NULL,
    `DetailsTraitement` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_CallbacksFlexPay` PRIMARY KEY (`IdCallback`),
    CONSTRAINT `FK_CallbacksFlexPay_TransactionsFlexPay_IdTransaction` FOREIGN KEY (`IdTransaction`) REFERENCES `TransactionsFlexPay` (`IdTransaction`) ON DELETE SET NULL
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_CallbackFlexPay_DateReception` ON `CallbacksFlexPay` (`DateReception`);

CREATE INDEX `IX_CallbackFlexPay_OrderNumber` ON `CallbacksFlexPay` (`OrderNumber`);

CREATE INDEX `IX_CallbacksFlexPay_IdTransaction` ON `CallbacksFlexPay` (`IdTransaction`);

CREATE UNIQUE INDEX `IX_InfoPaiementSociete_IdSite_Unique` ON `InfoPaiementsSociete` (`IdSite`);

CREATE INDEX `IX_InfoPaiementSociete_IdSociete` ON `InfoPaiementsSociete` (`IdSociete`);

CREATE UNIQUE INDEX `IX_TransactionFlexPay_OrderNumber` ON `TransactionsFlexPay` (`OrderNumber`);

CREATE INDEX `IX_TransactionFlexPay_Reference` ON `TransactionsFlexPay` (`Reference`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260524144823_FlexPayCallbackAndInfoPaiement', '6.0.25');

COMMIT;

