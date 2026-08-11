-- =============================================================================
-- Assigner les permissions Restaurant aux rôles
-- Admin / Gerant / Super-Admin / Client / Caissier / Financier
-- Corrige : 403 sur POST /api/restaurants/etablissements (Admin/Gerant Write)
--            et 403 vente acompte (Client, Caissier)
--
-- Obligatoire après déploiement tables Restaurant sur une DB existante (comptes
-- Admin / Gerant / Caissier / Client déjà créés). Idempotent (INSERT IGNORE).
-- Pas besoin de regenerer le JWT.
--
-- Diagnostic : Scripts/diagnostic_permissions_site_touristique_restaurant.sql
-- =============================================================================

INSERT IGNORE INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
VALUES
    ('Restaurant.Etablissement.Read', 'Restaurant', 'Etablissement.Read', 'Lister / consulter établissements et créneaux restaurant', 1, UTC_TIMESTAMP()),
    ('Restaurant.Etablissement.Write', 'Restaurant', 'Etablissement.Write', 'Créer / publier établissements et créneaux restaurant', 1, UTC_TIMESTAMP()),
    ('Restaurant.Zone.Read', 'Restaurant', 'Zone.Read', 'Lister zones restaurant (V1.1)', 1, UTC_TIMESTAMP()),
    ('Restaurant.Zone.Write', 'Restaurant', 'Zone.Write', 'Créer / modifier zones restaurant (V1.1)', 1, UTC_TIMESTAMP()),
    ('Restaurant.Hold.Create', 'Restaurant', 'Hold.Create', 'Créer un hold restaurant', 1, UTC_TIMESTAMP()),
    ('Restaurant.Reservation.Confirm', 'Restaurant', 'Reservation.Confirm', 'Confirmer réservation restaurant', 1, UTC_TIMESTAMP()),
    ('Restaurant.Dashboard.Read', 'Restaurant', 'Dashboard.Read', 'Dashboard réservation restaurant', 1, UTC_TIMESTAMP());

INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT r.`IdRole`, p.`IdPermission`, UTC_TIMESTAMP()
FROM `Roles` r
CROSS JOIN `Permissions` p
WHERE r.`Nom` IN ('Admin', 'Gerant')
  AND p.`Categorie` = 'Restaurant'
  AND p.`Statut` = 1;

INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT r.`IdRole`, p.`IdPermission`, UTC_TIMESTAMP()
FROM `Roles` r
CROSS JOIN `Permissions` p
WHERE r.`Nom` = 'Super-Admin'
  AND p.`Statut` = 1;

-- Client : lecture + achat (hold + confirm)
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT r.`IdRole`, p.`IdPermission`, UTC_TIMESTAMP()
FROM `Roles` r
CROSS JOIN `Permissions` p
WHERE r.`Nom` = 'Client'
  AND p.`Nom` IN (
      'Restaurant.Etablissement.Read',
      'Restaurant.Hold.Create',
      'Restaurant.Reservation.Confirm'
  );

-- Caissier : Read + Hold + Confirm (miroir SiteTouristique sans tickets)
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT r.`IdRole`, p.`IdPermission`, UTC_TIMESTAMP()
FROM `Roles` r
CROSS JOIN `Permissions` p
WHERE r.`Nom` = 'Caissier'
  AND p.`Nom` IN (
      'Restaurant.Etablissement.Read',
      'Restaurant.Hold.Create',
      'Restaurant.Reservation.Confirm'
  );

-- Financier : Read + Confirm + Dashboard
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT r.`IdRole`, p.`IdPermission`, UTC_TIMESTAMP()
FROM `Roles` r
CROSS JOIN `Permissions` p
WHERE r.`Nom` = 'Financier'
  AND p.`Nom` IN (
      'Restaurant.Etablissement.Read',
      'Restaurant.Reservation.Confirm',
      'Restaurant.Dashboard.Read'
  );

SELECT r.`Nom` AS Role, p.`Nom` AS Permission
FROM `RolePermissions` rp
INNER JOIN `Roles` r ON r.`IdRole` = rp.`IdRole`
INNER JOIN `Permissions` p ON p.`IdPermission` = rp.`IdPermission`
WHERE p.`Categorie` = 'Restaurant'
ORDER BY r.`Nom`, p.`Nom`;
