-- Email verification tokens (lien de confirmation d'email à l'inscription client)
-- Aligné sur Migrations/20260725143231_AddEmailVerificationTokens.cs

CREATE TABLE IF NOT EXISTS `EmailVerificationTokens` (
  `IdEmailVerificationToken` int NOT NULL AUTO_INCREMENT,
  `IdUtilisateur` int NOT NULL,
  `CodeHash` varchar(128) CHARACTER SET utf8mb4 NOT NULL,
  `DateCreation` datetime(6) NOT NULL,
  `DateExpiration` datetime(6) NOT NULL,
  `DateUtilisation` datetime(6) NULL,
  `AttemptCount` int NOT NULL,
  PRIMARY KEY (`IdEmailVerificationToken`),
  KEY `IX_EmailVerificationTokens_IdUtilisateur_DateUtilisation` (`IdUtilisateur`, `DateUtilisation`),
  CONSTRAINT `FK_EmailVerificationTokens_Utilisateurs_IdUtilisateur`
    FOREIGN KEY (`IdUtilisateur`) REFERENCES `Utilisateurs` (`IdUtilisateur`) ON DELETE CASCADE
) CHARACTER SET utf8mb4;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
SELECT '20260725143231_AddEmailVerificationTokens', '6.0.0'
FROM DUAL
WHERE NOT EXISTS (
  SELECT 1 FROM `__EFMigrationsHistory`
  WHERE `MigrationId` = '20260725143231_AddEmailVerificationTokens'
);
