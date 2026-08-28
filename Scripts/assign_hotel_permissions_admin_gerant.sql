-- Hôtel Phases 1 à 5 — permissions et rôles (idempotent)
INSERT IGNORE INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
VALUES
 ('Hotel.Etablissement.Read', 'Hotel', 'Etablissement.Read', 'Lister / consulter établissements hôtel', 1, UTC_TIMESTAMP()),
 ('Hotel.Etablissement.Write', 'Hotel', 'Etablissement.Write', 'Créer / modifier / publier établissements hôtel', 1, UTC_TIMESTAMP()),
 ('Hotel.RoomType.Read', 'Hotel', 'RoomType.Read', 'Lister / consulter types de chambres', 1, UTC_TIMESTAMP()),
 ('Hotel.RoomType.Write', 'Hotel', 'RoomType.Write', 'Créer / modifier / publier types de chambres', 1, UTC_TIMESTAMP()),
 ('Hotel.Hold.Create', 'Hotel', 'Hold.Create', 'Créer un hold multi-nuit hôtel', 1, UTC_TIMESTAMP()),
 ('Hotel.Reservation.Confirm', 'Hotel', 'Reservation.Confirm', 'Confirmer acompte ou annuler une réservation hôtel', 1, UTC_TIMESTAMP()),
 ('Hotel.Dashboard.Read', 'Hotel', 'Dashboard.Read', 'Dashboard réservation hôtel', 1, UTC_TIMESTAMP());

INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT r.`IdRole`, p.`IdPermission`, UTC_TIMESTAMP()
FROM `Roles` r CROSS JOIN `Permissions` p
WHERE r.`Nom` IN ('Admin', 'Gerant') AND p.`Categorie` = 'Hotel' AND p.`Statut` = 1;

INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT r.`IdRole`, p.`IdPermission`, UTC_TIMESTAMP()
FROM `Roles` r CROSS JOIN `Permissions` p
WHERE r.`Nom` = 'Super-Admin' AND p.`Statut` = 1;

INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT r.`IdRole`, p.`IdPermission`, UTC_TIMESTAMP()
FROM `Roles` r CROSS JOIN `Permissions` p
WHERE r.`Nom` IN ('Client', 'Caissier')
  AND p.`Nom` IN (
    'Hotel.Etablissement.Read', 'Hotel.RoomType.Read',
    'Hotel.Hold.Create', 'Hotel.Reservation.Confirm'
  );

INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT r.`IdRole`, p.`IdPermission`, UTC_TIMESTAMP()
FROM `Roles` r CROSS JOIN `Permissions` p
WHERE r.`Nom` = 'Financier'
  AND p.`Nom` IN (
    'Hotel.Etablissement.Read', 'Hotel.RoomType.Read',
    'Hotel.Reservation.Confirm', 'Hotel.Dashboard.Read'
  );
