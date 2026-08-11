-- =============================================================================
-- Diagnostic permissions SiteTouristique + Restaurant
-- Vérifie présence des permissions, grants par rôle, et (optionnel) un utilisateur.
--
-- Si Admin/Gerant manque Write, ou Caissier/Client manque la matrice vente/gate :
--   1) assign_site_touristique_permissions_admin_gerant.sql
--   2) assign_restaurant_permissions_admin_gerant.sql
-- Puis retester Swagger (pas besoin de regenerer le JWT).
-- Rôles exacts : `Gerant`, `Caissier`, `Client` (sans accent).
--
-- Remplacer @IdUtilisateur pour le contrôle utilisateur (0 = ignorer).
-- =============================================================================

SET @IdUtilisateur = 0;

-- ---------------------------------------------------------------------------
-- 1) Permissions présentes
-- ---------------------------------------------------------------------------
SELECT
    Categorie,
    COUNT(*) AS NbPermissions,
    SUM(CASE WHEN Statut = 1 THEN 1 ELSE 0 END) AS NbActives
FROM Permissions
WHERE Categorie IN ('SiteTouristique', 'Restaurant')
GROUP BY Categorie
ORDER BY Categorie;

SELECT IdPermission, Nom, Categorie, Action, Statut, DateCreation
FROM Permissions
WHERE Categorie IN ('SiteTouristique', 'Restaurant')
ORDER BY Categorie, Nom;

-- ---------------------------------------------------------------------------
-- 2a) Grants critiques config (Admin / Gerant / Super-Admin)
-- ---------------------------------------------------------------------------
SELECT
    r.Nom AS Role,
    p.Nom AS Permission,
    CASE WHEN rp.IdRole IS NULL THEN 'MANQUANT' ELSE 'OK' END AS StatutGrant
FROM Roles r
CROSS JOIN Permissions p
LEFT JOIN RolePermissions rp
    ON rp.IdRole = r.IdRole AND rp.IdPermission = p.IdPermission
WHERE r.Nom IN ('Admin', 'Gerant', 'Super-Admin')
  AND p.Nom IN (
      'SiteTouristique.Lieu.Write',
      'SiteTouristique.Lieu.Read',
      'Restaurant.Etablissement.Write',
      'Restaurant.Etablissement.Read'
  )
ORDER BY r.Nom, p.Nom;

-- ---------------------------------------------------------------------------
-- 2b) Grants critiques vente / gate (Caissier / Client)
-- ---------------------------------------------------------------------------
SELECT
    r.Nom AS Role,
    p.Nom AS Permission,
    CASE WHEN rp.IdRole IS NULL THEN 'MANQUANT' ELSE 'OK' END AS StatutGrant
FROM Roles r
CROSS JOIN Permissions p
LEFT JOIN RolePermissions rp
    ON rp.IdRole = r.IdRole AND rp.IdPermission = p.IdPermission
WHERE r.Nom = 'Client'
  AND p.Nom IN (
      'SiteTouristique.Lieu.Read',
      'SiteTouristique.Hold.Create',
      'SiteTouristique.Reservation.Confirm',
      'Restaurant.Etablissement.Read',
      'Restaurant.Hold.Create',
      'Restaurant.Reservation.Confirm'
  )
ORDER BY p.Nom;

SELECT
    r.Nom AS Role,
    p.Nom AS Permission,
    CASE WHEN rp.IdRole IS NULL THEN 'MANQUANT' ELSE 'OK' END AS StatutGrant
FROM Roles r
CROSS JOIN Permissions p
LEFT JOIN RolePermissions rp
    ON rp.IdRole = r.IdRole AND rp.IdPermission = p.IdPermission
WHERE r.Nom = 'Caissier'
  AND p.Nom IN (
      'SiteTouristique.Lieu.Read',
      'SiteTouristique.Hold.Create',
      'SiteTouristique.Reservation.Confirm',
      'SiteTouristique.Ticket.Check',
      'SiteTouristique.Ticket.Use',
      'Restaurant.Etablissement.Read',
      'Restaurant.Hold.Create',
      'Restaurant.Reservation.Confirm'
  )
ORDER BY p.Nom;

-- ---------------------------------------------------------------------------
-- 3) Toutes les grants ST / Restaurant par rôle
-- ---------------------------------------------------------------------------
SELECT r.Nom AS Role, p.Categorie, p.Nom AS Permission
FROM RolePermissions rp
INNER JOIN Roles r ON r.IdRole = rp.IdRole
INNER JOIN Permissions p ON p.IdPermission = rp.IdPermission
WHERE p.Categorie IN ('SiteTouristique', 'Restaurant')
ORDER BY r.Nom, p.Categorie, p.Nom;

-- ---------------------------------------------------------------------------
-- 4) Utilisateur ciblé (si @IdUtilisateur > 0)
-- ---------------------------------------------------------------------------
SELECT
    u.IdUtilisateur,
    u.Email,
    r.Nom AS Role,
    ur.Statut AS RoleActif
FROM Utilisateurs u
INNER JOIN UserRoles ur ON ur.IdUtilisateur = u.IdUtilisateur
INNER JOIN Roles r ON r.IdRole = ur.IdRole
WHERE @IdUtilisateur > 0
  AND u.IdUtilisateur = @IdUtilisateur
ORDER BY r.Nom;

SELECT
    p.Nom AS Permission,
    CASE
        WHEN EXISTS (
            SELECT 1
            FROM UserRoles ur
            INNER JOIN RolePermissions rp ON rp.IdRole = ur.IdRole
            INNER JOIN Permissions p2 ON p2.IdPermission = rp.IdPermission
            WHERE ur.IdUtilisateur = @IdUtilisateur
              AND ur.Statut = 1
              AND p2.Nom = p.Nom
              AND p2.Statut = 1
        ) THEN 'OK_VIA_ROLE'
        ELSE 'MANQUANT'
    END AS StatutEffectif
FROM Permissions p
WHERE @IdUtilisateur > 0
  AND p.Nom IN (
      'SiteTouristique.Lieu.Write',
      'Restaurant.Etablissement.Write',
      'SiteTouristique.Hold.Create',
      'Restaurant.Hold.Create',
      'SiteTouristique.Ticket.Check'
  )
ORDER BY p.Nom;

-- ---------------------------------------------------------------------------
-- 5) Verdict Admin/Gerant (Write) + Caissier/Client (vente / gate)
-- ---------------------------------------------------------------------------
SELECT
    CASE
        WHEN EXISTS (
            SELECT 1 FROM RolePermissions rp
            INNER JOIN Roles r ON r.IdRole = rp.IdRole
            INNER JOIN Permissions p ON p.IdPermission = rp.IdPermission
            WHERE r.Nom = 'Admin' AND p.Nom = 'SiteTouristique.Lieu.Write'
        )
        AND EXISTS (
            SELECT 1 FROM RolePermissions rp
            INNER JOIN Roles r ON r.IdRole = rp.IdRole
            INNER JOIN Permissions p ON p.IdPermission = rp.IdPermission
            WHERE r.Nom = 'Admin' AND p.Nom = 'Restaurant.Etablissement.Write'
        )
        AND EXISTS (
            SELECT 1 FROM RolePermissions rp
            INNER JOIN Roles r ON r.IdRole = rp.IdRole
            INNER JOIN Permissions p ON p.IdPermission = rp.IdPermission
            WHERE r.Nom = 'Gerant' AND p.Nom = 'SiteTouristique.Lieu.Write'
        )
        AND EXISTS (
            SELECT 1 FROM RolePermissions rp
            INNER JOIN Roles r ON r.IdRole = rp.IdRole
            INNER JOIN Permissions p ON p.IdPermission = rp.IdPermission
            WHERE r.Nom = 'Gerant' AND p.Nom = 'Restaurant.Etablissement.Write'
        )
        AND EXISTS (
            SELECT 1 FROM RolePermissions rp
            INNER JOIN Roles r ON r.IdRole = rp.IdRole
            INNER JOIN Permissions p ON p.IdPermission = rp.IdPermission
            WHERE r.Nom = 'Client' AND p.Nom = 'SiteTouristique.Hold.Create'
        )
        AND EXISTS (
            SELECT 1 FROM RolePermissions rp
            INNER JOIN Roles r ON r.IdRole = rp.IdRole
            INNER JOIN Permissions p ON p.IdPermission = rp.IdPermission
            WHERE r.Nom = 'Client' AND p.Nom = 'SiteTouristique.Reservation.Confirm'
        )
        AND EXISTS (
            SELECT 1 FROM RolePermissions rp
            INNER JOIN Roles r ON r.IdRole = rp.IdRole
            INNER JOIN Permissions p ON p.IdPermission = rp.IdPermission
            WHERE r.Nom = 'Client' AND p.Nom = 'Restaurant.Hold.Create'
        )
        AND EXISTS (
            SELECT 1 FROM RolePermissions rp
            INNER JOIN Roles r ON r.IdRole = rp.IdRole
            INNER JOIN Permissions p ON p.IdPermission = rp.IdPermission
            WHERE r.Nom = 'Client' AND p.Nom = 'Restaurant.Reservation.Confirm'
        )
        AND EXISTS (
            SELECT 1 FROM RolePermissions rp
            INNER JOIN Roles r ON r.IdRole = rp.IdRole
            INNER JOIN Permissions p ON p.IdPermission = rp.IdPermission
            WHERE r.Nom = 'Caissier' AND p.Nom = 'SiteTouristique.Hold.Create'
        )
        AND EXISTS (
            SELECT 1 FROM RolePermissions rp
            INNER JOIN Roles r ON r.IdRole = rp.IdRole
            INNER JOIN Permissions p ON p.IdPermission = rp.IdPermission
            WHERE r.Nom = 'Caissier' AND p.Nom = 'SiteTouristique.Ticket.Check'
        )
        AND EXISTS (
            SELECT 1 FROM RolePermissions rp
            INNER JOIN Roles r ON r.IdRole = rp.IdRole
            INNER JOIN Permissions p ON p.IdPermission = rp.IdPermission
            WHERE r.Nom = 'Caissier' AND p.Nom = 'SiteTouristique.Ticket.Use'
        )
        AND EXISTS (
            SELECT 1 FROM RolePermissions rp
            INNER JOIN Roles r ON r.IdRole = rp.IdRole
            INNER JOIN Permissions p ON p.IdPermission = rp.IdPermission
            WHERE r.Nom = 'Caissier' AND p.Nom = 'Restaurant.Hold.Create'
        )
        AND EXISTS (
            SELECT 1 FROM RolePermissions rp
            INNER JOIN Roles r ON r.IdRole = rp.IdRole
            INNER JOIN Permissions p ON p.IdPermission = rp.IdPermission
            WHERE r.Nom = 'Caissier' AND p.Nom = 'Restaurant.Reservation.Confirm'
        )
        THEN 'OK: Admin/Gerant Write + Client vente + Caissier vente/gate ST + vente Restaurant'
        ELSE 'ACTION REQUISE: exécuter assign_site_touristique_permissions_admin_gerant.sql puis assign_restaurant_permissions_admin_gerant.sql'
    END AS Verdict;
