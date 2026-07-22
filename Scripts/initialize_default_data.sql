-- ============================================================================
-- Script d'initialisation des données par défaut - CongoTravel API
-- Base de données: MariaDB / MySQL
-- Version: 2.0
-- Date: Décembre 2025
-- ============================================================================
-- 
-- INSTRUCTIONS:
-- 1. Assurez-vous d'avoir exécuté create_database_production.sql d'abord
-- 2. Utilisez la base de données: USE CongoTravelDB;
-- 3. Exécutez ce script pour initialiser les données par défaut
-- 
-- ⚠️  ATTENTION: Ce script peut être exécuté plusieurs fois sans erreur
--    (il vérifie l'existence avant d'insérer)
-- 
-- ============================================================================

SET FOREIGN_KEY_CHECKS = 0;
SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET AUTOCOMMIT = 0;
START TRANSACTION;
SET time_zone = "+00:00";

-- ============================================================================
-- 1. CRÉER TOUS LES RÔLES
-- ============================================================================

-- Super-Admin
INSERT IGNORE INTO `Roles` (`Nom`, `Description`, `Niveau`, `Statut`, `DateCreation`)
VALUES ('Super-Admin', 'Administrateur système avec tous les droits', 1, 1, NOW());

-- Admin
INSERT IGNORE INTO `Roles` (`Nom`, `Description`, `Niveau`, `Statut`, `DateCreation`)
VALUES ('Admin', 'Administrateur de société', 2, 1, NOW());

-- Gerant
INSERT IGNORE INTO `Roles` (`Nom`, `Description`, `Niveau`, `Statut`, `DateCreation`)
VALUES ('Gerant', 'Gérant de société', 3, 1, NOW());

-- Financier
INSERT IGNORE INTO `Roles` (`Nom`, `Description`, `Niveau`, `Statut`, `DateCreation`)
VALUES ('Financier', 'Gestionnaire financier', 4, 1, NOW());

-- Caissier
INSERT IGNORE INTO `Roles` (`Nom`, `Description`, `Niveau`, `Statut`, `DateCreation`)
VALUES ('Caissier', 'Caissier de la société', 5, 1, NOW());

-- Technicien
INSERT IGNORE INTO `Roles` (`Nom`, `Description`, `Niveau`, `Statut`, `DateCreation`)
VALUES ('Technicien', 'Technicien de la société', 6, 1, NOW());

-- Client
INSERT IGNORE INTO `Roles` (`Nom`, `Description`, `Niveau`, `Statut`, `DateCreation`)
VALUES ('Client', 'Client de la société', 7, 1, NOW());

-- ============================================================================
-- 2. CRÉER LA SOCIÉTÉ PAR DÉFAUT (CongoTravel)
-- ============================================================================

INSERT IGNORE INTO `Societes` (
    `Nom`, 
    `Devise`, 
    `Type`, 
    `Description`, 
    `Telephone`, 
    `EmailContact`, 
    `SiteWeb`, 
    `NomCompletResponsable`, 
    `GenreResponsable`, 
    `Statut`, 
    `DateCreation`
)
VALUES (
    'CongoTravel',
    'Excellence et Innovation',
    'Privée',
    'Société d''excellence offrant des services de qualité énergétique',
    '+243999999999',
    'contact@kenergie.cd',
    'https://www.kenergie.cd',
    'Administrateur Super Admin',
    'Masculin',
    1,
    NOW()
);

-- ============================================================================
-- 3. CRÉER L'AGENT MANAGER GÉNÉRAL
-- ============================================================================

-- Récupérer l'ID de la société CongoTravel
SET @societe_id = (SELECT `IdSociete` FROM `Societes` WHERE `Nom` = 'CongoTravel' LIMIT 1);

-- Générer un matricule unique
-- Format: NAT25-XXXXXX (année + 6 caractères aléatoires)
SET @matricule = CONCAT('NAT', SUBSTRING(YEAR(NOW()), 3, 2), '-', UPPER(SUBSTRING(REPLACE(UUID(), '-', ''), 1, 6)));

-- Si le matricule existe déjà, ajouter un suffixe
SET @matricule = IF(
    EXISTS (SELECT 1 FROM `Agents` WHERE `Matricule` = @matricule),
    CONCAT(@matricule, '-', UPPER(SUBSTRING(REPLACE(UUID(), '-', ''), 1, 3))),
    @matricule
);

INSERT IGNORE INTO `Agents` (
    `Matricule`,
    `NomComplet`,
    `Genre`,
    `DateNaissance`,
    `TelephoneAgent`,
    `EmailAgent`,
    `Statut`,
    `EtatCivil`,
    `Fonction`,
    `RoleAgent`,
    `IdSociete`,
    `DateCreation`
)
VALUES (
    @matricule,
    'Administrateur Super Admin',
    'Masculin',
    DATE_SUB(NOW(), INTERVAL 40 YEAR),
    '+243999999999',
    'superadmin@kenergie.cd',
    1,
    'Marié',
    'Manager Général',
    'Super-Administrateur',
    @societe_id,
    NOW()
);

-- ============================================================================
-- 4. CRÉER L'UTILISATEUR SUPER-ADMIN
-- ============================================================================

-- Récupérer les IDs nécessaires
SET @super_admin_role_id = (SELECT `IdRole` FROM `Roles` WHERE `Nom` = 'Super-Admin' LIMIT 1);
SET @agent_id = (SELECT `IdAgent` FROM `Agents` WHERE `Fonction` = 'Manager Général' AND `IdSociete` = @societe_id LIMIT 1);

-- Hash BCrypt pour le mot de passe "Super-Admin"
-- ⚠️  Ce hash a été généré avec BCrypt.Net avec un salt de 11 rounds
--     Mot de passe: "Super-Admin"
--     Pour régénérer: BCrypt.Net.BCrypt.HashPassword("Super-Admin", BCrypt.Net.BCrypt.GenerateSalt(11))
SET @password_hash = '$2a$11$tX0SfJiizuLVM/lKlukD9Oteko2c.uNAWAXl5UQ3YwvQnWzSRmrJ6';

-- Générer un GUID pour ReferenceUtilisateur
SET @reference_utilisateur = UUID();

INSERT IGNORE INTO `Utilisateurs` (
    `ReferenceUtilisateur`,
    `NomComplet`,
    `Email`,
    `Telephone`,
    `Genre`,
    `DateNaissance`,
    `MotDePasseHash`,
    `DefaultUsername`,
    `DoitChangerMotDePasse`,
    `Statut`,
    `IdRole`,
    `IdSociete`,
    `IdAgent`,
    `DateCreation`,
    `IsConnecte`
)
VALUES (
    @reference_utilisateur,
    'Administrateur Super Admin',
    'superadmin@kenergie.cd',
    '+243999999999',
    'Masculin',
    DATE_SUB(NOW(), INTERVAL 40 YEAR),
    @password_hash,
    'SuperAdmin',
    1, -- Doit changer le mot de passe à la première connexion
    1, -- Statut actif
    @super_admin_role_id,
    @societe_id,
    @agent_id,
    NOW(),
    0 -- Non connecté
);

-- ============================================================================
-- 5. CRÉER L'ASSOCIATION USERROLE (Multi-rôles)
-- ============================================================================

SET @utilisateur_id = (SELECT `IdUtilisateur` FROM `Utilisateurs` WHERE `Email` = 'superadmin@kenergie.cd' LIMIT 1);

-- Associer le rôle Super-Admin à l'utilisateur comme rôle principal
INSERT IGNORE INTO `UserRoles` (
    `IdUtilisateur`,
    `IdRole`,
    `IsPrimary`,
    `DateAttribution`,
    `Statut`
)
VALUES (
    @utilisateur_id,
    @super_admin_role_id,
    1, -- Rôle principal
    NOW(),
    1 -- Statut actif
);

-- ============================================================================
-- 6. CRÉER L'AGENT ADMINISTRATEUR
-- ============================================================================

-- Générer un matricule unique pour l'Agent Admin
SET @matricule_admin = CONCAT('NAT', SUBSTRING(YEAR(NOW()), 3, 2), '-', UPPER(SUBSTRING(REPLACE(UUID(), '-', ''), 1, 6)));

-- Si le matricule existe déjà, ajouter un suffixe
SET @matricule_admin = IF(
    EXISTS (SELECT 1 FROM `Agents` WHERE `Matricule` = @matricule_admin),
    CONCAT(@matricule_admin, '-', UPPER(SUBSTRING(REPLACE(UUID(), '-', ''), 1, 3))),
    @matricule_admin
);

INSERT IGNORE INTO `Agents` (
    `Matricule`,
    `NomComplet`,
    `Genre`,
    `DateNaissance`,
    `TelephoneAgent`,
    `EmailAgent`,
    `Statut`,
    `EtatCivil`,
    `Fonction`,
    `RoleAgent`,
    `IdSociete`,
    `DateCreation`
)
VALUES (
    @matricule_admin,
    'Administrateur CongoTravel',
    'Masculin',
    DATE_SUB(NOW(), INTERVAL 35 YEAR),
    '+243888888888',
    'admin@kenergie.cd',
    1,
    'Marié',
    'Administrateur',
    'Admin',
    @societe_id,
    NOW()
);

-- ============================================================================
-- 7. CRÉER L'UTILISATEUR ADMIN
-- ============================================================================

-- Récupérer les IDs nécessaires
SET @admin_role_id = (SELECT `IdRole` FROM `Roles` WHERE `Nom` = 'Admin' LIMIT 1);
SET @admin_agent_id = (SELECT `IdAgent` FROM `Agents` WHERE `Fonction` = 'Administrateur' AND `IdSociete` = @societe_id LIMIT 1);

-- Hash BCrypt pour le mot de passe "Admin"
-- ⚠️  Ce hash a été généré avec BCrypt.Net avec un salt de 11 rounds
--     Mot de passe: "Admin"
SET @password_hash_admin = '$2a$11$QEpQ6v/jCLZ5bu0Ea3bgV./7c9Hv4hvB3mI8F9V4ILJNpJFNEZBYC';

-- Générer un GUID pour ReferenceUtilisateur
SET @reference_utilisateur_admin = UUID();

INSERT IGNORE INTO `Utilisateurs` (
    `ReferenceUtilisateur`,
    `NomComplet`,
    `Email`,
    `Telephone`,
    `Genre`,
    `DateNaissance`,
    `MotDePasseHash`,
    `DefaultUsername`,
    `DoitChangerMotDePasse`,
    `Statut`,
    `IdRole`,
    `IdSociete`,
    `IdAgent`,
    `DateCreation`,
    `IsConnecte`
)
VALUES (
    @reference_utilisateur_admin,
    'Administrateur CongoTravel',
    'admin@kenergie.cd',
    '+243888888888',
    'Masculin',
    DATE_SUB(NOW(), INTERVAL 35 YEAR),
    @password_hash_admin,
    'Admin',
    1, -- Doit changer le mot de passe à la première connexion
    1, -- Statut actif
    @admin_role_id,
    @societe_id,
    @admin_agent_id,
    NOW(),
    0 -- Non connecté
);

-- ============================================================================
-- 8. CRÉER L'ASSOCIATION USERROLE POUR L'ADMIN
-- ============================================================================

SET @admin_utilisateur_id = (SELECT `IdUtilisateur` FROM `Utilisateurs` WHERE `Email` = 'admin@kenergie.cd' LIMIT 1);

-- Associer le rôle Admin à l'utilisateur comme rôle principal
INSERT IGNORE INTO `UserRoles` (
    `IdUtilisateur`,
    `IdRole`,
    `IsPrimary`,
    `DateAttribution`,
    `Statut`
)
VALUES (
    @admin_utilisateur_id,
    @admin_role_id,
    1, -- Rôle principal
    NOW(),
    1 -- Statut actif
);

-- ============================================================================
-- FIN DU SCRIPT
-- ============================================================================

SET FOREIGN_KEY_CHECKS = 1;
COMMIT;

-- ============================================================================
-- VÉRIFICATION DES DONNÉES CRÉÉES
-- ============================================================================

SELECT '✅ Rôles créés:' as Message;
SELECT `IdRole`, `Nom`, `Niveau`, `Statut` FROM `Roles` ORDER BY `Niveau`;

SELECT '✅ Société créée:' as Message;
SELECT `IdSociete`, `Nom`, `EmailContact`, `Telephone`, `Statut` FROM `Societes` WHERE `Nom` = 'CongoTravel';

SELECT '✅ Agents créés:' as Message;
SELECT `IdAgent`, `Matricule`, `NomComplet`, `Fonction`, `EmailAgent` FROM `Agents` WHERE `Fonction` IN ('Manager Général', 'Administrateur') ORDER BY `Fonction`;

SELECT '✅ Utilisateurs créés:' as Message;
SELECT 
    `IdUtilisateur`,
    `NomComplet`,
    `Email`,
    `DefaultUsername`,
    `Telephone`,
    `DoitChangerMotDePasse`,
    `Statut`
FROM `Utilisateurs` 
WHERE `Email` IN ('superadmin@kenergie.cd', 'admin@kenergie.cd')
ORDER BY `Email`;

SELECT '✅ Associations UserRole créées:' as Message;
SELECT 
    ur.`IdUserRole`,
    u.`NomComplet` as Utilisateur,
    u.`Email`,
    r.`Nom` as Role,
    ur.`IsPrimary`,
    ur.`Statut`
FROM `UserRoles` ur
INNER JOIN `Utilisateurs` u ON ur.`IdUtilisateur` = u.`IdUtilisateur`
INNER JOIN `Roles` r ON ur.`IdRole` = r.`IdRole`
WHERE u.`Email` IN ('superadmin@kenergie.cd', 'admin@kenergie.cd')
ORDER BY u.`Email`, ur.`IsPrimary` DESC;

-- ============================================================================
-- NOTES IMPORTANTES:
-- ============================================================================
-- 1. Après l'exécution de ce script, vous devez:
--    - Initialiser les permissions via l'API (POST /api/Init/initialize)
--      ou via PermissionSeeder.SeedPermissionsAsync()
--    - Les permissions ne sont PAS créées dans ce script car il y en a 80+
--      et elles doivent être assignées aux rôles
--
-- 2. Identifiants de connexion par défaut:
--    - Email: superadmin@kenergie.cd
--    - Username: SuperAdmin
--    - Mot de passe: Super-Admin
--    - ⚠️  Le système forcera le changement de mot de passe à la première connexion
--
-- 3. Pour initialiser les permissions, vous avez deux options:
--    Option A: Via l'API (recommandé)
--      POST https://votre-serveur/api/Init/initialize
--      Cela créera aussi les permissions et les assignera aux rôles
--
--    Option B: Via le code
--      await PermissionSeeder.SeedPermissionsAsync(context);
--
-- 4. Le hash du mot de passe dans ce script est un hash BCrypt valide.
--    Si vous devez le régénérer, utilisez BCrypt.Net avec 11 rounds de salt.
--
-- ============================================================================

