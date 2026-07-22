-- =============================================================================
-- Correctif données : réaffecter IsSitePrincipal au site ayant InfoPaiement active
-- Usage UAT société 60 / site satellite 71 :
--   SET @IdSociete = 60; SET @IdSiteSatellite = 71;
-- Prérequis : au moins une ligne InfoPaiementsSociete.Statut = 1 pour la société.
-- =============================================================================

SET @IdSociete = 60;
SET @IdSiteSatellite = 71;

-- Vérification préalable (doit retourner 1 ligne avec IdSite != satellite idéalement)
SELECT
    i.IdSite,
    s.NomSite,
    s.CodeSite,
    s.IsSitePrincipal AS actuellement_principal,
    i.CodeMarchand
FROM InfoPaiementsSociete i
INNER JOIN Sites s ON s.IdSite = i.IdSite AND s.IdSociete = i.IdSociete
WHERE i.IdSociete = @IdSociete
  AND i.Statut = 1
  AND s.Statut = 1
ORDER BY s.IsSitePrincipal DESC, i.IdSite;

-- Site cible = premier site actif avec InfoPaiement active (priorité ancien principal)
SET @IdSitePrincipalCible = (
    SELECT i.IdSite
    FROM InfoPaiementsSociete i
    INNER JOIN Sites s ON s.IdSite = i.IdSite AND s.IdSociete = i.IdSociete
    WHERE i.IdSociete = @IdSociete AND i.Statut = 1 AND s.Statut = 1
    ORDER BY s.IsSitePrincipal DESC, i.IdSite
    LIMIT 1
);

SELECT @IdSitePrincipalCible AS id_site_principal_apres_fix;

-- Transaction manuelle recommandée en prod
START TRANSACTION;

UPDATE Sites
SET IsSitePrincipal = 0,
    DateModification = UTC_TIMESTAMP()
WHERE IdSociete = @IdSociete;

UPDATE Sites
SET IsSitePrincipal = 1,
    DateModification = UTC_TIMESTAMP()
WHERE IdSite = @IdSitePrincipalCible
  AND IdSociete = @IdSociete;

COMMIT;

-- Contrôle post-fix
SELECT IdSite, CodeSite, NomSite, IsSitePrincipal, Statut
FROM Sites
WHERE IdSociete = @IdSociete
ORDER BY IdSite;
