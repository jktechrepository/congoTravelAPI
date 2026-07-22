-- =============================================================================
-- Assigner les permissions Evenement aux rôles Admin / Gérant / Super-Admin
-- Corrige : 403 sur POST /api/events/classes
--            Permission requise : Evenement.Session.Write
--
-- Diagnostic :
--   SELECT r.Nom, p.Nom
--   FROM RolePermissions rp
--   JOIN Roles r ON r.IdRole = rp.IdRole
--   JOIN Permissions p ON p.IdPermission = rp.IdPermission
--   WHERE p.Categorie = 'Evenement' AND r.Nom = 'Admin';
-- =============================================================================

-- 1) Créer les permissions Evenement si absentes (aligné PermissionSeeder)
INSERT IGNORE INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
VALUES
    ('Evenement.Session.Read', 'Evenement', 'Session.Read', 'Lister / consulter sessions événement', 1, UTC_TIMESTAMP()),
    ('Evenement.Session.Write', 'Evenement', 'Session.Write', 'Créer / publier sessions événement', 1, UTC_TIMESTAMP()),
    ('Evenement.Hold.Create', 'Evenement', 'Hold.Create', 'Créer un hold événement', 1, UTC_TIMESTAMP()),
    ('Evenement.Reservation.Confirm', 'Evenement', 'Reservation.Confirm', 'Confirmer réservation événement', 1, UTC_TIMESTAMP()),
    ('Evenement.Ticket.Check', 'Evenement', 'Ticket.Check', 'Vérifier ticket événement', 1, UTC_TIMESTAMP()),
    ('Evenement.Ticket.Use', 'Evenement', 'Ticket.Use', 'Utiliser ticket événement', 1, UTC_TIMESTAMP()),
    ('Evenement.Dashboard.Read', 'Evenement', 'Dashboard.Read', 'Dashboard billetterie événement', 1, UTC_TIMESTAMP());

-- 2) Admin + Gérant : toute la catégorie Evenement
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT r.`IdRole`, p.`IdPermission`, UTC_TIMESTAMP()
FROM `Roles` r
CROSS JOIN `Permissions` p
WHERE r.`Nom` IN ('Admin', 'Gerant')
  AND p.`Categorie` = 'Evenement'
  AND p.`Statut` = 1;

-- 3) Super-Admin : toutes les permissions actives
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT r.`IdRole`, p.`IdPermission`, UTC_TIMESTAMP()
FROM `Roles` r
CROSS JOIN `Permissions` p
WHERE r.`Nom` = 'Super-Admin'
  AND p.`Statut` = 1;

-- 4) Vérification
SELECT r.`Nom` AS Role, p.`Nom` AS Permission
FROM `RolePermissions` rp
INNER JOIN `Roles` r ON r.`IdRole` = rp.`IdRole`
INNER JOIN `Permissions` p ON p.`IdPermission` = rp.`IdPermission`
WHERE p.`Nom` = 'Evenement.Session.Write'
ORDER BY r.`Nom`;
