-- =============================================================================
-- Audit InfoPaiement / site principal — par société (UAT / prod)
-- Usage : SET @IdSociete = 60; puis exécuter les blocs ci-dessous.
-- Cas site 71 / société 60 : vérifier IsSitePrincipal et InfoPaiement active.
-- =============================================================================

SET @IdSociete = 60;

-- A) Sites de la société + flag principal
SELECT
    s.IdSite,
    s.CodeSite,
    s.NomSite,
    s.Statut AS site_statut,
    s.IsSitePrincipal
FROM Sites s
WHERE s.IdSociete = @IdSociete
ORDER BY s.IdSite;

-- B) InfoPaiement FlexPay par site
SELECT
    ips.IdInfoPaiementSociete,
    ips.IdSite,
    ips.Statut AS infopaiement_statut,
    ips.CodeMarchand,
    ips.ActifMobileMoney,
    ips.ActifCarteBancaire
FROM InfoPaiementsSociete ips
WHERE ips.IdSociete = @IdSociete
ORDER BY ips.IdSite;

-- C) Anomalie : 0 ou >1 site principal ACTIF
SELECT COUNT(*) AS nb_principaux_actifs
FROM Sites
WHERE IdSociete = @IdSociete AND IsSitePrincipal = 1 AND Statut = 1;

-- D) Site principal actif SANS InfoPaiement active (repli classique impossible)
SELECT
    s.IdSite,
    s.NomSite,
    s.CodeSite
FROM Sites s
WHERE s.IdSociete = @IdSociete
  AND s.IsSitePrincipal = 1
  AND s.Statut = 1
  AND NOT EXISTS (
      SELECT 1 FROM InfoPaiementsSociete i
      WHERE i.IdSite = s.IdSite AND i.IdSociete = @IdSociete AND i.Statut = 1
  );

-- E) Sites satellites actifs sans InfoPaiement propre (repli attendu si D vide et B non vide)
SELECT
    s.IdSite,
    s.CodeSite,
    s.NomSite
FROM Sites s
WHERE s.IdSociete = @IdSociete
  AND s.Statut = 1
  AND s.IsSitePrincipal = 0
  AND NOT EXISTS (
      SELECT 1 FROM InfoPaiementsSociete i
      WHERE i.IdSite = s.IdSite AND i.IdSociete = @IdSociete AND i.Statut = 1
  )
ORDER BY s.IdSite;

-- F) Société sans AUCUNE InfoPaiement active (FlexPay impossible)
SELECT COUNT(*) AS nb_infopaiement_actifs
FROM InfoPaiementsSociete
WHERE IdSociete = @IdSociete AND Statut = 1;
