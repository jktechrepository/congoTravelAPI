CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;

ALTER DATABASE CHARACTER SET utf8mb4;

CREATE TABLE `AuditLogs` (
    `IdAudit` bigint NOT NULL AUTO_INCREMENT,
    `TableName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `RecordId` int NOT NULL,
    `Action` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `UserId` int NOT NULL,
    `UserName` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `UserRole` varchar(50) CHARACTER SET utf8mb4 NULL,
    `IdSociete` int NULL,
    `DateAction` datetime(6) NOT NULL,
    `OldValues` TEXT CHARACTER SET utf8mb4 NULL,
    `NewValues` TEXT CHARACTER SET utf8mb4 NULL,
    `ChangedFields` varchar(500) CHARACTER SET utf8mb4 NULL,
    `IpAddress` varchar(50) CHARACTER SET utf8mb4 NULL,
    `UserAgent` varchar(500) CHARACTER SET utf8mb4 NULL,
    `Commentaire` TEXT CHARACTER SET utf8mb4 NULL,
    `HttpMethod` varchar(10) CHARACTER SET utf8mb4 NULL,
    `Endpoint` varchar(500) CHARACTER SET utf8mb4 NULL,
    `DurationMs` int NULL,
    `Success` tinyint(1) NOT NULL,
    `ErrorMessage` varchar(1000) CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_AuditLogs` PRIMARY KEY (`IdAudit`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Clients` (
    `IdClient` int NOT NULL AUTO_INCREMENT,
    `NomClient` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `AdresseClient` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `Telephone` varchar(20) CHARACTER SET utf8mb4 NULL,
    `EmailClient` varchar(256) CHARACTER SET utf8mb4 NULL,
    `GenreClient` varchar(10) CHARACTER SET utf8mb4 NULL,
    `Statut` tinyint(1) NOT NULL DEFAULT TRUE,
    `IsActif` tinyint(1) NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    `UpdatedAt` datetime(6) NULL,
    `IsDeleted` tinyint(1) NULL,
    `Province` varchar(100) CHARACTER SET utf8mb4 NULL,
    `Ville` varchar(100) CHARACTER SET utf8mb4 NULL,
    `Commune` varchar(100) CHARACTER SET utf8mb4 NULL,
    `Avenue` varchar(200) CHARACTER SET utf8mb4 NULL,
    `Numero` varchar(50) CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_Clients` PRIMARY KEY (`IdClient`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Permissions` (
    `IdPermission` int NOT NULL AUTO_INCREMENT,
    `Nom` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Description` varchar(255) CHARACTER SET utf8mb4 NULL,
    `Categorie` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Action` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `Statut` tinyint(1) NULL,
    `DateCreation` datetime(6) NOT NULL,
    CONSTRAINT `PK_Permissions` PRIMARY KEY (`IdPermission`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Roles` (
    `IdRole` int NOT NULL AUTO_INCREMENT,
    `Nom` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Description` varchar(255) CHARACTER SET utf8mb4 NULL,
    `Niveau` int NULL,
    `Statut` tinyint(1) NULL,
    `DateCreation` datetime(6) NOT NULL,
    CONSTRAINT `PK_Roles` PRIMARY KEY (`IdRole`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Societes` (
    `IdSociete` int NOT NULL AUTO_INCREMENT,
    `Nom` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `Devise` longtext CHARACTER SET utf8mb4 NULL,
    `Type` varchar(50) CHARACTER SET utf8mb4 NULL,
    `Logo` longtext CHARACTER SET utf8mb4 NULL,
    `Telephone` longtext CHARACTER SET utf8mb4 NULL,
    `EmailContact` longtext CHARACTER SET utf8mb4 NULL,
    `SiteWeb` longtext CHARACTER SET utf8mb4 NULL,
    `NomCompletResponsable` longtext CHARACTER SET utf8mb4 NULL,
    `GenreResponsable` varchar(10) CHARACTER SET utf8mb4 NULL,
    `Description` longtext CHARACTER SET utf8mb4 NULL,
    `Statut` tinyint(1) NULL,
    `AdresseResidence` varchar(500) CHARACTER SET utf8mb4 NULL,
    `DateCreation` datetime(6) NOT NULL,
    CONSTRAINT `PK_Societes` PRIMARY KEY (`IdSociete`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `RolePermissions` (
    `IdRolePermission` int NOT NULL AUTO_INCREMENT,
    `IdRole` int NOT NULL,
    `IdPermission` int NOT NULL,
    `DateAttribution` datetime(6) NOT NULL,
    `IdUtilisateurAttribution` int NULL,
    CONSTRAINT `PK_RolePermissions` PRIMARY KEY (`IdRolePermission`),
    CONSTRAINT `FK_RolePermissions_Permissions_IdPermission` FOREIGN KEY (`IdPermission`) REFERENCES `Permissions` (`IdPermission`) ON DELETE CASCADE,
    CONSTRAINT `FK_RolePermissions_Roles_IdRole` FOREIGN KEY (`IdRole`) REFERENCES `Roles` (`IdRole`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `CategorieSieges` (
    `IdCategorieSiege` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `CodeCategorieSiege` varchar(40) CHARACTER SET utf8mb4 NOT NULL,
    `Libelle` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
    `Statut` tinyint(1) NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_CategorieSieges` PRIMARY KEY (`IdCategorieSiege`),
    CONSTRAINT `FK_CategorieSieges_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `Destinations` (
    `IdDestination` int NOT NULL AUTO_INCREMENT,
    `VilleDepart` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `VilleArrivee` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Montant` decimal(18,2) NOT NULL,
    `HeureDepart` time NULL,
    `jourDepart` varchar(50) CHARACTER SET utf8mb4 NULL,
    `Statut` tinyint(1) NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    `IdSociete` int NOT NULL,
    CONSTRAINT `PK_Destinations` PRIMARY KEY (`IdDestination`),
    CONSTRAINT `FK_Destinations_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `Sites` (
    `IdSite` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `CodeSite` varchar(40) CHARACTER SET utf8mb4 NOT NULL,
    `NomSite` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Ville` varchar(120) CHARACTER SET utf8mb4 NULL,
    `Adresse` varchar(500) CHARACTER SET utf8mb4 NULL,
    `Telephone` varchar(30) CHARACTER SET utf8mb4 NULL,
    `NomResponsableSite` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Email` varchar(200) CHARACTER SET utf8mb4 NULL,
    `Genre` varchar(10) CHARACTER SET utf8mb4 NOT NULL,
    `Statut` tinyint(1) NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_Sites` PRIMARY KEY (`IdSite`),
    CONSTRAINT `FK_Sites_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `TypeVehicules` (
    `IdTypeVehicule` int NOT NULL AUTO_INCREMENT,
    `Libelle` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `IdSociete` int NOT NULL,
    `Statut` tinyint(1) NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_TypeVehicules` PRIMARY KEY (`IdTypeVehicule`),
    CONSTRAINT `FK_TypeVehicules_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `Agents` (
    `IdAgent` int NOT NULL AUTO_INCREMENT,
    `Matricule` varchar(50) CHARACTER SET utf8mb4 NULL,
    `NomComplet` varchar(200) CHARACTER SET utf8mb4 NULL,
    `Genre` varchar(10) CHARACTER SET utf8mb4 NULL,
    `DateNaissance` datetime(6) NOT NULL,
    `TelephoneAgent` longtext CHARACTER SET utf8mb4 NULL,
    `EmailAgent` varchar(255) CHARACTER SET utf8mb4 NULL,
    `Statut` tinyint(1) NULL,
    `EtatCivil` varchar(20) CHARACTER SET utf8mb4 NULL,
    `SerialNumber` varchar(255) CHARACTER SET utf8mb4 NULL,
    `Fonction` longtext CHARACTER SET utf8mb4 NULL,
    `RoleAgent` longtext CHARACTER SET utf8mb4 NULL,
    `PhotoUrl` longtext CHARACTER SET utf8mb4 NULL,
    `IdSociete` int NOT NULL,
    `IdSite` int NULL,
    `AdresseResidence` varchar(500) CHARACTER SET utf8mb4 NULL,
    `Zone` varchar(200) CHARACTER SET utf8mb4 NULL,
    `DateCreation` datetime(6) NOT NULL,
    CONSTRAINT `PK_Agents` PRIMARY KEY (`IdAgent`),
    CONSTRAINT `FK_Agents_Sites_IdSite` FOREIGN KEY (`IdSite`) REFERENCES `Sites` (`IdSite`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Agents_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `Vehicules` (
    `IdVehicule` int NOT NULL AUTO_INCREMENT,
    `Marques` varchar(100) CHARACTER SET utf8mb4 NULL,
    `AliasVehicule` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `IdTypeVehicule` int NOT NULL,
    `NombreSiege` int NOT NULL,
    `IdSociete` int NOT NULL,
    `NumeroDePlaque` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `Photo` varchar(500) CHARACTER SET utf8mb4 NULL,
    `Statut` tinyint(1) NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_Vehicules` PRIMARY KEY (`IdVehicule`),
    CONSTRAINT `FK_Vehicules_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Vehicules_TypeVehicules_IdTypeVehicule` FOREIGN KEY (`IdTypeVehicule`) REFERENCES `TypeVehicules` (`IdTypeVehicule`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `Utilisateurs` (
    `IdUtilisateur` int NOT NULL AUTO_INCREMENT,
    `ReferenceUtilisateur` char(36) COLLATE ascii_general_ci NULL,
    `NomComplet` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Email` varchar(255) CHARACTER SET utf8mb4 NULL,
    `Telephone` longtext CHARACTER SET utf8mb4 NULL,
    `PhotoUrl` longtext CHARACTER SET utf8mb4 NULL,
    `LieuNaissance` longtext CHARACTER SET utf8mb4 NULL,
    `DateNaissance` datetime(6) NULL,
    `Genre` longtext CHARACTER SET utf8mb4 NULL,
    `MotDePasseHash` longtext CHARACTER SET utf8mb4 NOT NULL,
    `DefaultUsername` longtext CHARACTER SET utf8mb4 NULL,
    `DoitChangerMotDePasse` tinyint(1) NOT NULL,
    `Statut` tinyint(1) NULL,
    `IdRole` int NULL,
    `IdSociete` int NULL,
    `AdresseResidence` varchar(500) CHARACTER SET utf8mb4 NULL,
    `DateCreation` datetime(6) NOT NULL,
    `IsConnecte` tinyint(1) NOT NULL,
    `IdAgent` int NULL,
    `IdClient` int NULL,
    `IdSite` int NULL,
    CONSTRAINT `PK_Utilisateurs` PRIMARY KEY (`IdUtilisateur`),
    CONSTRAINT `FK_Utilisateurs_Agents_IdAgent` FOREIGN KEY (`IdAgent`) REFERENCES `Agents` (`IdAgent`),
    CONSTRAINT `FK_Utilisateurs_Clients_IdClient` FOREIGN KEY (`IdClient`) REFERENCES `Clients` (`IdClient`),
    CONSTRAINT `FK_Utilisateurs_Roles_IdRole` FOREIGN KEY (`IdRole`) REFERENCES `Roles` (`IdRole`) ON DELETE CASCADE,
    CONSTRAINT `FK_Utilisateurs_Sites_IdSite` FOREIGN KEY (`IdSite`) REFERENCES `Sites` (`IdSite`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Utilisateurs_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `Sieges` (
    `IdSiege` int NOT NULL AUTO_INCREMENT,
    `IdVehicule` int NOT NULL,
    `NumeroOrdre` int NOT NULL,
    `CodeSiege` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
    `EstActif` tinyint(1) NOT NULL,
    `IdSociete` int NOT NULL,
    `IdCategorieSiege` int NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_Sieges` PRIMARY KEY (`IdSiege`),
    CONSTRAINT `FK_Sieges_CategorieSieges_IdCategorieSiege` FOREIGN KEY (`IdCategorieSiege`) REFERENCES `CategorieSieges` (`IdCategorieSiege`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Sieges_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Sieges_Vehicules_IdVehicule` FOREIGN KEY (`IdVehicule`) REFERENCES `Vehicules` (`IdVehicule`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `Voyages` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `date_depart` datetime(6) NOT NULL,
    `heure_depart` time(6) NOT NULL,
    `prix` int NOT NULL,
    `IdVehicule` int NOT NULL,
    `IdDestination` int NOT NULL,
    `IdSociete` int NOT NULL,
    `Statut` tinyint(1) NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_Voyages` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Voyages_Destinations_IdDestination` FOREIGN KEY (`IdDestination`) REFERENCES `Destinations` (`IdDestination`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Voyages_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Voyages_Vehicules_IdVehicule` FOREIGN KEY (`IdVehicule`) REFERENCES `Vehicules` (`IdVehicule`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `CommunicationCampaigns` (
    `IdCampagne` int NOT NULL AUTO_INCREMENT,
    `Titre` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Contenu` varchar(2000) CHARACTER SET utf8mb4 NOT NULL,
    `TypeCampagne` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `IdSociete` int NULL,
    `IdUtilisateurCreateur` int NOT NULL,
    `CriteresCiblage` TEXT CHARACTER SET utf8mb4 NULL,
    `ListeIdClients` TEXT CHARACTER SET utf8mb4 NULL,
    `ActiverPush` tinyint(1) NOT NULL,
    `ActiverSms` tinyint(1) NOT NULL,
    `ActiverEmail` tinyint(1) NOT NULL,
    `ActiverInApp` tinyint(1) NOT NULL,
    `DateEnvoi` datetime(6) NULL,
    `EstProgrammee` tinyint(1) NOT NULL,
    `EstEnCours` tinyint(1) NOT NULL,
    `EstTerminee` tinyint(1) NOT NULL,
    `NombreDestinataires` int NOT NULL,
    `NombreEnvoyes` int NOT NULL,
    `NombreSucces` int NOT NULL,
    `NombreEchecs` int NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateDerniereModification` datetime(6) NOT NULL,
    `DateEnvoiEffectif` datetime(6) NULL,
    `Statut` tinyint(1) NOT NULL,
    CONSTRAINT `PK_CommunicationCampaigns` PRIMARY KEY (`IdCampagne`),
    CONSTRAINT `FK_CommunicationCampaigns_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE SET NULL,
    CONSTRAINT `FK_CommunicationCampaigns_Utilisateurs_IdUtilisateurCreateur` FOREIGN KEY (`IdUtilisateurCreateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `NotificationPreferences` (
    `IdNotificationPreference` int NOT NULL AUTO_INCREMENT,
    `IdUtilisateur` int NOT NULL,
    `AllowPush` tinyint(1) NOT NULL,
    `AllowInApp` tinyint(1) NOT NULL,
    `AllowSms` tinyint(1) NOT NULL,
    `AllowEmail` tinyint(1) NOT NULL,
    `OptOutGlobal` tinyint(1) NOT NULL,
    `OptOutFactures` tinyint(1) NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NOT NULL,
    CONSTRAINT `PK_NotificationPreferences` PRIMARY KEY (`IdNotificationPreference`),
    CONSTRAINT `FK_NotificationPreferences_Utilisateurs_IdUtilisateur` FOREIGN KEY (`IdUtilisateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `Notifications` (
    `IdNotification` int NOT NULL AUTO_INCREMENT,
    `Titre` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Statut` tinyint(1) NULL,
    `Contenu` varchar(1000) CHARACTER SET utf8mb4 NOT NULL,
    `TypeNotification` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `EstLue` tinyint(1) NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateLecture` datetime(6) NULL,
    `LienAction` varchar(100) CHARACTER SET utf8mb4 NULL,
    `Icone` varchar(50) CHARACTER SET utf8mb4 NULL,
    `EstActive` tinyint(1) NOT NULL,
    `IdExpediteur` int NULL,
    `IdDestinataire` int NULL,
    `IdSociete` int NULL,
    `IdAgent` int NULL,
    `CanalUtilise` varchar(20) CHARACTER SET utf8mb4 NULL,
    `Priorite` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `PayloadJson` longtext CHARACTER SET utf8mb4 NULL,
    `StatutEnvoi` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `TrackingId` varchar(100) CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_Notifications` PRIMARY KEY (`IdNotification`),
    CONSTRAINT `FK_Notifications_Agents_IdAgent` FOREIGN KEY (`IdAgent`) REFERENCES `Agents` (`IdAgent`),
    CONSTRAINT `FK_Notifications_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`),
    CONSTRAINT `FK_Notifications_Utilisateurs_IdDestinataire` FOREIGN KEY (`IdDestinataire`) REFERENCES `Utilisateurs` (`IdUtilisateur`),
    CONSTRAINT `FK_Notifications_Utilisateurs_IdExpediteur` FOREIGN KEY (`IdExpediteur`) REFERENCES `Utilisateurs` (`IdUtilisateur`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `PasswordResetTokens` (
    `IdPasswordResetToken` int NOT NULL AUTO_INCREMENT,
    `IdUtilisateur` int NOT NULL,
    `Token` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateExpiration` datetime(6) NOT NULL,
    `DateUtilisation` datetime(6) NULL,
    CONSTRAINT `PK_PasswordResetTokens` PRIMARY KEY (`IdPasswordResetToken`),
    CONSTRAINT `FK_PasswordResetTokens_Utilisateurs_IdUtilisateur` FOREIGN KEY (`IdUtilisateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `PlainteClients` (
    `IdPlainte` int NOT NULL AUTO_INCREMENT,
    `IdClient` int NOT NULL,
    `Titre` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Description` varchar(2000) CHARACTER SET utf8mb4 NULL,
    `TypePanne` varchar(200) CHARACTER SET utf8mb4 NULL,
    `NiveauImportance` varchar(50) CHARACTER SET utf8mb4 NULL,
    `RisquesPrincipaux` varchar(500) CHARACTER SET utf8mb4 NULL,
    `StatutPlainte` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Priorite` varchar(50) CHARACTER SET utf8mb4 NULL,
    `IdAgentAssigné` int NULL,
    `IdUtilisateurCreateur` int NULL,
    `CommentaireResolution` varchar(1000) CHARACTER SET utf8mb4 NULL,
    `DateResolution` datetime(6) NULL,
    `EstUrgente` tinyint(1) NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateDerniereModification` datetime(6) NOT NULL,
    `Statut` tinyint(1) NOT NULL DEFAULT TRUE,
    CONSTRAINT `PK_PlainteClients` PRIMARY KEY (`IdPlainte`),
    CONSTRAINT `FK_PlainteClients_Agents_IdAgentAssigné` FOREIGN KEY (`IdAgentAssigné`) REFERENCES `Agents` (`IdAgent`) ON DELETE SET NULL,
    CONSTRAINT `FK_PlainteClients_Clients_IdClient` FOREIGN KEY (`IdClient`) REFERENCES `Clients` (`IdClient`) ON DELETE RESTRICT,
    CONSTRAINT `FK_PlainteClients_Utilisateurs_IdUtilisateurCreateur` FOREIGN KEY (`IdUtilisateurCreateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE SET NULL
) CHARACTER SET=utf8mb4;

CREATE TABLE `RefreshTokens` (
    `IdRefreshToken` int NOT NULL AUTO_INCREMENT,
    `IdUtilisateur` int NOT NULL,
    `TokenHash` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateExpiration` datetime(6) NOT NULL,
    `DateRevocation` datetime(6) NULL,
    `DeviceInfo` varchar(200) CHARACTER SET utf8mb4 NULL,
    `IpAddress` varchar(50) CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_RefreshTokens` PRIMARY KEY (`IdRefreshToken`),
    CONSTRAINT `FK_RefreshTokens_Utilisateurs_IdUtilisateur` FOREIGN KEY (`IdUtilisateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `SmsLogs` (
    `IdSmsLog` int NOT NULL AUTO_INCREMENT,
    `NumeroDestinataire` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `IdUtilisateur` int NULL,
    `Message` varchar(1600) CHARACTER SET utf8mb4 NOT NULL,
    `TypeNotification` varchar(50) CHARACTER SET utf8mb4 NULL,
    `Statut` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `MessageSid` varchar(100) CHARACTER SET utf8mb4 NULL,
    `MessageErreur` varchar(500) CHARACTER SET utf8mb4 NULL,
    `CodeErreur` int NULL,
    `CoutUsd` double NOT NULL,
    `CoutFc` double NOT NULL,
    `DateEnvoi` datetime(6) NOT NULL,
    `DateLivraison` datetime(6) NULL,
    `DateEchec` datetime(6) NULL,
    `NombreSegments` int NOT NULL,
    `Direction` varchar(10) CHARACTER SET utf8mb4 NULL,
    `NumeroExpediteur` varchar(50) CHARACTER SET utf8mb4 NULL,
    `UtilisateurIdUtilisateur` int NULL,
    CONSTRAINT `PK_SmsLogs` PRIMARY KEY (`IdSmsLog`),
    CONSTRAINT `FK_SmsLogs_Utilisateurs_UtilisateurIdUtilisateur` FOREIGN KEY (`UtilisateurIdUtilisateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `UserDevices` (
    `IdUserDevice` int NOT NULL AUTO_INCREMENT,
    `IdUtilisateur` int NOT NULL,
    `FcmToken` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `DeviceType` varchar(100) CHARACTER SET utf8mb4 NULL,
    `DeviceModel` varchar(100) CHARACTER SET utf8mb4 NULL,
    `OsVersion` varchar(50) CHARACTER SET utf8mb4 NULL,
    `DefaultDevice` varchar(100) CHARACTER SET utf8mb4 NULL,
    `Statut` tinyint(1) NULL,
    `DateEnregistrement` datetime(6) NOT NULL,
    `DateDerniereUtilisation` datetime(6) NULL,
    CONSTRAINT `PK_UserDevices` PRIMARY KEY (`IdUserDevice`),
    CONSTRAINT `FK_UserDevices_Utilisateurs_IdUtilisateur` FOREIGN KEY (`IdUtilisateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `UserPermissions` (
    `IdUserPermission` int NOT NULL AUTO_INCREMENT,
    `IdUtilisateur` int NOT NULL,
    `IdPermission` int NOT NULL,
    `IsGranted` tinyint(1) NOT NULL,
    `DateAttribution` datetime(6) NOT NULL,
    `DateExpiration` datetime(6) NULL,
    `Commentaire` varchar(500) CHARACTER SET utf8mb4 NULL,
    `AttribueParIdUtilisateur` int NULL,
    CONSTRAINT `PK_UserPermissions` PRIMARY KEY (`IdUserPermission`),
    CONSTRAINT `FK_UserPermissions_Permissions_IdPermission` FOREIGN KEY (`IdPermission`) REFERENCES `Permissions` (`IdPermission`) ON DELETE CASCADE,
    CONSTRAINT `FK_UserPermissions_Utilisateurs_AttribueParIdUtilisateur` FOREIGN KEY (`AttribueParIdUtilisateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`),
    CONSTRAINT `FK_UserPermissions_Utilisateurs_IdUtilisateur` FOREIGN KEY (`IdUtilisateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `UserRoles` (
    `IdUserRole` int NOT NULL AUTO_INCREMENT,
    `IdUtilisateur` int NOT NULL,
    `IdRole` int NOT NULL,
    `IsPrimary` tinyint(1) NOT NULL,
    `DateAttribution` datetime(6) NOT NULL,
    `IdUtilisateurAttribution` int NULL,
    `Statut` tinyint(1) NULL,
    CONSTRAINT `PK_UserRoles` PRIMARY KEY (`IdUserRole`),
    CONSTRAINT `FK_UserRoles_Roles_IdRole` FOREIGN KEY (`IdRole`) REFERENCES `Roles` (`IdRole`) ON DELETE RESTRICT,
    CONSTRAINT `FK_UserRoles_Utilisateurs_IdUtilisateur` FOREIGN KEY (`IdUtilisateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `Reservations` (
    `IdReservation` int NOT NULL AUTO_INCREMENT,
    `IdUtilisateur` int NOT NULL,
    `IdClient` int NOT NULL,
    `IdVoyage` int NOT NULL,
    `StatutReservation` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `Statut` tinyint(1) NOT NULL,
    `dateReservation` datetime(6) NOT NULL,
    `IdSociete` int NOT NULL,
    `IdSite` int NULL,
    `nombreDePlace` int NOT NULL DEFAULT 1,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_Reservations` PRIMARY KEY (`IdReservation`),
    CONSTRAINT `FK_Reservations_Clients_IdClient` FOREIGN KEY (`IdClient`) REFERENCES `Clients` (`IdClient`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Reservations_Sites_IdSite` FOREIGN KEY (`IdSite`) REFERENCES `Sites` (`IdSite`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Reservations_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Reservations_Utilisateurs_IdUtilisateur` FOREIGN KEY (`IdUtilisateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Reservations_Voyages_IdVoyage` FOREIGN KEY (`IdVoyage`) REFERENCES `Voyages` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `VoyageDestinations` (
    `IdVoyageDestination` int NOT NULL AUTO_INCREMENT,
    `IdVoyage` int NOT NULL,
    `IdDestination` int NOT NULL,
    `Ordre` int NOT NULL,
    `IdSociete` int NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_VoyageDestinations` PRIMARY KEY (`IdVoyageDestination`),
    CONSTRAINT `FK_VoyageDestinations_Destinations_IdDestination` FOREIGN KEY (`IdDestination`) REFERENCES `Destinations` (`IdDestination`) ON DELETE RESTRICT,
    CONSTRAINT `FK_VoyageDestinations_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
    CONSTRAINT `FK_VoyageDestinations_Voyages_IdVoyage` FOREIGN KEY (`IdVoyage`) REFERENCES `Voyages` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `VoyageTarifsCategorieSiege` (
    `IdVoyageTarifCategorieSiege` int NOT NULL AUTO_INCREMENT,
    `IdVoyage` int NOT NULL,
    `IdCategorieSiege` int NOT NULL,
    `Prix` int NOT NULL,
    `IdSociete` int NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_VoyageTarifsCategorieSiege` PRIMARY KEY (`IdVoyageTarifCategorieSiege`),
    CONSTRAINT `FK_VoyageTarifsCategorieSiege_CategorieSieges_IdCategorieSiege` FOREIGN KEY (`IdCategorieSiege`) REFERENCES `CategorieSieges` (`IdCategorieSiege`) ON DELETE RESTRICT,
    CONSTRAINT `FK_VoyageTarifsCategorieSiege_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
    CONSTRAINT `FK_VoyageTarifsCategorieSiege_Voyages_IdVoyage` FOREIGN KEY (`IdVoyage`) REFERENCES `Voyages` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `ReservationPassengers` (
    `IdReservationPassenger` int NOT NULL AUTO_INCREMENT,
    `IdReservation` int NOT NULL,
    `IdClient` int NULL,
    `NomComplet` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Telephone` varchar(20) CHARACTER SET utf8mb4 NULL,
    `Email` varchar(256) CHARACTER SET utf8mb4 NULL,
    `DocumentType` varchar(50) CHARACTER SET utf8mb4 NULL,
    `DocumentNumero` varchar(100) CHARACTER SET utf8mb4 NULL,
    `DateNaissance` date NULL,
    `Genre` varchar(10) CHARACTER SET utf8mb4 NULL,
    `IdSociete` int NOT NULL,
    `Statut` tinyint(1) NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_ReservationPassengers` PRIMARY KEY (`IdReservationPassenger`),
    CONSTRAINT `FK_ReservationPassengers_Clients_IdClient` FOREIGN KEY (`IdClient`) REFERENCES `Clients` (`IdClient`) ON DELETE RESTRICT,
    CONSTRAINT `FK_ReservationPassengers_Reservations_IdReservation` FOREIGN KEY (`IdReservation`) REFERENCES `Reservations` (`IdReservation`) ON DELETE CASCADE,
    CONSTRAINT `FK_ReservationPassengers_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `Billets` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `IsUsed` tinyint(1) NOT NULL DEFAULT FALSE,
    `IdReservation` int NULL,
    `QrCode` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `dateGeneration` datetime(6) NOT NULL,
    `IdSociete` int NOT NULL,
    `IdSite` int NULL,
    `IdClient` int NULL,
    `IdReservationPassenger` int NULL,
    `IdSiege` int NULL,
    `CodeSiege` varchar(120) CHARACTER SET utf8mb4 NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_Billets` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Billets_Clients_IdClient` FOREIGN KEY (`IdClient`) REFERENCES `Clients` (`IdClient`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Billets_ReservationPassengers_IdReservationPassenger` FOREIGN KEY (`IdReservationPassenger`) REFERENCES `ReservationPassengers` (`IdReservationPassenger`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Billets_Reservations_IdReservation` FOREIGN KEY (`IdReservation`) REFERENCES `Reservations` (`IdReservation`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Billets_Sieges_IdSiege` FOREIGN KEY (`IdSiege`) REFERENCES `Sieges` (`IdSiege`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Billets_Sites_IdSite` FOREIGN KEY (`IdSite`) REFERENCES `Sites` (`IdSite`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Billets_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `VoyageSeatAllocations` (
    `IdVoyageSeatAllocation` int NOT NULL AUTO_INCREMENT,
    `IdVoyage` int NOT NULL,
    `IdSiege` int NOT NULL,
    `IdReservationPassenger` int NOT NULL,
    `Statut` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_VoyageSeatAllocations` PRIMARY KEY (`IdVoyageSeatAllocation`),
    CONSTRAINT `FK_VoyageSeatAllocations_ReservationPassengers_IdReservationPas~` FOREIGN KEY (`IdReservationPassenger`) REFERENCES `ReservationPassengers` (`IdReservationPassenger`) ON DELETE CASCADE,
    CONSTRAINT `FK_VoyageSeatAllocations_Sieges_IdSiege` FOREIGN KEY (`IdSiege`) REFERENCES `Sieges` (`IdSiege`) ON DELETE RESTRICT,
    CONSTRAINT `FK_VoyageSeatAllocations_Voyages_IdVoyage` FOREIGN KEY (`IdVoyage`) REFERENCES `Voyages` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `BilletEmbarquements` (
    `IdEmbarquement` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `IdBillet` int NOT NULL,
    `IdReservationPassenger` int NOT NULL,
    `DateEmbarquementUtc` datetime(6) NOT NULL,
    `IdUtilisateurEnregistrement` int NULL,
    CONSTRAINT `PK_BilletEmbarquements` PRIMARY KEY (`IdEmbarquement`),
    CONSTRAINT `FK_BilletEmbarquements_Billets_IdBillet` FOREIGN KEY (`IdBillet`) REFERENCES `Billets` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_BilletEmbarquements_ReservationPassengers_IdReservationPasse~` FOREIGN KEY (`IdReservationPassenger`) REFERENCES `ReservationPassengers` (`IdReservationPassenger`) ON DELETE RESTRICT,
    CONSTRAINT `FK_BilletEmbarquements_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
    CONSTRAINT `FK_BilletEmbarquements_Utilisateurs_IdUtilisateurEnregistrement` FOREIGN KEY (`IdUtilisateurEnregistrement`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `Paiements` (
    `IdPaiement` int NOT NULL AUTO_INCREMENT,
    `MontantAPaye` decimal(18,2) NOT NULL,
    `MontantPaye` decimal(18,2) NULL,
    `ResteAPaye` decimal(18,2) NULL,
    `MethodePaiement` varchar(50) CHARACTER SET utf8mb4 NULL,
    `ReferenceTransaction` varchar(100) CHARACTER SET utf8mb4 NULL,
    `Statut` tinyint(1) NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    `IsDeleted` tinyint(1) NOT NULL DEFAULT FALSE,
    `IdUtilisateur` int NOT NULL,
    `IdReservation` int NULL,
    `IdSociete` int NOT NULL,
    `IdSite` int NULL,
    `DateEmissionBillet` datetime(6) NULL,
    `IdBilletEmis` int NULL,
    CONSTRAINT `PK_Paiements` PRIMARY KEY (`IdPaiement`),
    CONSTRAINT `FK_Paiements_Billets_IdBilletEmis` FOREIGN KEY (`IdBilletEmis`) REFERENCES `Billets` (`Id`),
    CONSTRAINT `FK_Paiements_Reservations_IdReservation` FOREIGN KEY (`IdReservation`) REFERENCES `Reservations` (`IdReservation`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Paiements_Sites_IdSite` FOREIGN KEY (`IdSite`) REFERENCES `Sites` (`IdSite`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Paiements_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Paiements_Utilisateurs_IdUtilisateur` FOREIGN KEY (`IdUtilisateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE UNIQUE INDEX `IX_Agents_Email_Unique` ON `Agents` (`EmailAgent`);

CREATE INDEX `IX_Agents_IdSite` ON `Agents` (`IdSite`);

CREATE INDEX `IX_Agents_IdSociete` ON `Agents` (`IdSociete`);

CREATE UNIQUE INDEX `IX_Agents_Matricule_Unique` ON `Agents` (`Matricule`);

CREATE UNIQUE INDEX `IX_Agents_SerialNumber_Unique` ON `Agents` (`SerialNumber`);

CREATE INDEX `IX_AuditLog_Action` ON `AuditLogs` (`Action`);

CREATE INDEX `IX_AuditLog_DateAction` ON `AuditLogs` (`DateAction`);

CREATE INDEX `IX_AuditLog_IdSociete` ON `AuditLogs` (`IdSociete`);

CREATE INDEX `IX_AuditLog_Table_Record` ON `AuditLogs` (`TableName`, `RecordId`);

CREATE INDEX `IX_AuditLog_UserId` ON `AuditLogs` (`UserId`);

CREATE UNIQUE INDEX `IX_BilletEmbarquements_IdBillet_Unique` ON `BilletEmbarquements` (`IdBillet`);

CREATE INDEX `IX_BilletEmbarquements_IdReservationPassenger` ON `BilletEmbarquements` (`IdReservationPassenger`);

CREATE INDEX `IX_BilletEmbarquements_IdSociete` ON `BilletEmbarquements` (`IdSociete`);

CREATE INDEX `IX_BilletEmbarquements_IdUtilisateurEnregistrement` ON `BilletEmbarquements` (`IdUtilisateurEnregistrement`);

CREATE INDEX `IX_Billets_DateGeneration` ON `Billets` (`dateGeneration`);

CREATE INDEX `IX_Billets_IdClient` ON `Billets` (`IdClient`);

CREATE INDEX `IX_Billets_IdReservation` ON `Billets` (`IdReservation`);

CREATE INDEX `IX_Billets_IdReservationPassenger` ON `Billets` (`IdReservationPassenger`);

CREATE INDEX `IX_Billets_IdSiege` ON `Billets` (`IdSiege`);

CREATE INDEX `IX_Billets_IdSite` ON `Billets` (`IdSite`);

CREATE INDEX `IX_Billets_IdSociete` ON `Billets` (`IdSociete`);

CREATE INDEX `IX_Billets_QrCode` ON `Billets` (`QrCode`);

CREATE INDEX `IX_CategorieSieges_IdSociete` ON `CategorieSieges` (`IdSociete`);

CREATE UNIQUE INDEX `IX_CategorieSieges_Societe_Code_Unique` ON `CategorieSieges` (`IdSociete`, `CodeCategorieSiege`);

CREATE UNIQUE INDEX `IX_Clients_EmailClient_Unique` ON `Clients` (`EmailClient`);

CREATE INDEX `IX_Clients_Sync` ON `Clients` (`UpdatedAt`, `IdClient`);

CREATE UNIQUE INDEX `IX_Clients_Telephone_Unique` ON `Clients` (`Telephone`);

CREATE INDEX `IX_CommunicationCampaigns_IdSociete` ON `CommunicationCampaigns` (`IdSociete`);

CREATE INDEX `IX_CommunicationCampaigns_IdUtilisateurCreateur` ON `CommunicationCampaigns` (`IdUtilisateurCreateur`);

CREATE INDEX `IX_Destinations_IdSociete` ON `Destinations` (`IdSociete`);

CREATE INDEX `IX_Destinations_Villes` ON `Destinations` (`VilleDepart`, `VilleArrivee`);

CREATE INDEX `IX_NotificationPreferences_IdUtilisateur` ON `NotificationPreferences` (`IdUtilisateur`);

CREATE INDEX `IX_Notifications_IdAgent` ON `Notifications` (`IdAgent`);

CREATE INDEX `IX_Notifications_IdDestinataire` ON `Notifications` (`IdDestinataire`);

CREATE INDEX `IX_Notifications_IdExpediteur` ON `Notifications` (`IdExpediteur`);

CREATE INDEX `IX_Notifications_IdSociete` ON `Notifications` (`IdSociete`);

CREATE INDEX `IX_Paiements_DateCreation` ON `Paiements` (`DateCreation`);

CREATE INDEX `IX_Paiements_IdBilletEmis` ON `Paiements` (`IdBilletEmis`);

CREATE INDEX `IX_Paiements_IdReservation` ON `Paiements` (`IdReservation`);

CREATE INDEX `IX_Paiements_IdSite` ON `Paiements` (`IdSite`);

CREATE INDEX `IX_Paiements_IdSociete` ON `Paiements` (`IdSociete`);

CREATE INDEX `IX_Paiements_IdUtilisateur` ON `Paiements` (`IdUtilisateur`);

CREATE INDEX `IX_Paiements_Statut` ON `Paiements` (`Statut`);

CREATE INDEX `IX_PasswordResetTokens_IdUtilisateur` ON `PasswordResetTokens` (`IdUtilisateur`);

CREATE UNIQUE INDEX `IX_PasswordResetTokens_Token` ON `PasswordResetTokens` (`Token`);

CREATE INDEX `IX_PlainteClients_IdAgentAssigné` ON `PlainteClients` (`IdAgentAssigné`);

CREATE INDEX `IX_PlainteClients_IdClient` ON `PlainteClients` (`IdClient`);

CREATE INDEX `IX_PlainteClients_IdUtilisateurCreateur` ON `PlainteClients` (`IdUtilisateurCreateur`);

CREATE INDEX `IX_RefreshTokens_IdUtilisateur` ON `RefreshTokens` (`IdUtilisateur`);

CREATE INDEX `IX_ReservationPassengers_IdClient` ON `ReservationPassengers` (`IdClient`);

CREATE INDEX `IX_ReservationPassengers_IdReservation` ON `ReservationPassengers` (`IdReservation`);

CREATE INDEX `IX_ReservationPassengers_IdSociete` ON `ReservationPassengers` (`IdSociete`);

CREATE INDEX `IX_Reservations_DateReservation` ON `Reservations` (`dateReservation`);

CREATE INDEX `IX_Reservations_IdClient` ON `Reservations` (`IdClient`);

CREATE INDEX `IX_Reservations_IdSite` ON `Reservations` (`IdSite`);

CREATE INDEX `IX_Reservations_IdSociete` ON `Reservations` (`IdSociete`);

CREATE INDEX `IX_Reservations_IdUtilisateur` ON `Reservations` (`IdUtilisateur`);

CREATE INDEX `IX_Reservations_IdVoyage` ON `Reservations` (`IdVoyage`);

CREATE INDEX `IX_Reservations_StatutReservation` ON `Reservations` (`StatutReservation`);

CREATE INDEX `IX_RolePermissions_IdPermission` ON `RolePermissions` (`IdPermission`);

CREATE INDEX `IX_RolePermissions_IdRole` ON `RolePermissions` (`IdRole`);

CREATE UNIQUE INDEX `IX_Roles_Nom` ON `Roles` (`Nom`);

CREATE INDEX `IX_Sieges_IdCategorieSiege` ON `Sieges` (`IdCategorieSiege`);

CREATE INDEX `IX_Sieges_IdSociete` ON `Sieges` (`IdSociete`);

CREATE UNIQUE INDEX `IX_Sieges_Vehicule_CodeSiege_Unique` ON `Sieges` (`IdVehicule`, `CodeSiege`);

CREATE UNIQUE INDEX `IX_Sieges_Vehicule_NumeroOrdre_Unique` ON `Sieges` (`IdVehicule`, `NumeroOrdre`);

CREATE INDEX `IX_Sites_IdSociete` ON `Sites` (`IdSociete`);

CREATE UNIQUE INDEX `IX_Sites_Societe_CodeSite_Unique` ON `Sites` (`IdSociete`, `CodeSite`);

CREATE INDEX `IX_Sites_Statut` ON `Sites` (`Statut`);

CREATE INDEX `IX_Sites_Ville` ON `Sites` (`Ville`);

CREATE INDEX `IX_SmsLogs_UtilisateurIdUtilisateur` ON `SmsLogs` (`UtilisateurIdUtilisateur`);

CREATE INDEX `IX_TypeVehicules_IdSociete` ON `TypeVehicules` (`IdSociete`);

CREATE INDEX `IX_TypeVehicules_Libelle` ON `TypeVehicules` (`Libelle`);

CREATE UNIQUE INDEX `IX_TypeVehicules_Societe_Libelle_Unique` ON `TypeVehicules` (`IdSociete`, `Libelle`);

CREATE INDEX `IX_UserDevices_IdUtilisateur` ON `UserDevices` (`IdUtilisateur`);

CREATE INDEX `IX_UserPermissions_AttribueParIdUtilisateur` ON `UserPermissions` (`AttribueParIdUtilisateur`);

CREATE INDEX `IX_UserPermissions_IdPermission` ON `UserPermissions` (`IdPermission`);

CREATE INDEX `IX_UserPermissions_IdUtilisateur` ON `UserPermissions` (`IdUtilisateur`);

CREATE INDEX `IX_UserRole_IdRole` ON `UserRoles` (`IdRole`);

CREATE INDEX `IX_UserRole_IdUtilisateur` ON `UserRoles` (`IdUtilisateur`);

CREATE UNIQUE INDEX `IX_UserRole_Utilisateur_Role_Unique` ON `UserRoles` (`IdUtilisateur`, `IdRole`);

CREATE INDEX `IX_UserRole_Utilisateur_Statut` ON `UserRoles` (`IdUtilisateur`, `Statut`);

CREATE UNIQUE INDEX `IX_Utilisateurs_Email_Unique` ON `Utilisateurs` (`Email`);

CREATE INDEX `IX_Utilisateurs_IdAgent` ON `Utilisateurs` (`IdAgent`);

CREATE INDEX `IX_Utilisateurs_IdClient` ON `Utilisateurs` (`IdClient`);

CREATE INDEX `IX_Utilisateurs_IdRole` ON `Utilisateurs` (`IdRole`);

CREATE INDEX `IX_Utilisateurs_IdSite` ON `Utilisateurs` (`IdSite`);

CREATE INDEX `IX_Utilisateurs_IdSociete` ON `Utilisateurs` (`IdSociete`);

CREATE INDEX `IX_Vehicules_IdSociete` ON `Vehicules` (`IdSociete`);

CREATE INDEX `IX_Vehicules_IdTypeVehicule` ON `Vehicules` (`IdTypeVehicule`);

CREATE UNIQUE INDEX `IX_Vehicules_Societe_AliasVehicule_Unique` ON `Vehicules` (`IdSociete`, `AliasVehicule`);

CREATE INDEX `IX_VoyageDestinations_IdDestination` ON `VoyageDestinations` (`IdDestination`);

CREATE INDEX `IX_VoyageDestinations_IdSociete` ON `VoyageDestinations` (`IdSociete`);

CREATE UNIQUE INDEX `IX_VoyageDestinations_Voyage_Ordre_Unique` ON `VoyageDestinations` (`IdVoyage`, `Ordre`);

CREATE INDEX `IX_Voyages_DateDepart` ON `Voyages` (`date_depart`);

CREATE INDEX `IX_Voyages_IdDestination` ON `Voyages` (`IdDestination`);

CREATE INDEX `IX_Voyages_IdSociete` ON `Voyages` (`IdSociete`);

CREATE INDEX `IX_Voyages_IdVehicule` ON `Voyages` (`IdVehicule`);

CREATE INDEX `IX_VoyageSeatAllocations_IdSiege` ON `VoyageSeatAllocations` (`IdSiege`);

CREATE INDEX `IX_VoyageSeatAllocations_IdVoyage` ON `VoyageSeatAllocations` (`IdVoyage`);

CREATE UNIQUE INDEX `IX_VoyageSeatAllocations_ReservationPassenger_Unique` ON `VoyageSeatAllocations` (`IdReservationPassenger`);

CREATE UNIQUE INDEX `IX_VoyageSeatAllocations_Voyage_Siege_Unique` ON `VoyageSeatAllocations` (`IdVoyage`, `IdSiege`);

CREATE INDEX `IX_VoyageTarifCategorieSieges_IdSociete` ON `VoyageTarifsCategorieSiege` (`IdSociete`);

CREATE UNIQUE INDEX `IX_VoyageTarifCategorieSieges_Voyage_Categorie_Unique` ON `VoyageTarifsCategorieSiege` (`IdVoyage`, `IdCategorieSiege`);

CREATE INDEX `IX_VoyageTarifsCategorieSiege_IdCategorieSiege` ON `VoyageTarifsCategorieSiege` (`IdCategorieSiege`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260507163135_InitialMigration', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Voyages` ADD `IdSite` int NULL;

CREATE INDEX `IX_Voyages_IdSite` ON `Voyages` (`IdSite`);

ALTER TABLE `Voyages` ADD CONSTRAINT `FK_Voyages_Sites_IdSite` FOREIGN KEY (`IdSite`) REFERENCES `Sites` (`IdSite`) ON DELETE RESTRICT;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260508131304_AddIdSiteToVoyages', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Societes` ADD `CodeDevisePrincipale` varchar(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF';

ALTER TABLE `Paiements` ADD `CodeDevisePaiement` varchar(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF';

ALTER TABLE `Paiements` ADD `CodeDevisePrincipale` varchar(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF';

ALTER TABLE `Paiements` ADD `DatePaiement` datetime(6) NOT NULL DEFAULT '0001-01-01 00:00:00';

ALTER TABLE `Paiements` ADD `MontantAPayeDevisePrincipale` decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE `Paiements` ADD `MontantPayeDevisePrincipale` decimal(18,2) NULL;

ALTER TABLE `Paiements` ADD `ResteAPayeDevisePrincipale` decimal(18,2) NULL;

ALTER TABLE `Paiements` ADD `TauxVersDevisePrincipale` decimal(18,8) NOT NULL DEFAULT 1.0;

CREATE TABLE `DevisesMonetaires` (
    `IdDeviseMonetaire` int NOT NULL AUTO_INCREMENT,
    `CodeDevise` varchar(3) CHARACTER SET utf8mb4 NOT NULL,
    `Libelle` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
    `Symbole` varchar(10) CHARACTER SET utf8mb4 NULL,
    `Statut` tinyint(1) NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_DevisesMonetaires` PRIMARY KEY (`IdDeviseMonetaire`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `TauxChanges` (
    `IdTauxChange` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `CodeDeviseSource` varchar(3) CHARACTER SET utf8mb4 NOT NULL,
    `CodeDeviseCible` varchar(3) CHARACTER SET utf8mb4 NOT NULL,
    `Taux` decimal(18,8) NOT NULL,
    `DateEffet` datetime(6) NOT NULL,
    `Statut` tinyint(1) NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_TauxChanges` PRIMARY KEY (`IdTauxChange`),
    CONSTRAINT `FK_TauxChanges_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

INSERT INTO `DevisesMonetaires` (`IdDeviseMonetaire`, `CodeDevise`, `DateCreation`, `DateModification`, `Libelle`, `Statut`, `Symbole`)
VALUES (1, 'CDF', TIMESTAMP '2026-01-01 00:00:00', NULL, 'Franc congolais', TRUE, 'FC');

INSERT INTO `DevisesMonetaires` (`IdDeviseMonetaire`, `CodeDevise`, `DateCreation`, `DateModification`, `Libelle`, `Statut`, `Symbole`)
VALUES (2, 'USD', TIMESTAMP '2026-01-01 00:00:00', NULL, 'Dollar americain', TRUE, '$');

CREATE INDEX `IX_Paiements_Societe_DevisePaiement_DatePaiement` ON `Paiements` (`IdSociete`, `CodeDevisePaiement`, `DatePaiement`);

CREATE UNIQUE INDEX `IX_DevisesMonetaires_CodeDevise_Unique` ON `DevisesMonetaires` (`CodeDevise`);

CREATE INDEX `IX_TauxChanges_Societe_Paire_DateEffet` ON `TauxChanges` (`IdSociete`, `CodeDeviseSource`, `CodeDeviseCible`, `DateEffet`);


                UPDATE Societes
                SET CodeDevisePrincipale = 'CDF'
                WHERE CodeDevisePrincipale IS NULL OR CodeDevisePrincipale = '';
            


                UPDATE Paiements p
                INNER JOIN Societes s ON s.IdSociete = p.IdSociete
                SET
                    p.CodeDevisePaiement = CASE
                        WHEN p.CodeDevisePaiement IS NULL OR p.CodeDevisePaiement = '' THEN 'CDF'
                        ELSE p.CodeDevisePaiement
                    END,
                    p.CodeDevisePrincipale = CASE
                        WHEN p.CodeDevisePrincipale IS NULL OR p.CodeDevisePrincipale = '' THEN COALESCE(NULLIF(s.CodeDevisePrincipale, ''), 'CDF')
                        ELSE p.CodeDevisePrincipale
                    END,
                    p.TauxVersDevisePrincipale = CASE
                        WHEN p.TauxVersDevisePrincipale IS NULL OR p.TauxVersDevisePrincipale = 0 THEN 1
                        ELSE p.TauxVersDevisePrincipale
                    END,
                    p.MontantAPayeDevisePrincipale = CASE
                        WHEN p.MontantAPayeDevisePrincipale IS NULL OR p.MontantAPayeDevisePrincipale = 0 THEN p.MontantAPaye
                        ELSE p.MontantAPayeDevisePrincipale
                    END,
                    p.MontantPayeDevisePrincipale = CASE
                        WHEN p.MontantPaye IS NULL THEN NULL
                        WHEN p.MontantPayeDevisePrincipale IS NULL OR p.MontantPayeDevisePrincipale = 0 THEN p.MontantPaye
                        ELSE p.MontantPayeDevisePrincipale
                    END,
                    p.ResteAPayeDevisePrincipale = CASE
                        WHEN p.ResteAPaye IS NULL THEN NULL
                        WHEN p.ResteAPayeDevisePrincipale IS NULL OR p.ResteAPayeDevisePrincipale = 0 THEN p.ResteAPaye
                        ELSE p.ResteAPayeDevisePrincipale
                    END,
                    p.DatePaiement = CASE
                        WHEN p.DatePaiement = '0001-01-01 00:00:00' THEN p.DateCreation
                        ELSE p.DatePaiement
                    END;
            

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260508135505_MultiDevisePhase1', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Voyages` ADD `CodeDevisePrincipale` varchar(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF';

ALTER TABLE `Voyages` ADD `CodeDevisePrix` varchar(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF';

ALTER TABLE `Voyages` ADD `PrixDevisePrincipale` decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE `Voyages` ADD `TauxVersDevisePrincipale` decimal(18,8) NOT NULL DEFAULT 1.0;

CREATE TABLE `Remboursements` (
    `IdRemboursement` int NOT NULL AUTO_INCREMENT,
    `IdPaiement` int NOT NULL,
    `IdSociete` int NOT NULL,
    `IdUtilisateur` int NOT NULL,
    `CodeDeviseRemboursement` varchar(3) CHARACTER SET utf8mb4 NOT NULL,
    `CodeDevisePrincipale` varchar(3) CHARACTER SET utf8mb4 NOT NULL,
    `MontantRembourse` decimal(18,2) NOT NULL,
    `TauxVersDevisePrincipale` decimal(18,8) NOT NULL,
    `MontantRembourseDevisePrincipale` decimal(18,2) NOT NULL,
    `DateRemboursement` datetime(6) NOT NULL,
    `Motif` varchar(250) CHARACTER SET utf8mb4 NULL,
    `Statut` tinyint(1) NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    CONSTRAINT `PK_Remboursements` PRIMARY KEY (`IdRemboursement`),
    CONSTRAINT `FK_Remboursements_Paiements_IdPaiement` FOREIGN KEY (`IdPaiement`) REFERENCES `Paiements` (`IdPaiement`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_Voyages_Societe_DevisePrix_Date` ON `Voyages` (`IdSociete`, `CodeDevisePrix`, `date_depart`);

CREATE INDEX `IX_Remboursements_IdPaiement` ON `Remboursements` (`IdPaiement`);

CREATE INDEX `IX_Remboursements_Societe_Date` ON `Remboursements` (`IdSociete`, `DateRemboursement`);


                UPDATE Voyages v
                LEFT JOIN Societes s ON s.IdSociete = v.IdSociete
                SET
                    v.CodeDevisePrix = CASE
                        WHEN v.CodeDevisePrix IS NULL OR v.CodeDevisePrix = '' THEN 'CDF'
                        ELSE v.CodeDevisePrix
                    END,
                    v.CodeDevisePrincipale = CASE
                        WHEN v.CodeDevisePrincipale IS NULL OR v.CodeDevisePrincipale = '' THEN COALESCE(NULLIF(s.CodeDevisePrincipale, ''), 'CDF')
                        ELSE v.CodeDevisePrincipale
                    END,
                    v.TauxVersDevisePrincipale = CASE
                        WHEN v.TauxVersDevisePrincipale IS NULL OR v.TauxVersDevisePrincipale = 0 THEN 1
                        ELSE v.TauxVersDevisePrincipale
                    END,
                    v.PrixDevisePrincipale = CASE
                        WHEN v.PrixDevisePrincipale IS NULL OR v.PrixDevisePrincipale = 0 THEN v.Prix
                        ELSE v.PrixDevisePrincipale
                    END;
            

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260508141208_VoyageDeviseAndReportingPhase23', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `DevisesMonetaires` ADD `IdSociete` int NULL;

CREATE INDEX `IX_DevisesMonetaires_IdSociete` ON `DevisesMonetaires` (`IdSociete`);

ALTER TABLE `DevisesMonetaires` ADD CONSTRAINT `FK_DevisesMonetaires_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260508151940_AddIdSocieteToDevisesMonetaires', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `DevisesMonetaires` DROP INDEX `IX_DevisesMonetaires_CodeDevise_Unique`;

CREATE UNIQUE INDEX `IX_DevisesMonetaires_Societe_CodeDevise_Unique` ON `DevisesMonetaires` (`IdSociete`, `CodeDevise`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260508152532_AddUniqueDeviseBySociete', '6.0.25');

COMMIT;

START TRANSACTION;

CREATE TABLE `PhotoVehicules` (
    `IdPhotoVehicule` int NOT NULL AUTO_INCREMENT,
    `IdVehicule` int NOT NULL,
    `FilePath` varchar(500) CHARACTER SET utf8mb4 NOT NULL,
    `Ordre` int NOT NULL,
    `OriginalFileName` varchar(100) CHARACTER SET utf8mb4 NULL,
    `TypeMIME` varchar(50) CHARACTER SET utf8mb4 NULL,
    `FileSize` bigint NULL,
    `Statut` tinyint(1) NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_PhotoVehicules` PRIMARY KEY (`IdPhotoVehicule`),
    CONSTRAINT `FK_PhotoVehicules_Vehicules_IdVehicule` FOREIGN KEY (`IdVehicule`) REFERENCES `Vehicules` (`IdVehicule`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_PhotoVehicules_IdVehicule` ON `PhotoVehicules` (`IdVehicule`);

CREATE UNIQUE INDEX `IX_PhotoVehicules_Vehicule_Ordre_Unique` ON `PhotoVehicules` (`IdVehicule`, `Ordre`);


                INSERT INTO PhotoVehicules (IdVehicule, FilePath, Ordre, Statut, DateCreation, TypeMIME)
                SELECT IdVehicule, Photo, 1, 1, NOW(), 'image/jpeg'
                FROM Vehicules
                WHERE Photo IS NOT NULL AND TRIM(Photo) <> '';
            

ALTER TABLE `Vehicules` DROP COLUMN `Photo`;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260520071424_AddPhotoVehicules', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `PhotoVehicules` RENAME COLUMN `FilePath` TO `PhotoBase64`;

ALTER TABLE `PhotoVehicules` MODIFY COLUMN `PhotoBase64` longtext CHARACTER SET utf8mb4 NOT NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260520072606_PhotoVehiculeRenameFilePathToPhotoBase64', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `PhotoVehicules` ADD `PhotoData` mediumblob NULL;


                UPDATE PhotoVehicules
                SET PhotoData = FROM_BASE64(PhotoBase64)
                WHERE PhotoBase64 IS NOT NULL AND TRIM(PhotoBase64) <> '';
            

ALTER TABLE `PhotoVehicules` MODIFY COLUMN `PhotoData` mediumblob NOT NULL;

ALTER TABLE `PhotoVehicules` DROP COLUMN `PhotoBase64`;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260520083546_PhotoVehiculePhotoDataMediumBlob', '6.0.25');

COMMIT;

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

START TRANSACTION;

ALTER TABLE `Voyages` ADD `DureeValiditeBilletJours` int NOT NULL DEFAULT 0;

ALTER TABLE `Billets` ADD `DateValiditeDebut` datetime(6) NULL;

ALTER TABLE `Billets` ADD `DateValiditeFin` datetime(6) NULL;


                UPDATE Billets b
                INNER JOIN Reservations r ON r.IdReservation = b.IdReservation
                INNER JOIN Voyages v ON v.Id = r.IdVoyage
                SET
                    b.DateValiditeDebut = COALESCE(b.DateValiditeDebut, DATE(v.date_depart)),
                    b.DateValiditeFin = COALESCE(b.DateValiditeFin, DATE_ADD(DATE(v.date_depart), INTERVAL GREATEST(v.DureeValiditeBilletJours, 0) DAY))
                WHERE b.IdReservation IS NOT NULL;
            

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260528110345_BilletValiditeMultiVoyages', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Voyages` ADD `PenaliteReaffectation` decimal(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE `Billets` ADD `PenaliteOverride` decimal(18,2) NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260528113255_PenaliteReaffectationBillet', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Voyages` ADD `HeuresLimiteReaffectation` int NOT NULL DEFAULT 2;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260528124139_LimiteReaffectationVoyage', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Sites` ADD `IsSitePrincipal` tinyint(1) NOT NULL DEFAULT FALSE;

CREATE INDEX `IX_Sites_IdSociete_IsSitePrincipal` ON `Sites` (`IdSociete`, `IsSitePrincipal`);


UPDATE Sites SET IsSitePrincipal = 0;

UPDATE Sites s
INNER JOIN (
    SELECT ips.IdSociete, MIN(ips.IdSite) AS IdSite
    FROM InfoPaiementsSociete ips
    INNER JOIN Sites st ON st.IdSite = ips.IdSite AND st.IdSociete = ips.IdSociete
    WHERE ips.Statut = 1
    GROUP BY ips.IdSociete
) pick ON s.IdSociete = pick.IdSociete AND s.IdSite = pick.IdSite
SET s.IsSitePrincipal = 1;

UPDATE Sites s
INNER JOIN (
    SELECT st.IdSociete, MIN(st.IdSite) AS IdSite
    FROM Sites st
    WHERE st.Statut = 1
      AND NOT EXISTS (
          SELECT 1 FROM Sites p
          WHERE p.IdSociete = st.IdSociete AND p.IsSitePrincipal = 1)
    GROUP BY st.IdSociete
) pick ON s.IdSociete = pick.IdSociete AND s.IdSite = pick.IdSite
SET s.IsSitePrincipal = 1;

UPDATE Sites s
INNER JOIN (
    SELECT st.IdSociete, MIN(st.IdSite) AS IdSite
    FROM Sites st
    WHERE NOT EXISTS (
          SELECT 1 FROM Sites p
          WHERE p.IdSociete = st.IdSociete AND p.IsSitePrincipal = 1)
    GROUP BY st.IdSociete
) pick ON s.IdSociete = pick.IdSociete AND s.IdSite = pick.IdSite
SET s.IsSitePrincipal = 1;


INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260530075224_SiteIsSitePrincipal', '6.0.25');

COMMIT;

START TRANSACTION;

CREATE TABLE `ConfigSocietes` (
    `IdConfigSociete` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `DureeValiditeBilletJours` int NOT NULL DEFAULT 0,
    `PenaliteReaffectation` decimal(18,2) NOT NULL DEFAULT 0.0,
    `JoursAvanceMaxReservation` int NULL,
    `HeuresLimiteReaffectation` int NOT NULL DEFAULT 2,
    `HeuresOuvertureEmbarquementAvantDepart` int NOT NULL DEFAULT 3,
    `HeuresFermetureEmbarquementApresJourDepart` int NOT NULL DEFAULT 24,
    `DureeHoldFlexPayMinutes` int NOT NULL DEFAULT 15,
    `ReaffectationActive` tinyint(1) NOT NULL DEFAULT TRUE,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_ConfigSocietes` PRIMARY KEY (`IdConfigSociete`),
    CONSTRAINT `FK_ConfigSocietes_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE UNIQUE INDEX `IX_ConfigSociete_IdSociete_Unique` ON `ConfigSocietes` (`IdSociete`);


INSERT INTO ConfigSocietes (
    IdSociete,
    DureeValiditeBilletJours,
    PenaliteReaffectation,
    JoursAvanceMaxReservation,
    HeuresLimiteReaffectation,
    HeuresOuvertureEmbarquementAvantDepart,
    HeuresFermetureEmbarquementApresJourDepart,
    DureeHoldFlexPayMinutes,
    ReaffectationActive,
    DateCreation
)
SELECT
    s.IdSociete,
    COALESCE(v.DureeValiditeBilletJours, 0),
    COALESCE(v.PenaliteReaffectation, 0),
    NULL,
    COALESCE(v.HeuresLimiteReaffectation, 2),
    3,
    24,
    15,
    1,
    UTC_TIMESTAMP(6)
FROM Societes s
LEFT JOIN (
    SELECT v1.IdSociete,
           v1.DureeValiditeBilletJours,
           v1.PenaliteReaffectation,
           v1.HeuresLimiteReaffectation
    FROM Voyages v1
    INNER JOIN (
        SELECT v2.IdSociete, MAX(v2.Id) AS IdVoyageRetenu
        FROM Voyages v2
        INNER JOIN (
            SELECT IdSociete, MAX(DateCreation) AS MaxDateCreation
            FROM Voyages
            GROUP BY IdSociete
        ) m ON m.IdSociete = v2.IdSociete AND v2.DateCreation = m.MaxDateCreation
        GROUP BY v2.IdSociete
    ) pick ON pick.IdVoyageRetenu = v1.Id
) v ON v.IdSociete = s.IdSociete;


ALTER TABLE `Voyages` DROP COLUMN `DureeValiditeBilletJours`;

ALTER TABLE `Voyages` DROP COLUMN `HeuresLimiteReaffectation`;

ALTER TABLE `Voyages` DROP COLUMN `PenaliteReaffectation`;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260530094931_ConfigSocieteCentralizedRules', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `ConfigSocietes` RENAME COLUMN `PenaliteReaffectation` TO `PenaliteReaffectationPourcentage`;

UPDATE `ConfigSocietes` SET `PenaliteReaffectationPourcentage` = 0 WHERE `PenaliteReaffectationPourcentage` <> 0;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260530121511_ConfigSocietePenalitePourcentage', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Voyages` ADD `IdPlanificationVoyage` int NULL;

CREATE TABLE `PlanificationsVoyage` (
    `IdPlanificationVoyage` int NOT NULL AUTO_INCREMENT,
    `Libelle` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `IdSociete` int NOT NULL,
    `IdSite` int NOT NULL,
    `IdVehicule` int NOT NULL,
    `HeureDepart` time(6) NOT NULL,
    `Prix` int NOT NULL,
    `CodeDevisePrix` varchar(3) CHARACTER SET utf8mb4 NOT NULL,
    `JoursSemaine` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Statut` tinyint(1) NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_PlanificationsVoyage` PRIMARY KEY (`IdPlanificationVoyage`),
    CONSTRAINT `FK_PlanificationsVoyage_Sites_IdSite` FOREIGN KEY (`IdSite`) REFERENCES `Sites` (`IdSite`) ON DELETE RESTRICT,
    CONSTRAINT `FK_PlanificationsVoyage_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT,
    CONSTRAINT `FK_PlanificationsVoyage_Vehicules_IdVehicule` FOREIGN KEY (`IdVehicule`) REFERENCES `Vehicules` (`IdVehicule`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `PlanificationGenerationLogs` (
    `IdPlanificationGenerationLog` int NOT NULL AUTO_INCREMENT,
    `IdPlanificationVoyage` int NOT NULL,
    `DateDebut` datetime(6) NOT NULL,
    `DateFin` datetime(6) NOT NULL,
    `NombreCrees` int NOT NULL,
    `NombreIgnores` int NOT NULL,
    `NombreEchecs` int NOT NULL,
    `DetailsJson` longtext CHARACTER SET utf8mb4 NOT NULL,
    `DeclencheParIdUtilisateur` int NULL,
    `DateCreation` datetime(6) NOT NULL,
    CONSTRAINT `PK_PlanificationGenerationLogs` PRIMARY KEY (`IdPlanificationGenerationLog`),
    CONSTRAINT `FK_PlanificationGenerationLogs_PlanificationsVoyage_IdPlanifica~` FOREIGN KEY (`IdPlanificationVoyage`) REFERENCES `PlanificationsVoyage` (`IdPlanificationVoyage`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `PlanificationVoyageEtapes` (
    `IdPlanificationVoyageEtape` int NOT NULL AUTO_INCREMENT,
    `IdPlanificationVoyage` int NOT NULL,
    `IdDestination` int NOT NULL,
    `Ordre` int NOT NULL,
    `IdSociete` int NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    CONSTRAINT `PK_PlanificationVoyageEtapes` PRIMARY KEY (`IdPlanificationVoyageEtape`),
    CONSTRAINT `FK_PlanificationVoyageEtapes_Destinations_IdDestination` FOREIGN KEY (`IdDestination`) REFERENCES `Destinations` (`IdDestination`) ON DELETE RESTRICT,
    CONSTRAINT `FK_PlanificationVoyageEtapes_PlanificationsVoyage_IdPlanificati~` FOREIGN KEY (`IdPlanificationVoyage`) REFERENCES `PlanificationsVoyage` (`IdPlanificationVoyage`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `PlanificationVoyageTarifs` (
    `IdPlanificationVoyageTarif` int NOT NULL AUTO_INCREMENT,
    `IdPlanificationVoyage` int NOT NULL,
    `IdCategorieSiege` int NOT NULL,
    `Prix` int NOT NULL,
    `IdSociete` int NOT NULL,
    `DateCreation` datetime(6) NOT NULL,
    CONSTRAINT `PK_PlanificationVoyageTarifs` PRIMARY KEY (`IdPlanificationVoyageTarif`),
    CONSTRAINT `FK_PlanificationVoyageTarifs_CategorieSieges_IdCategorieSiege` FOREIGN KEY (`IdCategorieSiege`) REFERENCES `CategorieSieges` (`IdCategorieSiege`) ON DELETE RESTRICT,
    CONSTRAINT `FK_PlanificationVoyageTarifs_PlanificationsVoyage_IdPlanificati~` FOREIGN KEY (`IdPlanificationVoyage`) REFERENCES `PlanificationsVoyage` (`IdPlanificationVoyage`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_Voyages_IdPlanificationVoyage` ON `Voyages` (`IdPlanificationVoyage`);

CREATE INDEX `IX_PlanificationGenerationLogs_IdPlanificationVoyage` ON `PlanificationGenerationLogs` (`IdPlanificationVoyage`);

CREATE INDEX `IX_PlanificationsVoyage_IdSite` ON `PlanificationsVoyage` (`IdSite`);

CREATE INDEX `IX_PlanificationsVoyage_IdSociete` ON `PlanificationsVoyage` (`IdSociete`);

CREATE INDEX `IX_PlanificationsVoyage_IdVehicule` ON `PlanificationsVoyage` (`IdVehicule`);

CREATE INDEX `IX_PlanificationVoyageEtapes_IdDestination` ON `PlanificationVoyageEtapes` (`IdDestination`);

CREATE UNIQUE INDEX `IX_PlanificationVoyageEtapes_Planif_Ordre_Unique` ON `PlanificationVoyageEtapes` (`IdPlanificationVoyage`, `Ordre`);

CREATE INDEX `IX_PlanificationVoyageTarifs_IdCategorieSiege` ON `PlanificationVoyageTarifs` (`IdCategorieSiege`);

CREATE UNIQUE INDEX `IX_PlanificationVoyageTarifs_Planif_Categorie_Unique` ON `PlanificationVoyageTarifs` (`IdPlanificationVoyage`, `IdCategorieSiege`);

ALTER TABLE `Voyages` ADD CONSTRAINT `FK_Voyages_PlanificationsVoyage_IdPlanificationVoyage` FOREIGN KEY (`IdPlanificationVoyage`) REFERENCES `PlanificationsVoyage` (`IdPlanificationVoyage`) ON DELETE SET NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260531142422_PlanificationVoyageV1', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Reservations` ADD `Origine` varchar(20) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'INCONNU';

ALTER TABLE `Paiements` ADD `Origine` varchar(20) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'INCONNU';

ALTER TABLE `CommandesReservationEnAttente` ADD `Origine` varchar(20) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'INCONNU';

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260608101418_OrigineOperationReservationPaiement', '6.0.25');

COMMIT;

START TRANSACTION;

CREATE UNIQUE INDEX `IX_Destinations_Societe_Villes_Unique` ON `Destinations` (`IdSociete`, `VilleDepart`, `VilleArrivee`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260615121938_DestinationSocieteVillesUnique', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `Sites` ADD `NumeroMobileMoney` varchar(30) CHARACTER SET utf8mb4 NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260618112928_SiteNumeroMobileMoney', '6.0.25');

COMMIT;

START TRANSACTION;

CREATE TABLE `ReversementsSite` (
    `IdReversementSite` int NOT NULL AUTO_INCREMENT,
    `IdSite` int NOT NULL,
    `IdSociete` int NOT NULL,
    `IdUtilisateur` int NOT NULL,
    `NumeroMobileMoney` varchar(30) CHARACTER SET utf8mb4 NOT NULL,
    `Montant` decimal(18,2) NOT NULL,
    `CodeDevise` varchar(10) CHARACTER SET utf8mb4 NOT NULL,
    `Reference` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `OrderNumber` varchar(100) CHARACTER SET utf8mb4 NULL,
    `ProviderReference` varchar(100) CHARACTER SET utf8mb4 NULL,
    `CodeMarchand` varchar(100) CHARACTER SET utf8mb4 NULL,
    `Statut` int NOT NULL,
    `CodeFlexPay` varchar(10) CHARACTER SET utf8mb4 NULL,
    `MessageFlexPay` varchar(500) CHARACTER SET utf8mb4 NULL,
    `Channel` varchar(50) CHARACTER SET utf8mb4 NULL,
    `Motif` varchar(500) CHARACTER SET utf8mb4 NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateCallback` datetime(6) NULL,
    CONSTRAINT `PK_ReversementsSite` PRIMARY KEY (`IdReversementSite`),
    CONSTRAINT `FK_ReversementsSite_Sites_IdSite` FOREIGN KEY (`IdSite`) REFERENCES `Sites` (`IdSite`) ON DELETE RESTRICT,
    CONSTRAINT `FK_ReversementsSite_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE UNIQUE INDEX `IX_ReversementSite_OrderNumber` ON `ReversementsSite` (`OrderNumber`);

CREATE INDEX `IX_ReversementSite_Societe_Site_Date` ON `ReversementsSite` (`IdSociete`, `IdSite`, `DateCreation`);

CREATE INDEX `IX_ReversementsSite_IdSite` ON `ReversementsSite` (`IdSite`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260618124839_ReversementSiteFlexPayPayOut', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `ReversementsSite` ADD `IdPaiement` int NULL;

ALTER TABLE `ReversementsSite` ADD `IdReservation` int NULL;

ALTER TABLE `ReversementsSite` ADD `Origine` varchar(30) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Manuel';

ALTER TABLE `ConfigSocietes` ADD `AutoReversementPaiementElectronique` tinyint(1) NOT NULL DEFAULT FALSE;

CREATE UNIQUE INDEX `IX_ReversementSite_IdPaiement` ON `ReversementsSite` (`IdPaiement`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260618133404_ReversementAutoPaiementElectronique', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `ConfigSocietes` ADD `PourcentageReversementSite` decimal(18,2) NOT NULL DEFAULT 100.0;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260618134551_PourcentageReversementSiteConfig', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `ConfigSocietes` ADD `CodeDeviseFraisPlateforme` varchar(3) CHARACTER SET utf8mb4 NULL;

ALTER TABLE `ConfigSocietes` ADD `FraisPlateforme` decimal(18,2) NOT NULL DEFAULT 0.0;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260618135910_FraisPlateformeConfig', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `ConfigSocietes` ADD `CodeDeviseMontAddPaieElectronique` varchar(3) CHARACTER SET utf8mb4 NULL;

ALTER TABLE `ConfigSocietes` ADD `MontAddPaieElectronique` decimal(18,2) NOT NULL DEFAULT 0.0;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260618171505_MontAddPaieElectroniqueConfig', '6.0.25');

COMMIT;

START TRANSACTION;

UPDATE Clients SET AdresseClient = NULL WHERE AdresseClient IS NOT NULL AND TRIM(AdresseClient) = '';

ALTER TABLE `Clients` MODIFY COLUMN `AdresseClient` varchar(500) CHARACTER SET utf8mb4 NULL;

ALTER TABLE `Agents` MODIFY COLUMN `TelephoneAgent` varchar(200) CHARACTER SET utf8mb4 NULL;

ALTER TABLE `Agents` MODIFY COLUMN `EmailAgent` varchar(200) CHARACTER SET utf8mb4 NULL;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260619134037_ClientAdresseClientOptional', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `ConfigSocietes` ADD `DureeHoldEvenementMinutes` int NOT NULL DEFAULT 15;

CREATE TABLE `EvenementClasses` (
    `IdEvenementClasse` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `CodeClasse` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Libelle` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
    `Description` varchar(500) CHARACTER SET utf8mb4 NULL,
    `Statut` tinyint(1) NOT NULL DEFAULT TRUE,
    `DateCreation` datetime(6) NOT NULL,
    CONSTRAINT `PK_EvenementClasses` PRIMARY KEY (`IdEvenementClasse`),
    CONSTRAINT `FK_EvenementClasses_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `EvenementSessions` (
    `IdEvenementSession` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `CodeSession` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `Libelle` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `StartAtUtc` datetime(6) NOT NULL,
    `EndAtUtc` datetime(6) NULL,
    `InventoryMode` enum('SeatNumbered','ClassQuota','GlobalQuota') CHARACTER SET utf8mb4 NOT NULL,
    `Status` enum('Draft','Published','Closed','Cancelled') CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Draft',
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_EvenementSessions` PRIMARY KEY (`IdEvenementSession`),
    CONSTRAINT `CK_EvenementSessions_StartEnd` CHECK (`EndAtUtc` IS NULL OR `EndAtUtc` >= `StartAtUtc`),
    CONSTRAINT `FK_EvenementSessions_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `EvenementReservations` (
    `IdEvenementReservation` int NOT NULL AUTO_INCREMENT,
    `IdSociete` int NOT NULL,
    `IdEvenementSession` int NOT NULL,
    `ReferenceReservation` varchar(64) CHARACTER SET utf8mb4 NOT NULL,
    `CustomerRef` varchar(100) CHARACTER SET utf8mb4 NULL,
    `Status` enum('HOLD','CONFIRMED','CANCELLED','EXPIRED') CHARACTER SET utf8mb4 NOT NULL,
    `ExpiresAtUtc` datetime(6) NULL,
    `MontantSousTotal` decimal(18,2) NOT NULL DEFAULT 0.0,
    `CodeDevise` char(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF',
    `IdempotencyKey` varchar(120) CHARACTER SET utf8mb4 NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_EvenementReservations` PRIMARY KEY (`IdEvenementReservation`),
    CONSTRAINT `FK_EvenementReservations_EvenementSessions_IdEvenementSession` FOREIGN KEY (`IdEvenementSession`) REFERENCES `EvenementSessions` (`IdEvenementSession`) ON DELETE RESTRICT,
    CONSTRAINT `FK_EvenementReservations_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `EvenementSessionClassQuotas` (
    `IdEvenementSessionClassQuota` int NOT NULL AUTO_INCREMENT,
    `IdEvenementSession` int NOT NULL,
    `IdEvenementClasse` int NOT NULL,
    `CapaciteTotale` int NOT NULL,
    `QuantiteHold` int NOT NULL DEFAULT 0,
    `QuantiteVendue` int NOT NULL DEFAULT 0,
    `PrixUnitaire` decimal(18,2) NOT NULL,
    `CodeDevise` char(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF',
    CONSTRAINT `PK_EvenementSessionClassQuotas` PRIMARY KEY (`IdEvenementSessionClassQuota`),
    CONSTRAINT `CK_EvenementSessionClassQuotas_Capacite` CHECK (`CapaciteTotale` >= 0),
    CONSTRAINT `CK_EvenementSessionClassQuotas_StockMax` CHECK (`QuantiteHold` + `QuantiteVendue` <= `CapaciteTotale`),
    CONSTRAINT `CK_EvenementSessionClassQuotas_StockPositive` CHECK (`QuantiteHold` >= 0 AND `QuantiteVendue` >= 0),
    CONSTRAINT `FK_EvenementSessionClassQuotas_EvenementClasses_IdEvenementClas~` FOREIGN KEY (`IdEvenementClasse`) REFERENCES `EvenementClasses` (`IdEvenementClasse`) ON DELETE RESTRICT,
    CONSTRAINT `FK_EvenementSessionClassQuotas_EvenementSessions_IdEvenementSes~` FOREIGN KEY (`IdEvenementSession`) REFERENCES `EvenementSessions` (`IdEvenementSession`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `EvenementSessionGlobalQuotas` (
    `IdEvenementSession` int NOT NULL,
    `CapaciteTotale` int NOT NULL,
    `QuantiteHold` int NOT NULL DEFAULT 0,
    `QuantiteVendue` int NOT NULL DEFAULT 0,
    CONSTRAINT `PK_EvenementSessionGlobalQuotas` PRIMARY KEY (`IdEvenementSession`),
    CONSTRAINT `CK_EvenementSessionGlobalQuotas_Capacite` CHECK (`CapaciteTotale` >= 0),
    CONSTRAINT `CK_EvenementSessionGlobalQuotas_StockMax` CHECK (`QuantiteHold` + `QuantiteVendue` <= `CapaciteTotale`),
    CONSTRAINT `CK_EvenementSessionGlobalQuotas_StockPositive` CHECK (`QuantiteHold` >= 0 AND `QuantiteVendue` >= 0),
    CONSTRAINT `FK_EvenementSessionGlobalQuotas_EvenementSessions_IdEvenementSe~` FOREIGN KEY (`IdEvenementSession`) REFERENCES `EvenementSessions` (`IdEvenementSession`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `EvenementSessionSections` (
    `IdEvenementSessionSection` int NOT NULL AUTO_INCREMENT,
    `IdEvenementSession` int NOT NULL,
    `CodeSection` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Libelle` varchar(120) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_EvenementSessionSections` PRIMARY KEY (`IdEvenementSessionSection`),
    CONSTRAINT `FK_EvenementSessionSections_EvenementSessions_IdEvenementSession` FOREIGN KEY (`IdEvenementSession`) REFERENCES `EvenementSessions` (`IdEvenementSession`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `EvenementPayments` (
    `IdEvenementPayment` int NOT NULL AUTO_INCREMENT,
    `IdEvenementReservation` int NOT NULL,
    `ReferencePaiement` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Provider` varchar(40) CHARACTER SET utf8mb4 NOT NULL,
    `ProviderTxRef` varchar(120) CHARACTER SET utf8mb4 NULL,
    `Status` enum('PENDING','SUCCEEDED','FAILED','REFUNDED') CHARACTER SET utf8mb4 NOT NULL,
    `Montant` decimal(18,2) NOT NULL,
    `CodeDevise` char(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF',
    `IdempotencyKey` varchar(120) CHARACTER SET utf8mb4 NULL,
    `DateCreation` datetime(6) NOT NULL,
    `DateModification` datetime(6) NULL,
    CONSTRAINT `PK_EvenementPayments` PRIMARY KEY (`IdEvenementPayment`),
    CONSTRAINT `FK_EvenementPayments_EvenementReservations_IdEvenementReservati~` FOREIGN KEY (`IdEvenementReservation`) REFERENCES `EvenementReservations` (`IdEvenementReservation`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `EvenementSessionSeats` (
    `IdEvenementSessionSeat` int NOT NULL AUTO_INCREMENT,
    `IdEvenementSession` int NOT NULL,
    `SeatCode` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `IdEvenementSessionSection` int NULL,
    `IdEvenementClasse` int NULL,
    `SeatStatus` enum('Available','Held','Sold','Blocked') CHARACTER SET utf8mb4 NOT NULL DEFAULT 'Available',
    `IdEvenementReservationCourante` int NULL,
    `HoldExpireAtUtc` datetime(6) NULL,
    `PrixUnitaire` decimal(18,2) NOT NULL,
    `CodeDevise` char(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF',
    CONSTRAINT `PK_EvenementSessionSeats` PRIMARY KEY (`IdEvenementSessionSeat`),
    CONSTRAINT `FK_EvenementSessionSeats_EvenementClasses_IdEvenementClasse` FOREIGN KEY (`IdEvenementClasse`) REFERENCES `EvenementClasses` (`IdEvenementClasse`) ON DELETE SET NULL,
    CONSTRAINT `FK_EvenementSessionSeats_EvenementReservations_IdEvenementReser~` FOREIGN KEY (`IdEvenementReservationCourante`) REFERENCES `EvenementReservations` (`IdEvenementReservation`) ON DELETE SET NULL,
    CONSTRAINT `FK_EvenementSessionSeats_EvenementSessions_IdEvenementSession` FOREIGN KEY (`IdEvenementSession`) REFERENCES `EvenementSessions` (`IdEvenementSession`) ON DELETE CASCADE,
    CONSTRAINT `FK_EvenementSessionSeats_EvenementSessionSections_IdEvenementSe~` FOREIGN KEY (`IdEvenementSessionSection`) REFERENCES `EvenementSessionSections` (`IdEvenementSessionSection`) ON DELETE SET NULL
) CHARACTER SET=utf8mb4;

CREATE TABLE `EvenementReservationLines` (
    `IdEvenementReservationLine` int NOT NULL AUTO_INCREMENT,
    `IdEvenementReservation` int NOT NULL,
    `LineType` enum('Seat','ClassQuota','GlobalQuota') CHARACTER SET utf8mb4 NOT NULL,
    `Quantite` int NOT NULL,
    `PrixUnitaire` decimal(18,2) NOT NULL,
    `CodeDevise` char(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF',
    `IdEvenementSessionSeat` int NULL,
    `IdEvenementSessionClassQuota` int NULL,
    CONSTRAINT `PK_EvenementReservationLines` PRIMARY KEY (`IdEvenementReservationLine`),
    CONSTRAINT `CK_EvenementReservationLines_Quantite` CHECK (`Quantite` > 0),
    CONSTRAINT `FK_EvenementReservationLines_EvenementReservations_IdEvenementR~` FOREIGN KEY (`IdEvenementReservation`) REFERENCES `EvenementReservations` (`IdEvenementReservation`) ON DELETE CASCADE,
    CONSTRAINT `FK_EvenementReservationLines_EvenementSessionClassQuotas_IdEven~` FOREIGN KEY (`IdEvenementSessionClassQuota`) REFERENCES `EvenementSessionClassQuotas` (`IdEvenementSessionClassQuota`) ON DELETE RESTRICT,
    CONSTRAINT `FK_EvenementReservationLines_EvenementSessionSeats_IdEvenementS~` FOREIGN KEY (`IdEvenementSessionSeat`) REFERENCES `EvenementSessionSeats` (`IdEvenementSessionSeat`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `EvenementTickets` (
    `IdEvenementTicket` int NOT NULL AUTO_INCREMENT,
    `IdEvenementReservationLine` int NOT NULL,
    `TicketCode` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Status` enum('ISSUED','USED','VOID') CHARACTER SET utf8mb4 NOT NULL DEFAULT 'ISSUED',
    `IssuedAtUtc` datetime(6) NOT NULL,
    `UsedAtUtc` datetime(6) NULL,
    CONSTRAINT `PK_EvenementTickets` PRIMARY KEY (`IdEvenementTicket`),
    CONSTRAINT `FK_EvenementTickets_EvenementReservationLines_IdEvenementReserv~` FOREIGN KEY (`IdEvenementReservationLine`) REFERENCES `EvenementReservationLines` (`IdEvenementReservationLine`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_EvenementClasses_IdSociete` ON `EvenementClasses` (`IdSociete`);

CREATE UNIQUE INDEX `IX_EvenementClasses_Societe_CodeClasse_UQ` ON `EvenementClasses` (`IdSociete`, `CodeClasse`);

CREATE UNIQUE INDEX `IX_EvenementPayments_Idempotency_UQ` ON `EvenementPayments` (`IdempotencyKey`);

CREATE UNIQUE INDEX `IX_EvenementPayments_ReferencePaiement_UQ` ON `EvenementPayments` (`ReferencePaiement`);

CREATE INDEX `IX_EvenementPayments_Reservation_Status` ON `EvenementPayments` (`IdEvenementReservation`, `Status`);

CREATE INDEX `IX_EvenementReservationLines_IdEvenementReservation` ON `EvenementReservationLines` (`IdEvenementReservation`);

CREATE INDEX `IX_EvenementReservationLines_IdEvenementSessionClassQuota` ON `EvenementReservationLines` (`IdEvenementSessionClassQuota`);

CREATE INDEX `IX_EvenementReservationLines_IdEvenementSessionSeat` ON `EvenementReservationLines` (`IdEvenementSessionSeat`);

CREATE UNIQUE INDEX `IX_EvenementReservationLines_Reservation_Seat_UQ` ON `EvenementReservationLines` (`IdEvenementReservation`, `IdEvenementSessionSeat`);

CREATE INDEX `IX_EvenementReservations_Session_Status` ON `EvenementReservations` (`IdEvenementSession`, `Status`);

CREATE UNIQUE INDEX `IX_EvenementReservations_Societe_Idempotency_UQ` ON `EvenementReservations` (`IdSociete`, `IdempotencyKey`);

CREATE UNIQUE INDEX `IX_EvenementReservations_Societe_Reference_UQ` ON `EvenementReservations` (`IdSociete`, `ReferenceReservation`);

CREATE INDEX `IX_EvenementReservations_Status_ExpiresAtUtc` ON `EvenementReservations` (`Status`, `ExpiresAtUtc`);

CREATE INDEX `IX_EvenementSessionClassQuotas_IdEvenementClasse` ON `EvenementSessionClassQuotas` (`IdEvenementClasse`);

CREATE INDEX `IX_EvenementSessionClassQuotas_IdEvenementSession` ON `EvenementSessionClassQuotas` (`IdEvenementSession`);

CREATE UNIQUE INDEX `IX_EvenementSessionClassQuotas_Session_Classe_UQ` ON `EvenementSessionClassQuotas` (`IdEvenementSession`, `IdEvenementClasse`);

CREATE INDEX `IX_EvenementSessions_IdSociete_StartAtUtc` ON `EvenementSessions` (`IdSociete`, `StartAtUtc`);

CREATE UNIQUE INDEX `IX_EvenementSessions_Societe_CodeSession_UQ` ON `EvenementSessions` (`IdSociete`, `CodeSession`);

CREATE INDEX `IX_EvenementSessionSeats_HoldExpireAtUtc` ON `EvenementSessionSeats` (`HoldExpireAtUtc`);

CREATE INDEX `IX_EvenementSessionSeats_IdEvenementClasse` ON `EvenementSessionSeats` (`IdEvenementClasse`);

CREATE INDEX `IX_EvenementSessionSeats_IdEvenementReservationCourante` ON `EvenementSessionSeats` (`IdEvenementReservationCourante`);

CREATE INDEX `IX_EvenementSessionSeats_IdEvenementSessionSection` ON `EvenementSessionSeats` (`IdEvenementSessionSection`);

CREATE UNIQUE INDEX `IX_EvenementSessionSeats_Session_SeatCode_UQ` ON `EvenementSessionSeats` (`IdEvenementSession`, `SeatCode`);

CREATE INDEX `IX_EvenementSessionSeats_Session_SeatStatus` ON `EvenementSessionSeats` (`IdEvenementSession`, `SeatStatus`);

CREATE INDEX `IX_EvenementSessionSections_IdEvenementSession` ON `EvenementSessionSections` (`IdEvenementSession`);

CREATE UNIQUE INDEX `IX_EvenementSessionSections_Session_CodeSection_UQ` ON `EvenementSessionSections` (`IdEvenementSession`, `CodeSection`);

CREATE INDEX `IX_EvenementTickets_IdEvenementReservationLine` ON `EvenementTickets` (`IdEvenementReservationLine`);

CREATE INDEX `IX_EvenementTickets_Status` ON `EvenementTickets` (`Status`);

CREATE UNIQUE INDEX `IX_EvenementTickets_TicketCode_UQ` ON `EvenementTickets` (`TicketCode`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260703101713_EvenementTicketingV1', '6.0.25');

COMMIT;

START TRANSACTION;

ALTER TABLE `EvenementSessionGlobalQuotas` ADD `CodeDevise` char(3) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'CDF';

ALTER TABLE `EvenementSessionGlobalQuotas` ADD `PrixUnitaire` decimal(18,2) NOT NULL DEFAULT 0.0;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260703120104_EvenementSessionGlobalQuotaPricing', '6.0.25');

COMMIT;

