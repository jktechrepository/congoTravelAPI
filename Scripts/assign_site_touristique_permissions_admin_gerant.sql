-- =============================================================================
-- Assigner les permissions SiteTouristique aux rôles
-- Admin / Gerant / Super-Admin / Client / Caissier
-- Corrige : 403 sur POST /api/sites-touristiques/lieux (Admin/Gerant Write)
--            et 403 vente / gate (Client, Caissier)
--
-- Obligatoire après déploiement tables ST sur une DB existante (comptes Admin /
-- Gerant / Caissier / Client déjà créés). Idempotent (INSERT IGNORE).
-- Pas besoin de regenerer le JWT.
--
-- Diagnostic : Scripts/diagnostic_permissions_site_touristique_restaurant.sql
-- =============================================================================

INSERT IGNORE INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
VALUES
    ('SiteTouristique.Lieu.Read', 'SiteTouristique', 'Lieu.Read', 'Lister / consulter lieux et journées site touristique', 1, UTC_TIMESTAMP()),
    ('SiteTouristique.Lieu.Write', 'SiteTouristique', 'Lieu.Write', 'Créer / publier lieux et journées site touristique', 1, UTC_TIMESTAMP()),
    ('SiteTouristique.Classe.Read', 'SiteTouristique', 'Classe.Read', 'Lister classes site touristique', 1, UTC_TIMESTAMP()),
    ('SiteTouristique.Classe.Write', 'SiteTouristique', 'Classe.Write', 'Créer / modifier classes site touristique', 1, UTC_TIMESTAMP()),
    ('SiteTouristique.Hold.Create', 'SiteTouristique', 'Hold.Create', 'Créer un hold site touristique', 1, UTC_TIMESTAMP()),
    ('SiteTouristique.Reservation.Confirm', 'SiteTouristique', 'Reservation.Confirm', 'Confirmer réservation site touristique', 1, UTC_TIMESTAMP()),
    ('SiteTouristique.Ticket.Check', 'SiteTouristique', 'Ticket.Check', 'Vérifier ticket site touristique', 1, UTC_TIMESTAMP()),
    ('SiteTouristique.Ticket.Use', 'SiteTouristique', 'Ticket.Use', 'Utiliser ticket site touristique', 1, UTC_TIMESTAMP()),
    ('SiteTouristique.Dashboard.Read', 'SiteTouristique', 'Dashboard.Read', 'Dashboard billetterie site touristique', 1, UTC_TIMESTAMP());

INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT r.`IdRole`, p.`IdPermission`, UTC_TIMESTAMP()
FROM `Roles` r
CROSS JOIN `Permissions` p
WHERE r.`Nom` IN ('Admin', 'Gerant')
  AND p.`Categorie` = 'SiteTouristique'
  AND p.`Statut` = 1;

INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT r.`IdRole`, p.`IdPermission`, UTC_TIMESTAMP()
FROM `Roles` r
CROSS JOIN `Permissions` p
WHERE r.`Nom` = 'Super-Admin'
  AND p.`Statut` = 1;

-- Client : lecture + achat (hold + confirm) — pas de gate
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT r.`IdRole`, p.`IdPermission`, UTC_TIMESTAMP()
FROM `Roles` r
CROSS JOIN `Permissions` p
WHERE r.`Nom` = 'Client'
  AND p.`Nom` IN (
      'SiteTouristique.Lieu.Read',
      'SiteTouristique.Hold.Create',
      'SiteTouristique.Reservation.Confirm'
  );

-- Caissier : vente guichet + gate tickets (aligné PermissionSeeder)
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT r.`IdRole`, p.`IdPermission`, UTC_TIMESTAMP()
FROM `Roles` r
CROSS JOIN `Permissions` p
WHERE r.`Nom` = 'Caissier'
  AND p.`Nom` IN (
      'SiteTouristique.Lieu.Read',
      'SiteTouristique.Hold.Create',
      'SiteTouristique.Reservation.Confirm',
      'SiteTouristique.Ticket.Check',
      'SiteTouristique.Ticket.Use'
  );

SELECT r.`Nom` AS Role, p.`Nom` AS Permission
FROM `RolePermissions` rp
INNER JOIN `Roles` r ON r.`IdRole` = rp.`IdRole`
INNER JOIN `Permissions` p ON p.`IdPermission` = rp.`IdPermission`
WHERE p.`Categorie` = 'SiteTouristique'
ORDER BY r.`Nom`, p.`Nom`;
