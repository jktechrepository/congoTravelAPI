-- =============================================================================
-- Audit InfoPaiement / site principal — repli FlexPay satellites
-- Exécuter sur UAT ou prod après création d'un nouveau site sans InfoPaiement.
-- Audit ciblé par société : Scripts/audit-infopaiement-by-societe.sql (@IdSociete)
-- Correctif principal : Scripts/fix-infopaiement-principal-by-societe.sql
-- =============================================================================

-- 1) Sociétés avec 0 ou >1 site principal ACTIF (anomalie)
SELECT IdSociete, COUNT(*) AS nb_principaux_actifs
FROM Sites
WHERE IsSitePrincipal = 1 AND Statut = 1
GROUP BY IdSociete
HAVING COUNT(*) <> 1;

-- 2) Site principal actif + InfoPaiement (doit avoir Statut = 1 pour le repli)
SELECT
    s.IdSociete,
    s.IdSite,
    s.NomSite,
    s.CodeSite,
    s.IsSitePrincipal,
    s.Statut AS site_statut,
    ips.IdInfoPaiementSociete,
    ips.Statut AS infopaiement_statut,
    ips.CodeMarchand,
    ips.ActifMobileMoney,
    ips.ActifCarteBancaire
FROM Sites s
LEFT JOIN InfoPaiementsSociete ips ON ips.IdSite = s.IdSite AND ips.IdSociete = s.IdSociete
WHERE s.IsSitePrincipal = 1 AND s.Statut = 1
ORDER BY s.IdSociete, s.IdSite;

-- 3) Sites satellites ACTIFS sans InfoPaiement propre active (candidats repli)
SELECT
    s.IdSociete,
    s.IdSite,
    s.CodeSite,
    s.NomSite
FROM Sites s
WHERE s.Statut = 1
  AND s.IsSitePrincipal = 0
  AND NOT EXISTS (
      SELECT 1
      FROM InfoPaiementsSociete i
      WHERE i.IdSite = s.IdSite
        AND i.IdSociete = s.IdSociete
        AND i.Statut = 1
  )
ORDER BY s.IdSociete, s.IdSite;

-- 4) Sociétés sans aucun site principal actif
SELECT DISTINCT s.IdSociete
FROM Sites s
WHERE s.Statut = 1
  AND NOT EXISTS (
      SELECT 1 FROM Sites p
      WHERE p.IdSociete = s.IdSociete AND p.IsSitePrincipal = 1 AND p.Statut = 1
  );

-- 5) Site principal actif mais SANS InfoPaiement active (repli impossible)
SELECT
    s.IdSociete,
    s.IdSite,
    s.NomSite
FROM Sites s
WHERE s.IsSitePrincipal = 1 AND s.Statut = 1
  AND NOT EXISTS (
      SELECT 1 FROM InfoPaiementsSociete i
      WHERE i.IdSite = s.IdSite AND i.IdSociete = s.IdSociete AND i.Statut = 1
  );
