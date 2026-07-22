-- ============================================================================
-- Script de création de la base de données CongoTravel pour la production
-- Base de données: MariaDB / MySQL
-- Version: 2.0
-- Date: Décembre 2025
-- ============================================================================
-- 
-- INSTRUCTIONS:
-- 1. Créez d'abord la base de données: CREATE DATABASE CongoTravelDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
-- 2. Utilisez la base de données: USE CongoTravelDB;
-- 3. Exécutez ce script pour créer toutes les tables
-- 
-- ============================================================================

SET FOREIGN_KEY_CHECKS = 0;
SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET AUTOCOMMIT = 0;
START TRANSACTION;
SET time_zone = "+00:00";

-- ============================================================================
-- 1. TABLE: Roles
-- ============================================================================
CREATE TABLE IF NOT EXISTS `Roles` (
  `IdRole` int NOT NULL AUTO_INCREMENT,
  `Nom` varchar(50) NOT NULL,
  `Description` varchar(255) DEFAULT NULL,
  `Niveau` int DEFAULT 5,
  `Statut` tinyint(1) DEFAULT 1,
  `DateCreation` datetime(6) NOT NULL,
  PRIMARY KEY (`IdRole`),
  UNIQUE KEY `IX_Roles_Nom` (`Nom`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================================
-- 2. TABLE: Permissions
-- ============================================================================
CREATE TABLE IF NOT EXISTS `Permissions` (
  `IdPermission` int NOT NULL AUTO_INCREMENT,
  `Nom` varchar(100) NOT NULL,
  `Description` varchar(255) DEFAULT NULL,
  `Categorie` varchar(50) NOT NULL,
  `Action` varchar(20) NOT NULL,
  `Statut` tinyint(1) DEFAULT 1,
  `DateCreation` datetime(6) NOT NULL,
  PRIMARY KEY (`IdPermission`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================================
-- 3. TABLE: Societes
-- ============================================================================
CREATE TABLE IF NOT EXISTS `Societes` (
  `IdSociete` int NOT NULL AUTO_INCREMENT,
  `Nom` varchar(150) NOT NULL,
  `Devise` longtext DEFAULT NULL,
  `Type` varchar(50) DEFAULT NULL,
  `Logo` longtext DEFAULT NULL,
  `Telephone` longtext DEFAULT NULL,
  `EmailContact` longtext DEFAULT NULL,
  `SiteWeb` longtext DEFAULT NULL,
  `NomCompletResponsable` longtext DEFAULT NULL,
  `GenreResponsable` varchar(10) DEFAULT NULL,
  `Description` longtext DEFAULT NULL,
  `Statut` tinyint(1) DEFAULT 1,
  `DateCreation` datetime(6) NOT NULL,
  `Province` longtext DEFAULT NULL,
  `Ville` longtext DEFAULT NULL,
  `Commune` longtext DEFAULT NULL,
  `Quartier` longtext DEFAULT NULL,
  `Avenue` longtext DEFAULT NULL,
  `Numero` longtext DEFAULT NULL,
  PRIMARY KEY (`IdSociete`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================================
-- 4. TABLE: Agents
-- ============================================================================
CREATE TABLE IF NOT EXISTS `Agents` (
  `IdAgent` int NOT NULL AUTO_INCREMENT,
  `Matricule` varchar(50) DEFAULT NULL,
  `NomComplet` varchar(200) DEFAULT NULL,
  `Genre` varchar(10) DEFAULT NULL,
  `DateNaissance` datetime(6) NOT NULL,
  `TelephoneAgent` longtext DEFAULT NULL,
  `EmailAgent` varchar(255) DEFAULT NULL,
  `Statut` tinyint(1) DEFAULT 1,
  `EtatCivil` varchar(20) DEFAULT NULL,
  `SerialNumber` varchar(255) DEFAULT NULL,
  `Fonction` longtext DEFAULT NULL,
  `RoleAgent` longtext DEFAULT NULL,
  `PhotoUrl` longtext DEFAULT NULL,
  `IdSociete` int DEFAULT NULL,
  `AdresseResidence` varchar(500) DEFAULT NULL,
  `DateCreation` datetime(6) NOT NULL,
  `Province` longtext DEFAULT NULL,
  `Ville` longtext DEFAULT NULL,
  `Commune` longtext DEFAULT NULL,
  `Quartier` longtext DEFAULT NULL,
  `Avenue` longtext DEFAULT NULL,
  `Numero` longtext DEFAULT NULL,
  PRIMARY KEY (`IdAgent`),
  UNIQUE KEY `IX_Agents_Matricule_Unique` (`Matricule`),
  UNIQUE KEY `IX_Agents_Email_Unique` (`EmailAgent`),
  UNIQUE KEY `IX_Agents_SerialNumber_Unique` (`SerialNumber`),
  KEY `IX_Agents_IdSociete` (`IdSociete`),
  CONSTRAINT `FK_Agents_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================================
-- 5. TABLE: Utilisateurs
-- ============================================================================
CREATE TABLE IF NOT EXISTS `Utilisateurs` (
  `IdUtilisateur` int NOT NULL AUTO_INCREMENT,
  `ReferenceUtilisateur` char(36) DEFAULT NULL,
  `NomComplet` varchar(200) NOT NULL,
  `Email` varchar(255) DEFAULT NULL,
  `Telephone` longtext DEFAULT NULL,
  `PhotoUrl` longtext DEFAULT NULL,
  `LieuNaissance` longtext DEFAULT NULL,
  `DateNaissance` datetime(6) DEFAULT NULL,
  `Genre` longtext DEFAULT NULL,
  `MotDePasseHash` longtext NOT NULL,
  `DefaultUsername` longtext DEFAULT NULL,
  `DoitChangerMotDePasse` tinyint(1) NOT NULL DEFAULT 0,
  `Statut` tinyint(1) DEFAULT 1,
  `IdRole` int DEFAULT NULL,
  `IdSociete` int DEFAULT NULL,
  `DateCreation` datetime(6) NOT NULL,
  `IsConnecte` tinyint(1) NOT NULL DEFAULT 0,
  `IdAgent` int DEFAULT NULL,
  `IdClient` int DEFAULT NULL,
  `AdresseResidence` varchar(500) DEFAULT NULL,
  PRIMARY KEY (`IdUtilisateur`),
  UNIQUE KEY `IX_Utilisateurs_Email_Unique` (`Email`),
  KEY `IX_Utilisateurs_IdAgent` (`IdAgent`),
  KEY `IX_Utilisateurs_IdClient` (`IdClient`),
  KEY `IX_Utilisateurs_IdRole` (`IdRole`),
  KEY `IX_Utilisateurs_IdSociete` (`IdSociete`),
  CONSTRAINT `FK_Utilisateurs_Agents_IdAgent` FOREIGN KEY (`IdAgent`) REFERENCES `Agents` (`IdAgent`) ON DELETE NO ACTION,
  CONSTRAINT `FK_Utilisateurs_Clients_IdClient` FOREIGN KEY (`IdClient`) REFERENCES `Clients` (`IdClient`) ON DELETE NO ACTION,
  CONSTRAINT `FK_Utilisateurs_Roles_IdRole` FOREIGN KEY (`IdRole`) REFERENCES `Roles` (`IdRole`) ON DELETE CASCADE,
  CONSTRAINT `FK_Utilisateurs_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================================
-- 6. TABLE: RolePermissions
-- ============================================================================
CREATE TABLE IF NOT EXISTS `RolePermissions` (
  `IdRolePermission` int NOT NULL AUTO_INCREMENT,
  `IdRole` int NOT NULL,
  `IdPermission` int NOT NULL,
  `DateAttribution` datetime(6) NOT NULL,
  `IdUtilisateurAttribution` int DEFAULT NULL,
  PRIMARY KEY (`IdRolePermission`),
  KEY `IX_RolePermissions_IdRole` (`IdRole`),
  KEY `IX_RolePermissions_IdPermission` (`IdPermission`),
  CONSTRAINT `FK_RolePermissions_Roles_IdRole` FOREIGN KEY (`IdRole`) REFERENCES `Roles` (`IdRole`) ON DELETE CASCADE,
  CONSTRAINT `FK_RolePermissions_Permissions_IdPermission` FOREIGN KEY (`IdPermission`) REFERENCES `Permissions` (`IdPermission`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================================
-- 7. TABLE: UserRoles
-- ============================================================================
CREATE TABLE IF NOT EXISTS `UserRoles` (
  `IdUserRole` int NOT NULL AUTO_INCREMENT,
  `IdUtilisateur` int NOT NULL,
  `IdRole` int NOT NULL,
  `IsPrimary` tinyint(1) NOT NULL DEFAULT 0,
  `DateAttribution` datetime(6) NOT NULL,
  `IdUtilisateurAttribution` int DEFAULT NULL,
  `Statut` tinyint(1) DEFAULT 1,
  PRIMARY KEY (`IdUserRole`),
  UNIQUE KEY `IX_UserRole_Utilisateur_Role_Unique` (`IdUtilisateur`, `IdRole`),
  KEY `IX_UserRole_IdUtilisateur` (`IdUtilisateur`),
  KEY `IX_UserRole_IdRole` (`IdRole`),
  KEY `IX_UserRole_Utilisateur_Statut` (`IdUtilisateur`, `Statut`),
  CONSTRAINT `FK_UserRoles_Utilisateurs_IdUtilisateur` FOREIGN KEY (`IdUtilisateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE CASCADE,
  CONSTRAINT `FK_UserRoles_Roles_IdRole` FOREIGN KEY (`IdRole`) REFERENCES `Roles` (`IdRole`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================================
-- 8. TABLE: UserPermissions
-- ============================================================================
CREATE TABLE IF NOT EXISTS `UserPermissions` (
  `IdUserPermission` int NOT NULL AUTO_INCREMENT,
  `IdUtilisateur` int NOT NULL,
  `IdPermission` int NOT NULL,
  `IsGranted` tinyint(1) NOT NULL,
  `DateAttribution` datetime(6) NOT NULL,
  `DateExpiration` datetime(6) DEFAULT NULL,
  `Commentaire` varchar(500) DEFAULT NULL,
  `AttribueParIdUtilisateur` int DEFAULT NULL,
  PRIMARY KEY (`IdUserPermission`),
  KEY `IX_UserPermissions_IdUtilisateur` (`IdUtilisateur`),
  KEY `IX_UserPermissions_IdPermission` (`IdPermission`),
  KEY `IX_UserPermissions_AttribueParIdUtilisateur` (`AttribueParIdUtilisateur`),
  CONSTRAINT `FK_UserPermissions_Utilisateurs_IdUtilisateur` FOREIGN KEY (`IdUtilisateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE CASCADE,
  CONSTRAINT `FK_UserPermissions_Permissions_IdPermission` FOREIGN KEY (`IdPermission`) REFERENCES `Permissions` (`IdPermission`) ON DELETE CASCADE,
  CONSTRAINT `FK_UserPermissions_Utilisateurs_AttribueParIdUtilisateur` FOREIGN KEY (`AttribueParIdUtilisateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================================
-- 9. TABLE: UserDevices
-- ============================================================================
CREATE TABLE IF NOT EXISTS `UserDevices` (
  `IdUserDevice` int NOT NULL AUTO_INCREMENT,
  `IdUtilisateur` int NOT NULL,
  `FcmToken` varchar(500) NOT NULL,
  `DeviceType` varchar(100) DEFAULT NULL,
  `DeviceModel` varchar(100) DEFAULT NULL,
  `OsVersion` varchar(50) DEFAULT NULL,
  `DefaultDevice` varchar(100) DEFAULT NULL,
  `Statut` tinyint(1) DEFAULT 1,
  `DateEnregistrement` datetime(6) NOT NULL,
  `DateDerniereUtilisation` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`IdUserDevice`),
  KEY `IX_UserDevices_IdUtilisateur` (`IdUtilisateur`),
  CONSTRAINT `FK_UserDevices_Utilisateurs_IdUtilisateur` FOREIGN KEY (`IdUtilisateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================================
-- 10. TABLE: SmsLogs
-- ============================================================================
CREATE TABLE IF NOT EXISTS `SmsLogs` (
  `IdSmsLog` int NOT NULL AUTO_INCREMENT,
  `NumeroDestinataire` varchar(20) NOT NULL,
  `IdUtilisateur` int DEFAULT NULL,
  `Message` varchar(1600) NOT NULL,
  `TypeNotification` varchar(50) DEFAULT NULL,
  `Statut` varchar(20) NOT NULL DEFAULT 'PENDING',
  `MessageSid` varchar(100) DEFAULT NULL,
  `MessageErreur` varchar(500) DEFAULT NULL,
  `CodeErreur` int DEFAULT NULL,
  `CoutUsd` double NOT NULL DEFAULT 0.0467,
  `CoutFc` double NOT NULL DEFAULT 0.0,
  `DateEnvoi` datetime(6) NOT NULL,
  `DateLivraison` datetime(6) DEFAULT NULL,
  `DateEchec` datetime(6) DEFAULT NULL,
  `NombreSegments` int NOT NULL DEFAULT 1,
  `Direction` varchar(10) DEFAULT 'OUTBOUND',
  `NumeroExpediteur` varchar(20) DEFAULT NULL,
  PRIMARY KEY (`IdSmsLog`),
  KEY `IX_SmsLogs_UtilisateurIdUtilisateur` (`IdUtilisateur`),
  CONSTRAINT `FK_SmsLogs_Utilisateurs_UtilisateurIdUtilisateur` FOREIGN KEY (`IdUtilisateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================================
-- 11. TABLE: RefreshTokens
-- ============================================================================
CREATE TABLE IF NOT EXISTS `RefreshTokens` (
  `IdRefreshToken` int NOT NULL AUTO_INCREMENT,
  `IdUtilisateur` int NOT NULL,
  `TokenHash` varchar(500) NOT NULL,
  `DateCreation` datetime(6) NOT NULL,
  `DateExpiration` datetime(6) NOT NULL,
  `DateRevocation` datetime(6) DEFAULT NULL,
  `DeviceInfo` varchar(200) DEFAULT NULL,
  `IpAddress` varchar(50) DEFAULT NULL,
  PRIMARY KEY (`IdRefreshToken`),
  KEY `IX_RefreshTokens_IdUtilisateur` (`IdUtilisateur`),
  CONSTRAINT `FK_RefreshTokens_Utilisateurs_IdUtilisateur` FOREIGN KEY (`IdUtilisateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================================
-- 12. TABLE: PasswordResetTokens
-- ============================================================================
CREATE TABLE IF NOT EXISTS `PasswordResetTokens` (
  `IdPasswordResetToken` int NOT NULL AUTO_INCREMENT,
  `IdUtilisateur` int NOT NULL,
  `Token` varchar(200) NOT NULL,
  `DateCreation` datetime(6) NOT NULL,
  `DateExpiration` datetime(6) NOT NULL,
  `DateUtilisation` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`IdPasswordResetToken`),
  UNIQUE KEY `IX_PasswordResetTokens_Token` (`Token`),
  KEY `IX_PasswordResetTokens_IdUtilisateur` (`IdUtilisateur`),
  CONSTRAINT `FK_PasswordResetTokens_Utilisateurs_IdUtilisateur` FOREIGN KEY (`IdUtilisateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================================
-- 13. TABLE: Notifications
-- ============================================================================
CREATE TABLE IF NOT EXISTS `Notifications` (
  `IdNotification` int NOT NULL AUTO_INCREMENT,
  `Titre` varchar(200) NOT NULL,
  `Statut` tinyint(1) DEFAULT 1,
  `Contenu` varchar(1000) NOT NULL,
  `TypeNotification` varchar(50) NOT NULL,
  `EstLue` tinyint(1) NOT NULL DEFAULT 0,
  `DateCreation` datetime(6) NOT NULL,
  `DateLecture` datetime(6) DEFAULT NULL,
  `LienAction` varchar(100) DEFAULT NULL,
  `Icone` varchar(50) DEFAULT NULL,
  `EstActive` tinyint(1) NOT NULL DEFAULT 1,
  `IdExpediteur` int DEFAULT NULL,
  `IdDestinataire` int DEFAULT NULL,
  `IdSociete` int DEFAULT NULL,
  `IdAgent` int DEFAULT NULL,
  `CanalUtilise` varchar(20) DEFAULT NULL,
  `Priorite` varchar(20) NOT NULL DEFAULT 'INFO',
  `PayloadJson` longtext DEFAULT NULL,
  `StatutEnvoi` varchar(20) NOT NULL DEFAULT 'Envoye',
  `TrackingId` varchar(100) DEFAULT NULL,
  PRIMARY KEY (`IdNotification`),
  KEY `IX_Notifications_IdExpediteur` (`IdExpediteur`),
  KEY `IX_Notifications_IdDestinataire` (`IdDestinataire`),
  KEY `IX_Notifications_IdSociete` (`IdSociete`),
  KEY `IX_Notifications_IdAgent` (`IdAgent`),
  CONSTRAINT `FK_Notifications_Utilisateurs_IdExpediteur` FOREIGN KEY (`IdExpediteur`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE NO ACTION,
  CONSTRAINT `FK_Notifications_Utilisateurs_IdDestinataire` FOREIGN KEY (`IdDestinataire`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE NO ACTION,
  CONSTRAINT `FK_Notifications_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE NO ACTION,
  CONSTRAINT `FK_Notifications_Agents_IdAgent` FOREIGN KEY (`IdAgent`) REFERENCES `Agents` (`IdAgent`) ON DELETE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================================
-- 14. TABLE: AuditLogs
-- ============================================================================
CREATE TABLE IF NOT EXISTS `AuditLogs` (
  `IdAudit` bigint NOT NULL AUTO_INCREMENT,
  `TableName` varchar(100) NOT NULL,
  `RecordId` int NOT NULL,
  `Action` varchar(20) NOT NULL,
  `UserId` int NOT NULL,
  `UserName` varchar(200) NOT NULL,
  `UserRole` varchar(50) DEFAULT NULL,
  `IdSociete` int DEFAULT NULL,
  `DateAction` datetime(6) NOT NULL,
  `OldValues` text DEFAULT NULL,
  `NewValues` text DEFAULT NULL,
  `ChangedFields` varchar(500) DEFAULT NULL,
  `IpAddress` varchar(50) DEFAULT NULL,
  `UserAgent` varchar(500) DEFAULT NULL,
  `Commentaire` text DEFAULT NULL,
  `HttpMethod` varchar(10) DEFAULT NULL,
  `Endpoint` varchar(500) DEFAULT NULL,
  `DurationMs` int DEFAULT NULL,
  `Success` tinyint(1) NOT NULL,
  `ErrorMessage` varchar(1000) DEFAULT NULL,
  PRIMARY KEY (`IdAudit`),
  KEY `IX_AuditLog_Table_Record` (`TableName`, `RecordId`),
  KEY `IX_AuditLog_UserId` (`UserId`),
  KEY `IX_AuditLog_DateAction` (`DateAction`),
  KEY `IX_AuditLog_IdSociete` (`IdSociete`),
  KEY `IX_AuditLog_Action` (`Action`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================================
-- 15. TABLE: CategorieClients
-- ============================================================================
CREATE TABLE IF NOT EXISTS `CategorieClients` (
  `IdCategorie` int NOT NULL AUTO_INCREMENT,
  `NomCategorie` varchar(100) DEFAULT NULL,
  `Description` varchar(500) DEFAULT NULL,
  `Actif` tinyint(1) DEFAULT 1,
  `IdSociete` int NOT NULL,
  `DateCreation` datetime(6) NOT NULL,
  PRIMARY KEY (`IdCategorie`),
  UNIQUE KEY `IX_CategorieClient_NomCategorie_IdSociete` (`NomCategorie`, `IdSociete`),
  KEY `IX_CategorieClients_IdSociete` (`IdSociete`),
  CONSTRAINT `FK_CategorieClients_Societes_IdSociete` FOREIGN KEY (`IdSociete`) REFERENCES `Societes` (`IdSociete`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================================
-- 16. TABLE: Clients
-- ============================================================================
CREATE TABLE IF NOT EXISTS `Clients` (
  `IdClient` int NOT NULL AUTO_INCREMENT,
  `NomClient` varchar(200) NOT NULL,
  `AdresseClient` varchar(500) NOT NULL,
  `Telephone` varchar(20) DEFAULT NULL,
  `EmailClient` varchar(256) DEFAULT NULL,
  `GenreClient` varchar(10) DEFAULT NULL,
  `numero_compteur` varchar(50) DEFAULT NULL,
  `Statut` tinyint(1) NOT NULL DEFAULT 1,
  `IdCategorieClient` int DEFAULT NULL,
  `DateCreation` datetime(6) NOT NULL,
  PRIMARY KEY (`IdClient`),
  UNIQUE KEY `IX_Client_NumeroCompteur_Unique` (`numero_compteur`),
  KEY `IX_Clients_IdCategorieClient` (`IdCategorieClient`),
  CONSTRAINT `FK_Clients_CategorieClients_IdCategorieClient` FOREIGN KEY (`IdCategorieClient`) REFERENCES `CategorieClients` (`IdCategorie`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================================
-- 17. TABLE: Factures
-- ============================================================================
CREATE TABLE IF NOT EXISTS `Factures` (
  `IdFacture` int NOT NULL AUTO_INCREMENT,
  `numero_facture` varchar(100) DEFAULT NULL,
  `MontantTotal` decimal(18,2) DEFAULT NULL,
  `MontantPaye` decimal(18,2) DEFAULT NULL,
  `DateEmission` datetime(6) DEFAULT NULL,
  `Statut` tinyint(1) NOT NULL DEFAULT 1,
  `MoisEmission` int NOT NULL,
  `AnneesEmission` int NOT NULL,
  `IdCategorie` int DEFAULT NULL,
  `DateCreation` datetime(6) NOT NULL,
  PRIMARY KEY (`IdFacture`),
  UNIQUE KEY `IX_Facture_NumeroFacture_Unique` (`numero_facture`),
  KEY `IX_Facture_Mois_Annee_Categorie` (`MoisEmission`, `AnneesEmission`, `IdCategorie`),
  KEY `IX_Factures_IdCategorie` (`IdCategorie`),
  CONSTRAINT `FK_Factures_CategorieClients_IdCategorie` FOREIGN KEY (`IdCategorie`) REFERENCES `CategorieClients` (`IdCategorie`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================================
-- 18. TABLE: Paiements
-- ============================================================================
CREATE TABLE IF NOT EXISTS `Paiements` (
  `IdPaiement` int NOT NULL AUTO_INCREMENT,
  `IdFacture` int NOT NULL,
  `IdClient` int DEFAULT NULL,
  `Montant` decimal(18,2) NOT NULL,
  `DatePaiement` datetime(6) NOT NULL,
  `MethodePaiement` varchar(50) DEFAULT NULL,
  `ReferenceTransaction` varchar(100) DEFAULT NULL,
  `Commentaire` varchar(500) DEFAULT NULL,
  `Statut` varchar(20) NOT NULL DEFAULT 'Validé',
  `IdUtilisateurEnregistrement` int DEFAULT NULL,
  `DateCreation` datetime(6) NOT NULL,
  PRIMARY KEY (`IdPaiement`),
  KEY `IX_Paiements_IdFacture` (`IdFacture`),
  KEY `IX_Paiements_IdClient` (`IdClient`),
  KEY `IX_Paiements_DatePaiement` (`DatePaiement`),
  CONSTRAINT `FK_Paiements_Factures_IdFacture` FOREIGN KEY (`IdFacture`) REFERENCES `Factures` (`IdFacture`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Paiements_Clients_IdClient` FOREIGN KEY (`IdClient`) REFERENCES `Clients` (`IdClient`) ON DELETE SET NULL,
  CONSTRAINT `FK_Paiements_Utilisateurs_IdUtilisateurEnregistrement` FOREIGN KEY (`IdUtilisateurEnregistrement`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================================
-- FIN DU SCRIPT
-- ============================================================================

SET FOREIGN_KEY_CHECKS = 1;
COMMIT;

-- ============================================================================
-- NOTES IMPORTANTES:
-- ============================================================================
-- 1. Après l'exécution de ce script, vous devez:
--    - Exécuter l'initialisation des données par défaut via l'API
--      (POST /api/Init/initialize) ou via le code d'initialisation
--    - Créer les rôles par défaut (Super-Admin, Admin, Gerant, etc.)
--    - Créer la société par défaut (CongoTravel)
--    - Créer l'utilisateur Super-Admin par défaut
--    - Initialiser les permissions via PermissionSeeder
--
-- 2. Les valeurs par défaut sont définies dans le code C# et seront
--    appliquées lors de l'initialisation via l'API.
--
-- 3. Pour les environnements de production, assurez-vous de:
--    - Configurer les backups automatiques
--    - Configurer la réplication si nécessaire
--    - Optimiser les index selon vos besoins de requêtes
--    - Configurer les paramètres de performance MariaDB/MySQL
--
-- 4. Le charset utf8mb4 est utilisé pour supporter les emojis et
--    tous les caractères Unicode.
--
-- ============================================================================

