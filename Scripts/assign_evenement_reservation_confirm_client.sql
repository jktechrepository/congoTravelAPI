-- =============================================================================
-- Assigner Evenement.Reservation.Confirm au rôle Client
-- Corrige : 403 sur POST /api/events/reservations/with-paiement-electronique
--            Permissions requises : Evenement.Hold.Create + Evenement.Reservation.Confirm
--
-- Diagnostic :
--   SELECT r.Nom, p.Nom
--   FROM RolePermissions rp
--   JOIN Roles r ON r.IdRole = rp.IdRole
--   JOIN Permissions p ON p.IdPermission = rp.IdPermission
--   WHERE r.Nom = 'Client' AND p.Categorie = 'Evenement';
-- =============================================================================

-- 1) Créer la permission si absente (aligné PermissionSeeder)
INSERT IGNORE INTO `Permissions` (`Nom`, `Categorie`, `Action`, `Description`, `Statut`, `DateCreation`)
VALUES
    ('Evenement.Reservation.Confirm', 'Evenement', 'Reservation.Confirm', 'Confirmer réservation événement', 1, UTC_TIMESTAMP());

-- 2) Client : Evenement.Reservation.Confirm (FlexPay + annulation propre)
INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT r.`IdRole`, p.`IdPermission`, UTC_TIMESTAMP()
FROM `Roles` r
CROSS JOIN `Permissions` p
WHERE r.`Nom` = 'Client'
  AND p.`Nom` = 'Evenement.Reservation.Confirm'
  AND p.`Statut` = 1;

-- 3) Vérification
SELECT r.`Nom` AS Role, p.`Nom` AS Permission
FROM `RolePermissions` rp
INNER JOIN `Roles` r ON r.`IdRole` = rp.`IdRole`
INNER JOIN `Permissions` p ON p.`IdPermission` = rp.`IdPermission`
WHERE r.`Nom` = 'Client' AND p.`Categorie` = 'Evenement'
ORDER BY p.`Nom`;
