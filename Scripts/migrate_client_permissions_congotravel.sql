-- Migration one-shot : permissions rôle Client (CongoTravel transport)
-- Retire legacy Kenergie (Facture, CategorieClient) et assigne le périmètre transport.

SET @client_role_id = (SELECT `IdRole` FROM `Roles` WHERE `Nom` = 'Client' LIMIT 1);

DELETE rp FROM `RolePermissions` rp
INNER JOIN `Permissions` p ON p.`IdPermission` = rp.`IdPermission`
WHERE rp.`IdRole` = @client_role_id
  AND @client_role_id IS NOT NULL
  AND p.`Categorie` IN ('Facture', 'CategorieClient');

INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT @client_role_id, p.`IdPermission`, NOW()
FROM `Permissions` p
WHERE @client_role_id IS NOT NULL
  AND p.`Statut` = 1
  AND (
      (p.`Categorie` = 'Client' AND p.`Action` IN ('Read', 'ReadAll'))
      OR (p.`Categorie` = 'PlainteClient' AND p.`Action` IN ('Create', 'Read', 'ReadAll'))
      OR (p.`Categorie` = 'ClientDashboard' AND p.`Action` = 'ReadAll')
      OR (p.`Categorie` = 'Reservation' AND p.`Action` IN ('Create', 'Read', 'ReadAll'))
      OR (p.`Categorie` = 'Paiement' AND p.`Action` IN ('Read', 'ReadAll'))
      OR (p.`Categorie` = 'Billet' AND p.`Action` IN ('Read', 'ReadAll'))
      OR (p.`Categorie` = 'Voyage' AND p.`Action` IN ('Read', 'ReadAll'))
      OR (p.`Categorie` = 'Destination' AND p.`Action` IN ('Read', 'ReadAll'))
  );

INSERT IGNORE INTO `RolePermissions` (`IdRole`, `IdPermission`, `DateAttribution`)
SELECT r.`IdRole`, p.`IdPermission`, NOW()
FROM `Roles` r
INNER JOIN `Permissions` p ON p.`Nom` = 'Utilisateur.DeactivateSelf' AND p.`Statut` = 1
WHERE r.`Nom` = 'Client' AND r.`Statut` = 1;
